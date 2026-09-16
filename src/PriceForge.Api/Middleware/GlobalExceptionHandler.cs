using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.Common.Exceptions;

namespace PriceForge.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        int status = exception switch
        {
            ValidationException         => StatusCodes.Status400BadRequest,
            BusinessRuleException       => StatusCodes.Status400BadRequest,
            KeyNotFoundException        => StatusCodes.Status404NotFound,
            NotFoundException           => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ConflictException           => StatusCodes.Status409Conflict,
            ForbiddenException          => StatusCodes.Status403Forbidden,
            TooManyRequestsException    => StatusCodes.Status429TooManyRequests,
            DomainException             => StatusCodes.Status400BadRequest,
            _                           => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = status, Title = "An error occurred", Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
