using System.Diagnostics;
using FluentValidation;

namespace Server.API.Exceptions;

public class ComponentExceptionHandler(ILogger<ComponentExceptionHandler> logger)
{
    public async Task<ComponentResult<T>> RunAsync<T>(Func<Task<T>> operation, Func<Task>? finallyAction = null)
    {
        try
        {
            T result = await operation();
            return ComponentResult<T>.Ok(result);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Component operation was cancelled.");
            return ComponentResult<T>.Canceled();
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
        finally
        {
            if (finallyAction is not null)
            {
                try
                {
                    await finallyAction();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Finally action failed in component operation.");
                }
            }
        }
    }

    private ComponentResult<T> HandleException<T>(Exception ex)
    {
        logger.LogError(ex, "An exception occured in component operation.");

        string traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (ex is ValidationException validationException)
        {
            ValidationErrorResponse validation = new()
            {
                Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    ),
                TraceId = traceId
            };

            return ComponentResult<T>.FromValidation(validation);
        }

        int status = ex switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            UnauthorizedException or UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotFoundException or KeyNotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            InternalServerException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        ErrorResponse error = new()
        {
            StatusCode = status,
            Message = ex.Message,
            TraceId = traceId
        };

        return ComponentResult<T>.FromError(error);
    }
}