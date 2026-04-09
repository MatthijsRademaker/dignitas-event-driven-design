using EDA.Server.CallCenter.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDA.Server.CallCenter.Consumers;

public sealed class SuggestionProjector(CallCenterDbContext db) : IConsumer<TranscriptReceived>
{
    public async Task Consume(ConsumeContext<TranscriptReceived> context)
    {
        var message = context.Message;

        var existing = await db.Suggestions
            .Where(entry => entry.CallId == message.CallId)
            .ToListAsync(context.CancellationToken);

        if (existing.Count > 0)
        {
            db.Suggestions.RemoveRange(existing);
        }

        var suggestions = SuggestionGenerator.FromText(message.Text);
        var createdAt = DateTimeOffset.UtcNow;

        foreach (var suggestion in suggestions)
        {
            db.Suggestions.Add(new SuggestionEntry
            {
                Id = Guid.NewGuid(),
                CallId = message.CallId,
                Text = suggestion.Text,
                Category = suggestion.Category,
                CreatedAt = createdAt
            });
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
