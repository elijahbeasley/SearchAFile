using System.Net;

public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public int StatusCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, string[]>? Errors { get; private set; }

    // ✅ Add this:
    public HttpResponseMessage? RawResponse { get; private set; }

    public static ApiResult<T> Success(T? data, HttpResponseMessage? response = null) => new()
    {
        IsSuccess = true,
        Data = data,
        StatusCode = (int)(response?.StatusCode ?? HttpStatusCode.OK),
        RawResponse = response
    };

    public static ApiResult<T> Failure(int statusCode, string? errorMessage, Dictionary<string, string[]>? errors = null, HttpResponseMessage? response = null) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        ErrorMessage = errorMessage,
        Errors = errors,
        RawResponse = response
    };
}