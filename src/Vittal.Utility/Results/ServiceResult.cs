using System;
using System.Collections.Generic;

namespace Vittal.Utility.Results;

public class ServiceResult
{
    public bool IsSuccess { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public ServiceErrorType ErrorType { get; protected set; }
    public List<string> Errors { get; protected set; } = new();

    public static ServiceResult Success(string message = "Operación exitosa")
    {
        return new ServiceResult { IsSuccess = true, Message = message };
    }

    public static ServiceResult Failure(string message, ServiceErrorType errorType = ServiceErrorType.InternalError, List<string>? errors = null)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            ErrorType = errorType,
            Errors = errors ?? new List<string>()
        };
    }
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public static ServiceResult<T> Success(T data, string message = "Operación exitosa")
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public new static ServiceResult<T> Failure(string message, ServiceErrorType errorType = ServiceErrorType.InternalError, List<string>? errors = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            ErrorType = errorType,
            Errors = errors ?? new List<string>(),
            Data = default
        };
    }
}
