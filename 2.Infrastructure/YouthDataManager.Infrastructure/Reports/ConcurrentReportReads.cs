using System.Collections.Concurrent;
using YouthDataManager.Reports.Service.Abstractions.DTOs;

namespace YouthDataManager.Infrastructure.Reports;

// Share only overlapping identical reads. Completed results are never cached,
// so recording/correcting a meeting needs no cache invalidation or expiry delay.
internal static class ConcurrentReportReads
{
    private static readonly ConcurrentDictionary<string,Lazy<Task<ReportTable>>> Reads=new();
    public static async Task<ReportTable> Run(string key,Func<Task<ReportTable>> read,CancellationToken cancellation)
    {
        while(true)
        {
            Lazy<Task<ReportTable>> candidate=null!;
            candidate=new(async()=>{
                try{return await read();}
                finally{((ICollection<KeyValuePair<string,Lazy<Task<ReportTable>>>>)Reads).Remove(new(key,candidate));}
            });
            var pending=Reads.GetOrAdd(key,candidate);
            try{return ReferenceEquals(pending,candidate) ? await pending.Value : await pending.Value.WaitAsync(cancellation);}
            // A different caller can cancel the shared SQL work. Remaining callers
            // repeat a fresh scoped read with their own cancellation context.
            catch(OperationCanceledException) when(!cancellation.IsCancellationRequested){}
        }
    }
}
