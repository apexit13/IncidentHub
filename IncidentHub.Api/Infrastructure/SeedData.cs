using IncidentHub.Api.Domain;
using IncidentHub.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Infrastructure;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Incidents.AnyAsync())
            return; // already seeded

        var now = DateTimeOffset.UtcNow;

        // ── Users (Auth0 sub claims — replace with real subs after creating users) ──
        const string jayD   = "auth0|69c9754aa20453904df4ac43";
        const string samR   = "auth0|69c975908b672911c49fd5dd";
        const string alexL  = "auth0|69c975c78b672911c49fd5e5";
        const string miaC   = "auth0|69c975fe8b672911c49fd5f3";

        // ── Incidents ──────────────────────────────────────────────────────────────

        var inc1 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Title       = "Payment gateway timeout — EU region",
            Description = "Customers in the EU region are receiving 504 errors when attempting to complete checkout. Affects approximately 12% of transactions.",
            Severity    = Severity.Critical,
            Status      = IncidentStatus.Investigating,
            AssignedTo  = jayD,
            CreatedAt   = now.AddHours(-2)
        };

        var inc2 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Title       = "Search indexing delayed — all regions",
            Description = "New listings are taking 15–20 minutes to appear in search results. Expected SLA is under 60 seconds.",
            Severity    = Severity.High,
            Status      = IncidentStatus.Identified,
            AssignedTo  = samR,
            CreatedAt   = now.AddHours(-3)
        };

        var inc3 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Title       = "Webhook delivery latency elevated",
            Description = "Outbound webhooks to third-party integrations are experiencing 30–45 second delays. Stripe and Shopify integrations most affected.",
            Severity    = Severity.Medium,
            Status      = IncidentStatus.Monitoring,
            AssignedTo  = alexL,
            CreatedAt   = now.AddHours(-5)
        };

        var inc4 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Title       = "Login service intermittent 401s",
            Description = "Small percentage of users receiving unexpected 401 responses despite valid sessions. Likely a token validation edge case.",
            Severity    = Severity.High,
            Status      = IncidentStatus.Investigating,
            AssignedTo  = miaC,
            CreatedAt   = now.AddHours(-1)
        };

        var inc5 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            Title       = "Image CDN cache purge failure",
            Description = "Updated product images not reflecting for some users due to CDN cache not purging on update.",
            Severity    = Severity.Low,
            Status      = IncidentStatus.Resolved,
            AssignedTo  = samR,
            CreatedAt   = now.AddHours(-8),
            ResolvedAt  = now.AddHours(-6)
        };

        var inc6 = new Incident
        {
            Id          = Guid.Parse("00000000-0000-0000-0000-000000000006"),
            Title       = "Database connection pool exhausted — US-West",
            Description = "API response times exceeding 10s in US-West. Root cause traced to a missing index causing full table scans under load.",
            Severity    = Severity.Critical,
            Status      = IncidentStatus.Resolved,
            AssignedTo  = jayD,
            CreatedAt   = now.AddDays(-1),
            ResolvedAt  = now.AddDays(-1).AddHours(2)
        };

        db.Incidents.AddRange(inc1, inc2, inc3, inc4, inc5, inc6);

        // ── Timelines ─────────────────────────────────────────────────────────────

        db.IncidentTimelines.AddRange(

            // INC-001 — Payment gateway
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc1.Id, Message = "Incident raised", ChangedBy = jayD, Timestamp = now.AddHours(-2), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc1.Id, Message = "PagerDuty alert triggered, on-call notified", ChangedBy = jayD, Timestamp = now.AddHours(-2).AddMinutes(1) },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc1.Id, Message = "Status changed from New to Investigating", ChangedBy = jayD, Timestamp = now.AddHours(-2).AddMinutes(3), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc1.Id, Message = "Traced to EU payment processor returning unexpected TLS handshake errors", ChangedBy = jayD, Timestamp = now.AddHours(-1).AddMinutes(30) },

            // INC-002 — Search indexing
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc2.Id, Message = "Incident raised", ChangedBy = samR, Timestamp = now.AddHours(-3), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc2.Id, Message = "Status changed from New to Investigating", ChangedBy = samR, Timestamp = now.AddHours(-3).AddMinutes(5), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc2.Id, Message = "Elasticsearch cluster node failure detected on es-node-04", ChangedBy = samR, Timestamp = now.AddHours(-2).AddMinutes(45) },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc2.Id, Message = "Status changed from Investigating to Identified", ChangedBy = samR, Timestamp = now.AddHours(-2).AddMinutes(50), NewStatus = IncidentStatus.Identified },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc2.Id, Message = "Re-index job queued, estimated completion 40 minutes", ChangedBy = samR, Timestamp = now.AddHours(-2).AddMinutes(55) },

            // INC-003 — Webhook latency
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc3.Id, Message = "Incident raised", ChangedBy = alexL, Timestamp = now.AddHours(-5), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc3.Id, Message = "Status changed from New to Investigating", ChangedBy = alexL, Timestamp = now.AddHours(-5).AddMinutes(8), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc3.Id, Message = "Traced to downstream rate limiting on Stripe endpoint. Retry logic updated.", ChangedBy = alexL, Timestamp = now.AddHours(-4).AddMinutes(20) },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc3.Id, Message = "Status changed from Investigating to Monitoring", ChangedBy = alexL, Timestamp = now.AddHours(-4), NewStatus = IncidentStatus.Monitoring },

            // INC-004 — Login 401s
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc4.Id, Message = "Incident raised", ChangedBy = miaC, Timestamp = now.AddHours(-1), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc4.Id, Message = "Status changed from New to Investigating", ChangedBy = miaC, Timestamp = now.AddHours(-1).AddMinutes(4), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc4.Id, Message = "Narrowed to users with tokens issued before last deployment. Auth0 rule change suspected.", ChangedBy = miaC, Timestamp = now.AddMinutes(-30) },

            // INC-005 — CDN cache (resolved)
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc5.Id, Message = "Incident raised", ChangedBy = samR, Timestamp = now.AddHours(-8), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc5.Id, Message = "Status changed from New to Investigating", ChangedBy = samR, Timestamp = now.AddHours(-8).AddMinutes(5), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc5.Id, Message = "CDN purge API returning 200 but not actually invalidating. Raised with CDN vendor.", ChangedBy = samR, Timestamp = now.AddHours(-7).AddMinutes(15) },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc5.Id, Message = "Vendor confirmed bug in purge API. Workaround: force-purge via CLI.", ChangedBy = samR, Timestamp = now.AddHours(-6).AddMinutes(30) },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc5.Id, Message = "Incident resolved. All images serving correctly.", ChangedBy = samR, Timestamp = now.AddHours(-6), NewStatus = IncidentStatus.Resolved },

            // INC-006 — DB connection pool (resolved)
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc6.Id, Message = "Incident raised", ChangedBy = jayD, Timestamp = now.AddDays(-1), NewStatus = IncidentStatus.New },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc6.Id, Message = "Status changed from New to Investigating", ChangedBy = jayD, Timestamp = now.AddDays(-1).AddMinutes(3), NewStatus = IncidentStatus.Investigating },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc6.Id, Message = "Root cause identified: missing index on Orders.CustomerId causing full table scans under peak load", ChangedBy = jayD, Timestamp = now.AddDays(-1).AddMinutes(45), NewStatus = IncidentStatus.Identified },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc6.Id, Message = "Index created on Orders.CustomerId. Response times recovering.", ChangedBy = jayD, Timestamp = now.AddDays(-1).AddHours(1).AddMinutes(30), NewStatus = IncidentStatus.Monitoring },
            new IncidentTimeline { Id = Guid.NewGuid(), IncidentId = inc6.Id, Message = "Incident resolved. P95 response time back under 200ms.", ChangedBy = jayD, Timestamp = now.AddDays(-1).AddHours(2), NewStatus = IncidentStatus.Resolved }
        );

        await db.SaveChangesAsync();
    }
}
