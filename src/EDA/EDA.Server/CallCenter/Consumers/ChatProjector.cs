using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter.Consumers;

public sealed class ChatProjector(CallCenterDbContext db) : IConsumer<TranscriptReceived>
{
    public async Task Consume(ConsumeContext<TranscriptReceived> context)
    {
        var message = context.Message;

        var exists = await db.ChatMessages
            .AnyAsync(entry => entry.SegmentId == message.SegmentId, context.CancellationToken);

        if (exists)
        {
            return;
        }

        db.ChatMessages.Add(new ChatMessageProjection
        {
            Id = message.SegmentId,
            SegmentId = message.SegmentId,
            CallId = message.CallId,
            Speaker = message.Speaker,
            Text = message.Text,
            ReceivedAt = message.ReceivedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
