namespace SearchAFile.Web.Helpers;

public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public int StatusCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, string[]>? Errors { get; private set; }

    public static ApiResult<T> Success(T? data) => new()
    {
        IsSuccess = true,
        Data = data,
        StatusCode = 200
    };

    public static ApiResult<T> Failure(int statusCode, string? errorMessage, Dictionary<string, string[]>? errors = null) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        ErrorMessage = errorMessage,
        Errors = errors
    };
}