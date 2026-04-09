using System.Text.Json;
using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CallCenterDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var pending = await db.OutboxMessages
                    .Where(message => message.Status == OutboxStatuses.Pending)
                    .OrderBy(message => message.OccurredAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (pending.Count > 0)
                {
                    foreach (var message in pending)
                    {
                        await DispatchAsync(message, publisher, stoppingToken);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private static async Task DispatchAsync(
        OutboxMessage message,
        IPublishEndpoint publisher,
        CancellationToken cancellationToken)
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
            message.LastError = ex.Message;
            message.Status = OutboxStatuses.Pending;
        }
    }
}
