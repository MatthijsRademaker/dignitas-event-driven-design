#if SOLUTION
using EDA.Server.CallCenter.Contracts;
using MassTransit;

namespace EDA.Server.CallCenter.Sagas;

public sealed class CallResolutionSaga : MassTransitStateMachine<CallResolutionState>
{
    public State InProgress { get; private set; } = null!;
    public State OnHold { get; private set; } = null!;
    public State Ended { get; private set; } = null!;

    public Event<CallStarted> CallStarted { get; private set; } = null!;
    public Event<TranscriptStreaming> TranscriptStreaming { get; private set; } = null!;
    public Event<CallHeld> CallHeld { get; private set; } = null!;
    public Event<CallResumed> CallResumed { get; private set; } = null!;
    public Event<CallEnded> CallEnded { get; private set; } = null!;

    public CallResolutionSaga()
    {
        InstanceState(state => state.CurrentState);

        Event(() => CallStarted, configurator =>
        {
            configurator.CorrelateById(context => context.Message.CallId);
            configurator.SelectId(context => context.Message.CallId);
        });

        Event(() => TranscriptStreaming, configurator =>
            configurator.CorrelateById(context => context.Message.CallId));

        Event(() => CallHeld, configurator =>
            configurator.CorrelateById(context => context.Message.CallId));

        Event(() => CallResumed, configurator =>
            configurator.CorrelateById(context => context.Message.CallId));

        Event(() => CallEnded, configurator =>
            configurator.CorrelateById(context => context.Message.CallId));

        Initially(
            When(CallStarted)
                .Then(context =>
                {
                    context.Saga.CallId = context.Message.CallId;
                    context.Saga.StartedAt = context.Message.StartedAt;
                    context.Saga.LastUpdatedAt = context.Message.StartedAt;
                    context.Saga.TranscriptSegments = 0;
                })
                .TransitionTo(InProgress));

        During(InProgress,
            When(TranscriptStreaming)
                .Then(context =>
                {
                    context.Saga.TranscriptSegments++;
                    context.Saga.LastUpdatedAt = context.Message.ReceivedAt;
                }));

        During(InProgress,
            When(CallHeld)
                .Then(context =>
                {
                    context.Saga.LastUpdatedAt = context.Message.HeldAt;
                })
                .TransitionTo(OnHold));

        During(OnHold,
            Ignore(TranscriptStreaming));

        During(OnHold,
            When(CallResumed)
                .Then(context =>
                {
                    context.Saga.LastUpdatedAt = context.Message.ResumedAt;
                })
                .TransitionTo(InProgress));

        DuringAny(
            When(CallEnded)
                .Then(context =>
                {
                    context.Saga.LastUpdatedAt = context.Message.EndedAt;
                })
                .TransitionTo(Ended)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
#endif
