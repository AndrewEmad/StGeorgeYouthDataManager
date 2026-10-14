using YouthDataManager.Shared.Service.Abstractions;
namespace YouthDataManager.WebApi.Middleware;

// Read scope only. Commands and background workers always see complete history.
public sealed class ServiceYearMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if(HttpMethods.IsGet(context.Request.Method) && context.Request.Path.StartsWithSegments("/api") && context.Request.Query.TryGetValue("serviceYear",out var value))
        {
            if(value.Count==1 && value[0]=="all")
            {
                context.Items["AllServiceYears"]=true;
                await next(context);return;
            }
            if(value.Count!=1 || !int.TryParse(value[0],out var year) || year is <1900 or >9997)
            {
                context.Response.StatusCode=400;
                await context.Response.WriteAsJsonAsync(new {message="عام الخدمة غير صحيح."},context.RequestAborted);
                return;
            }
            context.Items["ServiceYear"]=ServiceYear.From(year);
        }
        await next(context);
    }
}
