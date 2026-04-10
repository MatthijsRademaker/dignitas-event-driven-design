namespace EDA.Server.CallCenter.Contracts;

public sealed record CallStarted(
    Guid CallId,
    string AgentName,
    string CallerName,
    DateTimeOffset StartedAt);

public sealed record TranscriptStreaming(
    Guid CallId,
    Guid SegmentId,
    string Speaker,
    string Text,
    DateTimeOffset ReceivedAt);

public sealed record CallHeld(
    Guid CallId,
    DateTimeOffset HeldAt,
    string Reason);

public sealed record CallResumed(
    Guid CallId,
    DateTimeOffset ResumedAt);

public sealed record CallEnded(
    Guid CallId,
    DateTimeOffset EndedAt,
    string Reason);
