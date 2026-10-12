using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.WebApi.BackgroundWorkers;

public sealed class NotificationReminderWorker(IServiceScopeFactory scopes,ILogger<NotificationReminderWorker> logger,IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!configuration.GetValue("NotificationWorker:Enabled",true))return;
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using(var scope=scopes.CreateScope())await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().EnqueueOccurrences(DateTime.UtcNow,stoppingToken);
                Guid[] ids;
                using(var scope=scopes.CreateScope())ids=await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().Due(DateTime.UtcNow,stoppingToken);
                await Parallel.ForEachAsync(ids,new ParallelOptions {MaxDegreeOfParallelism=Math.Clamp(configuration.GetValue("NotificationWorker:Concurrency",4),1,8),CancellationToken=stoppingToken},async(id,ct)=>{
                    using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().Deliver(id,DateTime.UtcNow,ct);
                });
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Notification cycle failed; pending deliveries remain retryable.");}
            try{await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("NotificationWorker:IntervalSeconds",30),5,300)),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
