using FluentValidation;
using IncidentHub.Api.Constants;
using IncidentHub.Api.Features.Incidents.Commands.AssignIncident;
using IncidentHub.Api.Features.Incidents.Commands.CreateIncident;
using IncidentHub.Api.Features.Incidents.Commands.ResolveIncident;
using IncidentHub.Api.Features.Incidents.Commands.UpdateIncidentStatus;
using IncidentHub.Api.Features.Incidents.Queries.GetIncidentById;
using IncidentHub.Api.Features.Incidents.Queries.GetIncidents;
using IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;
using IncidentHub.Api.Features.Users.Queries.GetUserById;
using IncidentHub.Api.Features.Users.Queries.GetUsersByRole;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using IncidentHub.Api.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using static IncidentHub.Api.Constants.AuthPolicies;

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

    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<IUserDisplayNameService, UserDisplayNameService>();

    builder.Services.AddOpenApi();
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
        // API validates the "permissions" claim found in the Access Token
        options.AddPolicy(Policies.CanReadIncidents, policy =>
            policy.RequireClaim("permissions", Permissions.ReadIncidents));

        options.AddPolicy(Policies.CanCreateIncidents, policy =>
            policy.RequireClaim("permissions", Permissions.CreateIncidents));

        options.AddPolicy(Policies.CanManageIncidents, policy =>
            policy.RequireClaim("permissions", Permissions.ManageIncidents));

        options.AddPolicy(Policies.CanAssignIncidents, policy =>
            policy.RequireClaim("permissions", Permissions.AssignIncidents));

        options.AddPolicy(Policies.CanReadUsers, policy =>
            policy.RequireClaim("permissions", Permissions.ReadUsers));
    });

    // CORS is just implemented for local dev.
    // For production, Azure configures CORS in front of the API to only allow specific clients
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendCors", policy => policy
            .WithOrigins(
            "https://lively-ocean-03dfd8a0f.7.azurestaticapps.net",
            "http://localhost:5173",
            "https://localhost:7123"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Mock middleware stays active in Development so we can still test
        // locally via Scalar with the X-Permissions header — no Auth0 token needed.
        app.UseMiddleware<TestUserMiddleware>();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "IncidentHub API";
        });

        // Seed database in development
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();   // runs any pending migrations
        await SeedData.SeedAsync(db);       // seeds if empty
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("FrontendCors");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHub<IncidentBroadcastHub>("/hubs/incidents");

    // ── Queries ───────────────────────────────────────────────────────────
    app.MapGet("/api/incidents", async (IMediator m)
        => await m.Send(new GetIncidentsQuery()))
        .RequireAuthorization(Policies.CanReadIncidents);

    app.MapGet("/api/incidents/{id:guid}", async (Guid id, IMediator m) =>
    {
        var result = await m.Send(new GetIncidentByIdQuery(id));
        return result is null ? Results.NotFound() : Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanReadIncidents);

    app.MapGet("/api/incidents/{id:guid}/timeline", async (Guid id, IMediator m)
        => await m.Send(new GetIncidentTimelineQuery(id)))
        .RequireAuthorization(Policies.CanReadIncidents);

    app.MapGet("/api/users/{id}", async (
        string id,
        IMediator m) =>
    {
        var result = await m.Send(new GetUserByIdQuery(id));
        return Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanReadUsers);

    app.MapGet("/api/users/by-role/{role}", async (
    string role,
    IMediator m) =>
    {
        var result = await m.Send(new GetUsersByRoleQuery(role));
        return Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanReadUsers);

    // ── Commands ──────────────────────────────────────────────────────────
    app.MapPost("/api/incidents", async (
        CreateIncidentCommand cmd, IMediator m, HttpContext http) =>
    {
        var result = await m.Send(cmd);
        return Results.Created($"/api/incidents/{result.Id}", result);
    })
        .RequireAuthorization(Policies.CanCreateIncidents);

    app.MapPatch("/api/incidents/{id:guid}/status", async (
        Guid id, UpdateIncidentStatusCommand cmd, IMediator m, HttpContext http) =>
    {
        var result = await m.Send(cmd with { Id = id });
        return Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanManageIncidents);

    app.MapPost("/api/incidents/{id:guid}/resolve", async (
        Guid id, ResolveIncidentCommand cmd, IMediator m, HttpContext http) =>
    {
        var sub = http.User.FindFirst("sub")?.Value;
        var result = await m.Send(cmd with { Id = id, ChangedBy = sub });
        return Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanManageIncidents);

    app.MapPatch("/api/incidents/{id:guid}/assignment", async (
        Guid id, AssignIncidentCommand cmd, IMediator m) =>
    {
        var result = await m.Send(cmd with { Id = id });
        return Results.Ok(result);
    })
        .RequireAuthorization(Policies.CanAssignIncidents);

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