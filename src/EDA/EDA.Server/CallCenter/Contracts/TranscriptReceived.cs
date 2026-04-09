namespace EDA.Server.CallCenter.Contracts;

public sealed record TranscriptReceived(
    Guid CallId,
    Guid SegmentId,
    string Speaker,
    string Text,
    DateTimeOffset ReceivedAt);
