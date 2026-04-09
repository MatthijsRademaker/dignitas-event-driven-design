namespace EDA.Server.CallCenter;

public sealed class OutboxDispatchRunner
{
    public Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        // TODO: Dispatch pending outbox messages.
        return Task.CompletedTask;
    }
}
