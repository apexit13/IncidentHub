using FluentValidation;
using IncidentHub.Api.Features.Incidents.Commands.RaiseIncident;
using IncidentHub.Api.Features.Incidents.Commands.UpdateIncidentStatus;
using IncidentHub.Api.Features.Incidents.Queries.GetIncidents;
using IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSignalR().AddAzureSignalR();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddAuthentication().AddJwtBearer(o =>
{
    o.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
    o.Audience = builder.Configuration["Auth0:Audience"];
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("responder", policy =>
        policy.RequireClaim("https://incidenthub/roles", "responder"));
});
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
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "IncidentHub API";
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["Bearer"]
        };
    });

    // Seed database in development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();   // runs any pending migrations
    await SeedData.SeedAsync(db);       // seeds if empty
}

app.UseHttpsRedirection();
app.UseCors("SignalRPolicy"); // must come before MapHub
app.MapHub<IncidentBroadcastHub>("/hubs/incidents");

// Endpoints
app.MapPost("/api/incidents", async (RaiseIncidentCommand cmd, IMediator m) =>
{
    var result = await m.Send(cmd);
    return Results.Created($"/api/incidents/{result.Id}", result);
})
    .RequireAuthorization("responder");

app.MapPatch("/api/incidents/{id}/status", async (
    Guid id, UpdateIncidentStatusCommand cmd, IMediator m, HttpContext http) =>
{
    var sub = http.User.FindFirst("sub")?.Value;
    return await m.Send(cmd with { Id = id, ChangedBy = sub });
})
.RequireAuthorization("responder");

app.MapGet("/api/incidents", async (IMediator m)
    => await m.Send(new GetIncidentsQuery()))
    .RequireAuthorization();

app.MapGet("/api/incidents/{id}/timeline", async (Guid id, IMediator m)
    => await m.Send(new GetIncidentTimelineQuery(id)))
    .RequireAuthorization();

app.Run();