using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Common.Errors;
public sealed class ApiErrorResultFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var status = context.Result switch
        {
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            ObjectResult { Value: not ApiErrorResponse, StatusCode: not null } objectResult =>
                objectResult.StatusCode.Value,
            _ => 0
        };

        if (status is >= 400 and < 500)
        {
            var message = status == StatusCodes.Status404NotFound
                ? "Resource not found"
                : "Request failed";
            context.Result = new ObjectResult(
                ApiErrorResponseFactory.Create(context.HttpContext, status, message))
            {
                StatusCode = status
            };
        }

        return next();
    }
}