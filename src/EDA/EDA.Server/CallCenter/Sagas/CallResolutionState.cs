using MassTransit;

namespace EDA.Server.CallCenter.Sagas;

public sealed class CallResolutionState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid CallId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public int TranscriptSegments { get; set; }
}
