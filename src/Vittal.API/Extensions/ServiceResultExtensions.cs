using Microsoft.AspNetCore.Mvc;
using Vittal.API.Models;
using Vittal.Utility.Results;

namespace Vittal.API.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new ApiResponse<T>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            });
        }

        var response = new ApiResponse<T>
        {
            Success = false,
            Message = result.Message,
            Errors = result.Errors
        };

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => new NotFoundObjectResult(response),
            ServiceErrorType.Validation => new BadRequestObjectResult(response),
            ServiceErrorType.Unauthorized => new UnauthorizedObjectResult(response),
            ServiceErrorType.Forbidden => new ObjectResult(response) { StatusCode = 403 },
            ServiceErrorType.Conflict => new ConflictObjectResult(response),
            _ => new ObjectResult(response) { StatusCode = 500 }
        };
    }

    public static IActionResult ToActionResult(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new ApiResponse
            {
                Success = true,
                Message = result.Message
            });
        }

        var response = new ApiResponse
        {
            Success = false,
            Message = result.Message,
            Errors = result.Errors
        };

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => new NotFoundObjectResult(response),
            ServiceErrorType.Validation => new BadRequestObjectResult(response),
            ServiceErrorType.Unauthorized => new UnauthorizedObjectResult(response),
            ServiceErrorType.Forbidden => new ObjectResult(response) { StatusCode = 403 },
            ServiceErrorType.Conflict => new ConflictObjectResult(response),
            _ => new ObjectResult(response) { StatusCode = 500 }
        };
    }
}
