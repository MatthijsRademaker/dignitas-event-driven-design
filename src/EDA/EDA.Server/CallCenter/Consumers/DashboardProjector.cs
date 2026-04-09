using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter.Consumers;

public sealed class DashboardProjector(CallCenterDbContext db) : IConsumer<TranscriptReceived>
{
    public async Task Consume(ConsumeContext<TranscriptReceived> context)
    {
        var message = context.Message;

        var segmentCount = await db.TranscriptSegments
            .CountAsync(segment => segment.CallId == message.CallId, context.CancellationToken);

        var dashboard = await db.AgentDashboards
            .FirstOrDefaultAsync(projection => projection.CallId == message.CallId, context.CancellationToken);

        if (dashboard is null)
        {
            dashboard = new AgentDashboardProjection { CallId = message.CallId };
            db.AgentDashboards.Add(dashboard);
        }

        dashboard.LastSpeaker = message.Speaker;
        dashboard.LastSnippet = TrimSnippet(message.Text);
        dashboard.SegmentCount = segmentCount;
        dashboard.UpdatedAt = message.ReceivedAt;

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private static string TrimSnippet(string text)
    {
        const int maxLength = 120;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : string.Concat(trimmed.AsSpan(0, maxLength), "...");
    }
}
