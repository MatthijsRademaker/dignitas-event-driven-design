using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter;

public sealed class TranscriptRecorder(CallCenterDbContext db, IPublishEndpoint publishEndpoint)
{
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

        db.TranscriptSegments.Add(segment);
        await db.SaveChangesAsync(cancellationToken);

        if (request.SimulatePublishFailure)
        {
            return new TranscriptRecordResult(
                callId,
                segment.Id,
                Published: false,
                Error: "Simulated publish failure.");
        }

        await publishEndpoint.Publish(
            new TranscriptReceived(callId, segment.Id, speaker, text, segment.ReceivedAt),
            cancellationToken);

        return new TranscriptRecordResult(callId, segment.Id, Published: true, Error: null);
    }
}
