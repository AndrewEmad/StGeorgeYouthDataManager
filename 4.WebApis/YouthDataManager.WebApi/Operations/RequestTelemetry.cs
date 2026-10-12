using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace YouthDataManager.WebApi.Operations;

public sealed class RequestTelemetry
{
    private static readonly Meter Meter=new("YouthDataManager.Api");
    private static readonly Histogram<double> Duration=Meter.CreateHistogram<double>("ydm.http.duration","ms");
    private static readonly Counter<long> Errors=Meter.CreateCounter<long>("ydm.http.errors");
    private long _requests,_errors;
    public object Snapshot()=>new {Requests=Interlocked.Read(ref _requests),Errors=Interlocked.Read(ref _errors)};
    public void Record(HttpContext context,TimeSpan elapsed)
    {
        Interlocked.Increment(ref _requests);
        var route=(context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        Duration.Record(elapsed.TotalMilliseconds,new KeyValuePair<string,object?>("route",route),new("status",context.Response.StatusCode));
        if(context.Response.StatusCode>=500){Interlocked.Increment(ref _errors);Errors.Add(1,new KeyValuePair<string,object?>("route",route));}
    }
}
public sealed class RequestTelemetryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,RequestTelemetry telemetry)
    {
        var start=Stopwatch.GetTimestamp();try{await next(context);}finally{telemetry.Record(context,Stopwatch.GetElapsedTime(start));}
    }
}
