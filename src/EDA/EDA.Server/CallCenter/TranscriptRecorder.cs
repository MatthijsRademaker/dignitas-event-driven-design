using System.Text.Json;
using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class TranscriptRecorder(CallCenterDbContext db, IPublishEndpoint publishEndpoint)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<TranscriptRecordResult?> RecordAsync(TranscriptRequest request, CancellationToken cancellationToken)
    {
        var callId = request.CallId ?? DemoIds.ActiveCallId;

        var callExists = await db.CallSessions
            .AnyAsync(call => call.Id == callId, cancellationToken);

        if (!callExists)
        {
            return null;
        }

        var speaker = string.IsNullOrWhiteSpace(request.Speaker) ? "Caller" : request.Speaker.Trim();
        var text = request.Text.Trim();

        var segment = new TranscriptSegment
        {
            Id = Guid.NewGuid(),
            CallId = callId,
            Speaker = speaker,
            Text = text,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var transcriptEvent = new TranscriptReceived(callId, segment.Id, speaker, text, segment.ReceivedAt);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredAt = segment.ReceivedAt,
            MessageType = nameof(TranscriptReceived),
            Payload = JsonSerializer.Serialize(transcriptEvent, SerializerOptions),
            Status = OutboxStatuses.Pending
        };

        db.TranscriptSegments.Add(segment);
        db.OutboxMessages.Add(outboxMessage);
        await db.SaveChangesAsync(cancellationToken);

        if (request.SimulatePublishFailure)
        {
            outboxMessage.Attempts = 1;
            outboxMessage.LastError = "Simulated publish failure: event queued in outbox.";
            await db.SaveChangesAsync(cancellationToken);

            return new TranscriptRecordResult(
                callId,
                segment.Id,
                Published: false,
                Error: outboxMessage.LastError);
        }

        try
        {
            await publishEndpoint.Publish(transcriptEvent, cancellationToken);

            outboxMessage.Status = OutboxStatuses.Published;
            outboxMessage.PublishedAt = DateTimeOffset.UtcNow;
            outboxMessage.Attempts = Math.Max(outboxMessage.Attempts, 1);
            outboxMessage.LastError = null;
            await db.SaveChangesAsync(cancellationToken);

            return new TranscriptRecordResult(callId, segment.Id, Published: true, Error: null);
        }
        catch (Exception ex)
        {
            outboxMessage.Attempts = Math.Max(outboxMessage.Attempts, 0) + 1;
            outboxMessage.LastError = ex.Message;
            await db.SaveChangesAsync(cancellationToken);

            return new TranscriptRecordResult(callId, segment.Id, Published: false, Error: ex.Message);
        }
    }
}
