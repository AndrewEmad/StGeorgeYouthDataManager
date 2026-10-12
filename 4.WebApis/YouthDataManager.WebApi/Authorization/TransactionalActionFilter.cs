using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Controllers;

namespace YouthDataManager.WebApi.Authorization;

public sealed class TransactionalActionFilter(IApplicationStore store) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) || context.Controller is AuthController)
        { await next(); return; }
        await store.Transaction(async () => await next(), result =>
            result.Exception == null && (result.Result as IStatusCodeActionResult)?.StatusCode is not >= 400);
    }
}
