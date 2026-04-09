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
    DashboardView? Dashboard,
    IReadOnlyList<SuggestionView> Suggestions)
{
    public static DemoState From(
        CallSession? call,
        IReadOnlyList<TranscriptSegment> transcripts,
        AgentDashboardProjection? dashboard,
        IReadOnlyList<SuggestionEntry> suggestions)
    {
        return new DemoState(
            CallSummary.From(call),
            transcripts.Select(TranscriptView.From).ToList(),
            DashboardView.From(dashboard),
            suggestions.Select(SuggestionView.From).ToList());
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
