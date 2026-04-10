using MassTransit;

namespace EDA.Server.CallCenter.Sagas;

public sealed class CallResolutionSaga : MassTransitStateMachine<CallResolutionState>
{
    public State InProgress { get; private set; } = null!;
    public State SampleState { get; private set; } = null!;

    public Event<SampleEvent> SampleEvent { get; private set; } = null!;

    public CallResolutionSaga()
    {
        InstanceState(state => state.CurrentState);

        Event(
            () => SampleEvent,
            configurator =>
            {
                configurator.CorrelateById(context => context.Message.CallId);
                configurator.SelectId(context => context.Message.CallId);
                // Docs: https://masstransit.io/documentation/configuration/sagas/statemachine
            }
        );

        // TODO: Define call lifecycle events (start, hold, resume, hang up, transcript).
        // TODO: Add states for InProgress, OnHold, Ended.
        // TODO: Ignore TranscriptStreaming while in the OnHold state.
    }
}

public sealed record SampleEvent(Guid CallId);
