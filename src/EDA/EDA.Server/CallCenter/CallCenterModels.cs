namespace EDA.Server.CallCenter;

public sealed class CallSession
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartedAt { get; set; }
}

public sealed class TranscriptSegment
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class AgentDashboardProjection
{
    public Guid CallId { get; set; }
    public string LastSpeaker { get; set; } = string.Empty;
    public string LastSnippet { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SuggestionEntry
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = OutboxStatuses.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class ChatMessageProjection
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public Guid SegmentId { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}
