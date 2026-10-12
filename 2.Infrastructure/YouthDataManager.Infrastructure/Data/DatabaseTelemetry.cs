using System.Data.Common;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace YouthDataManager.Infrastructure.Data;

public sealed class DatabaseTelemetry(ILogger<DatabaseTelemetry> logger) : DbCommandInterceptor
{
    private static readonly Meter Meter=new("YouthDataManager.Database");
    private static readonly Histogram<double> Duration=Meter.CreateHistogram<double>("ydm.database.duration","ms");
    private static readonly Counter<long> Slow=Meter.CreateCounter<long>("ydm.database.slow_queries");
    private static readonly Counter<long> Errors=Meter.CreateCounter<long>("ydm.database.errors");
    private long _queries,_slow,_errors;
    public object Snapshot() => new {Queries=Interlocked.Read(ref _queries),SlowQueries=Interlocked.Read(ref _slow),Errors=Interlocked.Read(ref _errors)};
    private void Record(CommandExecutedEventData data)
    {
        Interlocked.Increment(ref _queries);Duration.Record(data.Duration.TotalMilliseconds);
        if(data.Duration.TotalMilliseconds<500)return;
        Interlocked.Increment(ref _slow);Slow.Add(1);
        // SQL text/parameters can contain private fields; log only duration and EF command identity.
        logger.LogWarning("Slow database command {CommandId}: {DurationMs} ms",data.CommandId,data.Duration.TotalMilliseconds);
    }
    public override DbDataReader ReaderExecuted(DbCommand command,CommandExecutedEventData data,DbDataReader result){Record(data);return result;}
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,CommandExecutedEventData data,DbDataReader result,CancellationToken ct=default){Record(data);return ValueTask.FromResult(result);}
    public override int NonQueryExecuted(DbCommand command,CommandExecutedEventData data,int result){Record(data);return result;}
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command,CommandExecutedEventData data,int result,CancellationToken ct=default){Record(data);return ValueTask.FromResult(result);}
    public override object? ScalarExecuted(DbCommand command,CommandExecutedEventData data,object? result){Record(data);return result;}
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command,CommandExecutedEventData data,object? result,CancellationToken ct=default){Record(data);return ValueTask.FromResult(result);}
    public override void CommandFailed(DbCommand command,CommandErrorEventData data){Interlocked.Increment(ref _errors);Errors.Add(1);}
    public override Task CommandFailedAsync(DbCommand command,CommandErrorEventData data,CancellationToken ct=default){CommandFailed(command,data);return Task.CompletedTask;}
}
