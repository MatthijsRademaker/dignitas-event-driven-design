namespace EDA.OutboxWorker;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Poll the outbox table and publish pending messages.
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
