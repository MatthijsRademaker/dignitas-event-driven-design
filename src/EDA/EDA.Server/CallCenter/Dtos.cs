namespace EDA.Server.CallCenter;

public sealed record TranscriptRequest(
    Guid? CallId,
    string Speaker,
    string Text,
    bool SimulatePublishFailure);

public sealed record TranscriptRecordResult(
    Guid CallId,
    Guid SegmentId,
    bool Published,
    string? Error);

public sealed record DemoState(
    CallSummary? Call,
    IReadOnlyList<TranscriptView> Transcripts,
    IReadOnlyList<ChatMessageView> Chat,
    DashboardView? Dashboard,
    IReadOnlyList<SuggestionView> Suggestions,
    OutboxSnapshot Outbox)
{
    public static DemoState From(
        CallSession? call,
        IReadOnlyList<TranscriptSegment> transcripts,
        IReadOnlyList<ChatMessageProjection> chat,
        AgentDashboardProjection? dashboard,
        IReadOnlyList<SuggestionEntry> suggestions,
        OutboxSnapshot outbox)
    {
        return new DemoState(
            CallSummary.From(call),
            transcripts.Select(TranscriptView.From).ToList(),
            chat.Select(ChatMessageView.From).ToList(),
            DashboardView.From(dashboard),
            suggestions.Select(SuggestionView.From).ToList(),
            outbox);
    }
}

public sealed record CallSummary(
    Guid Id,
    string AgentName,
    string CallerName,
    string Status,
    DateTimeOffset StartedAt)
{
    public static CallSummary? From(CallSession? call)
    {
        if (call is null)
        {
            return null;
        }

        return new CallSummary(call.Id, call.AgentName, call.CallerName, call.Status, call.StartedAt);
    }
}

public sealed record TranscriptView(
    Guid Id,
    string Speaker,
    string Text,
    DateTimeOffset ReceivedAt)
{
    public static TranscriptView From(TranscriptSegment segment)
    {
        return new TranscriptView(segment.Id, segment.Speaker, segment.Text, segment.ReceivedAt);
    }
}

public sealed record ChatMessageView(
    Guid Id,
    string Speaker,
    string Text,
    DateTimeOffset ReceivedAt)
{
    public static ChatMessageView From(ChatMessageProjection entry)
    {
        return new ChatMessageView(entry.Id, entry.Speaker, entry.Text, entry.ReceivedAt);
    }
}

public sealed record DashboardView(
    string LastSpeaker,
    string LastSnippet,
    int SegmentCount,
    DateTimeOffset UpdatedAt)
{
    public static DashboardView? From(AgentDashboardProjection? projection)
    {
        if (projection is null)
        {
            return null;
        }

        return new DashboardView(
            projection.LastSpeaker,
            projection.LastSnippet,
            projection.SegmentCount,
            projection.UpdatedAt);
    }
}

public sealed record SuggestionView(
    Guid Id,
    string Text,
    string Category,
    DateTimeOffset CreatedAt)
{
    public static SuggestionView From(SuggestionEntry entry)
    {
        return new SuggestionView(entry.Id, entry.Text, entry.Category, entry.CreatedAt);
    }
}

public sealed record OutboxSnapshot(
    int Pending,
    int Published,
    int Failed)
{
    public static OutboxSnapshot From(IEnumerable<OutboxMessage> messages)
    {
        var pending = 0;
        var published = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            switch (message.Status)
            {
                case OutboxStatuses.Pending:
                    pending++;
                    break;
                case OutboxStatuses.Published:
                    published++;
                    break;
                case OutboxStatuses.Failed:
                    failed++;
                    break;
            }
        }

        return new OutboxSnapshot(pending, published, failed);
    }
}
