using CleanPro.Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace CleanPro.WebAPI.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToProblemDetails(result.Error);
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToProblemDetails(result.Error);
    }

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        string routeName,
        object routeValues)
    {
        if (result.IsSuccess)
        {
            return new CreatedAtRouteResult(routeName, routeValues, result.Value);
        }

        return ToProblemDetails(result.Error);
    }

    private static IActionResult ToProblemDetails(Error error)
    {
        var statusCode = GetStatusCode(error.Code);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = error.Message,
            Extensions = { ["errorCode"] = error.Code }
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            _ when errorCode.Contains("NotFound") => StatusCodes.Status404NotFound,
            _ when errorCode.Contains("Conflict") || errorCode.Contains("Exists") => StatusCodes.Status409Conflict,
            _ when errorCode.Contains("Validation") => StatusCodes.Status400BadRequest,
            _ when errorCode.Contains("Unauthorized") => StatusCodes.Status401Unauthorized,
            _ when errorCode.Contains("Forbidden") => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Error"
        };
    }
}
