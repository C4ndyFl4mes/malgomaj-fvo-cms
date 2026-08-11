namespace Server.API.Exceptions;

public record ErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = "An unexpected error occurred.";
    public string? TraceId { get; init; }
}

public record ValidationErrorResponse
{
    public int StatusCode { get; init; } = StatusCodes.Status400BadRequest;
    public string Message { get; init; } = "One or more validation errors occured.";
    public Dictionary<string, string[]> Errors { get; init; } = new();
    public string? TraceId { get; init; }
}

public record ComponentResult<T>
{
    public bool IsSuccess { get; set; }
    public bool IsCanceled { get; set; }
    public T? Value { get; set; }
    public ErrorResponse? Error { get; set; }
    public ValidationErrorResponse? ValidationError { get; set; }

    public static ComponentResult<T> Ok(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static ComponentResult<T> FromError(ErrorResponse error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public static ComponentResult<T> FromValidation(ValidationErrorResponse validation) => new()
    {
        IsSuccess = false,
        ValidationError = validation
    };

    public static ComponentResult<T> Canceled() => new()
    {
        IsSuccess = false,
        IsCanceled = true
    };
}