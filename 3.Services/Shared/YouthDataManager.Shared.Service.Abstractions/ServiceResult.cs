using System.Collections.Generic;
using System.Linq;

namespace YouthDataManager.Shared.Service.Abstractions;

public enum ServiceResultStatus
{
    Success,
    Failure
}

public record ServiceError(string Code, string? Description = null);

public class ServiceResult
{
    public ServiceResultStatus Status { get; }
    public string? Message { get; }
    public IReadOnlyList<ServiceError> Errors { get; }

    protected ServiceResult(ServiceResultStatus status, string? message = null, IEnumerable<ServiceError>? errors = null)
    {
        Status = status;
        Message = message;
        Errors = errors?.ToList() ?? new List<ServiceError>();
    }

    public static ServiceResult Success() => new(ServiceResultStatus.Success);
    public static ServiceResult Failure(string message, params ServiceError[] errors) 
        => new(ServiceResultStatus.Failure, message, errors);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    private ServiceResult(ServiceResultStatus status, T? data = default, string? message = null, IEnumerable<ServiceError>? errors = null)
        : base(status, message, errors)
    {
        Data = data;
    }

    public static ServiceResult<T> Success(T data) => new(ServiceResultStatus.Success, data);
    public new static ServiceResult<T> Failure(string message, params ServiceError[] errors) 
        => new(ServiceResultStatus.Failure, default, message, errors);
}
