namespace VpnPlatform.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public bool IsRetryable { get; init; }
    public string? Error { get; init; }
    public T? Value { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error, bool isRetryable = false) => new() { IsSuccess = false, IsRetryable = isRetryable, Error = error };
}
