namespace CommonHall.Application.Common;

public sealed record ApiResponse<T>
{
    public T? Data { get; init; }
    public List<ApiError>? Errors { get; init; }
    public ApiMeta? Meta { get; init; }

    public static ApiResponse<T> Success(T data, ApiMeta? meta = null) =>
        new() { Data = data, Meta = meta };

    public static ApiResponse<T> Failure(List<ApiError> errors) =>
        new() { Errors = errors };

    public static ApiResponse<T> Failure(string code, string message, string? field = null) =>
        new() { Errors = [new ApiError(code, message, field)] };
}

public sealed record ApiError(string Code, string Message, string? Field = null);

public sealed record ApiMeta
{
    public int? Total { get; init; }
    public bool? HasMore { get; init; }
    public string? NextCursor { get; init; }
}
