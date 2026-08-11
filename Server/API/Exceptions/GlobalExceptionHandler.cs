using FluentValidation;

namespace Server.API.Exceptions;


public sealed class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred.");

            if (ex is ValidationException validationException)
            {
                await context.Response.WriteAsJsonAsync(new ValidationErrorResponse
                {
                    Errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        ),
                    TraceId = context.TraceIdentifier
                });
            }
            else
            {
                context.Response.StatusCode = ex switch
                {
                    BadRequestException => StatusCodes.Status400BadRequest,
                    UnauthorizedException or UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    NotFoundException or KeyNotFoundException => StatusCodes.Status404NotFound,
                    ConflictException => StatusCodes.Status409Conflict,
                    InternalServerException => StatusCodes.Status500InternalServerError,
                    _ => StatusCodes.Status500InternalServerError
                };

                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    TraceId = context.TraceIdentifier
                });
            }
        }
    }
}

