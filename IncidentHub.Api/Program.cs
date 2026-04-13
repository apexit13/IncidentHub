using FluentValidation;
using IncidentHub.Api.Common;
using IncidentHub.Api.Features.Incidents.Commands.RaiseIncident;
using IncidentHub.Api.Features.Incidents.Commands.ResolveIncident;
using IncidentHub.Api.Features.Incidents.Commands.UpdateIncidentStatus;
using IncidentHub.Api.Features.Incidents.Queries.GetIncidentById;
using IncidentHub.Api.Features.Incidents.Queries.GetIncidents;
using IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Security;
using IncidentHub.Api.Middleware;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("IncidentHub API starting up");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var config = configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);

        // Add Application Insights only if connection string is provided
        var aiConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(aiConnectionString))
        {
            config.WriteTo.ApplicationInsights(aiConnectionString, TelemetryConverter.Traces);
        }
    });



    if (builder.Environment.IsDevelopment())
    {
        // Add Bearer Auth section to OpenAPI document for testing without manual copy/paste of dev tokens each time
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
    }

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMediatR(typeof(Program));
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    if (!builder.Environment.IsDevelopment())
    {
        // Only add Application Insights in non-development environments to avoid polluting telemetry with dev/test data 
        builder.Services.AddApplicationInsightsTelemetry();
    }

    var signalR = builder.Services.AddSignalR();

    // Only delegate to Azure SignalR Service when a connection string is configured.
    // Locally this falls back to in-process SignalR — no Azure resource needed.
    var azureSignalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
    if (!string.IsNullOrEmpty(azureSignalRConnectionString))
    {
        signalR.AddAzureSignalR(azureSignalRConnectionString);
    }

    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddAuthentication().AddJwtBearer(o =>
    {
        o.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        o.Audience = builder.Configuration["Auth0:Audience"];
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("responder", policy =>
            policy.RequireClaim(ClaimConstants.RolesUri, ClaimConstants.RoleTypeResponder));
        options.AddPolicy("viewer", policy =>
            policy.RequireClaim(ClaimConstants.RolesUri, ClaimConstants.RoleTypeViewer, ClaimConstants.RoleTypeResponder));
    });

    // CORS is just implemented for local dev.
    // For production, Azure configures CORS in front of the API to only allow specific clients
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SignalRPolicy", policy => policy
            .WithOrigins(
                "http://localhost:5173",
                "https://incidenthub.azurestaticapps.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Mock middleware stays active in Development so we can still test
        // locally via Scalar with the X-Role header — no Auth0 token needed.
        app.UseMiddleware<TestUserMiddleware>();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "IncidentHub API";
            options.Theme = ScalarTheme.DeepSpace;

            // Read the dev token from appsettings.Development.json so we
            // never have to paste it manually into Scalar
            var devToken = builder.Configuration["Scalar:DevToken"] ?? string.Empty;

            options.AddPreferredSecuritySchemes("Bearer");
            options.AddHttpAuthentication("Bearer", auth =>
            {
                auth.Token = devToken;
            });
        });

        // Seed database in development
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();   // runs any pending migrations
        await SeedData.SeedAsync(db);       // seeds if empty
    }

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "IncidentHub API";
        options.Theme = ScalarTheme.DeepSpace;

        // Read the dev token from appsettings.Development.json so we
        // never have to paste it manually into Scalar
        var devToken = builder.Configuration["Scalar:DevToken"] ?? string.Empty;

        options.AddPreferredSecuritySchemes("Bearer");
        options.AddHttpAuthentication("Bearer", auth =>
        {
            auth.Token = devToken;
        });
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("SignalRPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHub<IncidentBroadcastHub>("/hubs/incidents");

    // ── Queries ───────────────────────────────────────────────────────────
    app.MapGet("/api/incidents", async (IMediator m)
        => await m.Send(new GetIncidentsQuery()))
        .RequireAuthorization();

    app.MapGet("/api/incidents/{id:guid}", async (Guid id, IMediator m) =>
    {
        var result = await m.Send(new GetIncidentByIdQuery(id));
        return result is null ? Results.NotFound() : Results.Ok(result);
    })
        .RequireAuthorization();

    app.MapGet("/api/incidents/{id:guid}/timeline", async (Guid id, IMediator m)
        => await m.Send(new GetIncidentTimelineQuery(id)))
        .RequireAuthorization();

    // ── Commands ──────────────────────────────────────────────────────────
    app.MapPost("/api/incidents", async (RaiseIncidentCommand cmd, IMediator m, HttpContext http) =>
    {
        var sub = http.User.FindFirst("sub")?.Value;
        var result = await m.Send(cmd with { AssignedTo = sub });
        return Results.Created($"/api/incidents/{result.Id}", result);
    })
        .RequireAuthorization(ClaimConstants.RoleTypeResponder);

    app.MapPatch("/api/incidents/{id:guid}/status", async (
        Guid id, UpdateIncidentStatusCommand cmd, IMediator m, HttpContext http) =>
    {
        var sub = http.User.FindFirst("sub")?.Value;
        var result = await m.Send(cmd with { Id = id, ChangedBy = sub });
        return Results.Ok(result);
    })
        .RequireAuthorization(ClaimConstants.RoleTypeResponder);

    app.MapPost("/api/incidents/{id:guid}/resolve", async (
        Guid id, ResolveIncidentCommand cmd, IMediator m, HttpContext http) =>
    {
        var sub = http.User.FindFirst("sub")?.Value;
        var result = await m.Send(cmd with { Id = id, ChangedBy = sub });
        return Results.Ok(result);
    })
        .RequireAuthorization(ClaimConstants.RoleTypeResponder);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "IncidentHub API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}