using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Exceptions;

namespace YouthDataManager.WebApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,"Concurrent operation conflicted");
            context.Response.StatusCode=409;
            await context.Response.WriteAsJsonAsync(new { message="The record changed. Refresh before retrying." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627 or 1205)
        {
            _logger.LogWarning(ex,"Duplicate or concurrent operation conflicted");
            context.Response.StatusCode=409;
            await context.Response.WriteAsJsonAsync(new { message="A matching operation already exists or changed. Refresh before retrying." });
        }
        catch (SqlException ex) when(ex.Number is 1205 or 2601 or 2627 or 51001)
        {
            _logger.LogWarning(ex,"Concurrent database operation conflicted");context.Response.StatusCode=409;
            await context.Response.WriteAsJsonAsync(new {message="The operation conflicted. Refresh and retry."});
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(ex, "Domain validation error");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
        }
    }
}
