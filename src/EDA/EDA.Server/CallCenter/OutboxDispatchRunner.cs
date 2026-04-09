using System.Text.Json;
using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class OutboxDispatchRunner(CallCenterDbContext db, IPublishEndpoint publisher, ILogger<OutboxDispatchRunner> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await db.OutboxMessages
            .Where(message => message.Status == OutboxStatuses.Pending)
            .OrderBy(message => message.OccurredAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            await DispatchAsync(message, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        message.Attempts++;

        try
        {
            if (message.MessageType == nameof(TranscriptReceived))
            {
                var payload = JsonSerializer.Deserialize<TranscriptReceived>(message.Payload, SerializerOptions);

                if (payload is null)
                {
                    message.Status = OutboxStatuses.Failed;
                    message.LastError = "Unable to deserialize TranscriptReceived payload.";
                    return;
                }

                await publisher.Publish(payload, cancellationToken);
                message.Status = OutboxStatuses.Published;
                message.PublishedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
                return;
            }

            message.Status = OutboxStatuses.Failed;
            message.LastError = $"Unknown message type '{message.MessageType}'.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox dispatch failed for message {MessageId}", message.Id);
            message.LastError = ex.Message;
            message.Status = OutboxStatuses.Pending;
        }
    }
}
