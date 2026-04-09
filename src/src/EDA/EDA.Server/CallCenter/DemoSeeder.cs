using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class DemoSeeder(IServiceScopeFactory scopeFactory, ILogger<DemoSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(reset: false, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task SeedAsync(bool reset, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CallCenterDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        try
        {
            await db.OutboxMessages.AnyAsync(cancellationToken);
        }
        catch (Exception)
        {
            logger.LogWarning("Database schema missing outbox table. Recreating demo database.");
            await db.Database.EnsureDeletedAsync(cancellationToken);
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (reset)
        {
            db.OutboxMessages.RemoveRange(db.OutboxMessages);
            db.Suggestions.RemoveRange(db.Suggestions);
            db.AgentDashboards.RemoveRange(db.AgentDashboards);
            db.TranscriptSegments.RemoveRange(db.TranscriptSegments);
            db.CallSessions.RemoveRange(db.CallSessions);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.CallSessions.AnyAsync(cancellationToken))
        {
            return;
        }

        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-3);

        var call = new CallSession
        {
            Id = DemoIds.ActiveCallId,
            AgentName = "Alex Rivera",
            CallerName = "Jamie Lee",
            Status = "Active",
            StartedAt = startedAt
        };

        var dashboard = new AgentDashboardProjection
        {
            CallId = call.Id,
            LastSpeaker = "System",
            LastSnippet = "Call connected. Waiting for transcript.",
            SegmentCount = 0,
            UpdatedAt = startedAt
        };

        db.CallSessions.Add(call);
        db.AgentDashboards.Add(dashboard);

        await db.SaveChangesAsync(cancellationToken);
    }
}
