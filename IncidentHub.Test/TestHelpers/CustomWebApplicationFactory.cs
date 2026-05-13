using System.Security.Claims;
using System.Text.Encodings.Web;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using IncidentHub.Tests.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentHub.Tests.TestHelpers
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                services.AddScoped<IDataSeeder, TestDataSeeder>();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
            });
        }
        // Test implementation of ICurrentUserService
        public class TestCurrentUserService : ICurrentUserService
        {
            public string UserId => "test-user-id";
            public string UserName => "Test User";
            public string UserEmail => "test@example.com";
            public string Nickname => "tester";
            public string? Picture => null;
            public bool IsAuthenticated => true;
        }

        // Test implementation of IUserDisplayNameService
        public class TestUserDisplayNameService : IUserDisplayNameService
        {
            public Task<string> GetDisplayNameAsync(string userId)
            {
                return Task.FromResult("Test User");
            }
        }

        // Add a simple data seeder for tests that uses TestDataFactory
        public interface IDataSeeder
        {
            Task SeedAsync();
        }

        public class TestDataSeeder(AppDbContext context) : IDataSeeder
        {
            public async Task SeedAsync()
            {
                await context.Database.EnsureCreatedAsync();

                // Use TestDataFactory to create test incidents
                if (!context.Incidents.Any())
                {
                    var incident1 = TestDataFactory.CreateIncident(
                        "Critical Server Outage",
                        Severity.Critical,
                        IncidentStatus.New,
                        "responder1");

                    var incident2 = TestDataFactory.CreateIncident(
                        "Network Performance Issue",
                        Severity.Medium,
                        IncidentStatus.Identified,
                        "responder2");

                    var incident3 = TestDataFactory.CreateIncident(
                        "Database Connection Failure",
                        Severity.High,
                        IncidentStatus.Resolved,
                        "responder1");

                    context.Incidents.AddRange(incident1, incident2, incident3);

                    // Add timeline entries for each incident
                    context.IncidentTimelines.AddRange(
                        TestDataFactory.CreateTimelineEntry(incident1.Id, "Incident reported by monitoring system", "system"),
                        TestDataFactory.CreateTimelineEntry(incident1.Id, "Investigation started", "responder1"),
                        TestDataFactory.CreateTimelineEntry(incident2.Id, "Performance degradation detected", "system"),
                        TestDataFactory.CreateTimelineEntry(incident2.Id, "Team assigned to investigate", "responder2"),
                        TestDataFactory.CreateTimelineEntry(incident3.Id, "Database connection restored", "responder1", IncidentStatus.Resolved)
                    );

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}