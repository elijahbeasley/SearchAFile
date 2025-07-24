using SearchAFile.Web.Helpers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchAFile.Web.Services;

public class AuthenticatedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public AuthenticatedApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["ApiAuth:ClientId"] ?? throw new ArgumentNullException("ClientId not configured");
        _clientSecret = configuration["ApiAuth:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret not configured");
    }

    private void AddAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Client-Id", _clientId);
        request.Headers.Add("X-Client-Secret", _clientSecret);
    }

    //public async Task<ApiResult<T>> GetAsync<T>(string url)
    //{
    //    var request = new HttpRequestMessage(HttpMethod.Get, url);
    //    AddAuthHeaders(request);

    //    var response = await _httpClient.SendAsync(request);
    //    var content = await response.Content.ReadAsStringAsync();

    //    try
    //    {
    //        if (response.IsSuccessStatusCode)
    //        {
    //            // Try to deserialize directly into T (your actual object)
    //            var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
    //            {
    //                PropertyNameCaseInsensitive = true
    //            });

    //            return ApiResult<T>.Success(data);
    //        }
    //        else
    //        {
    //            // Try to extract error from JSON like: { message: "...", detail: "..." }
    //            var errorObj = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
    //            {
    //                PropertyNameCaseInsensitive = true
    //            });

    //            return ApiResult<T>.Failure((int)response.StatusCode, errorObj?.Message ?? "Unknown error", null);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return ApiResult<T>.Failure((int)response.StatusCode, $"Deserialization error: {ex.Message}");
    //    }
    //}

    //private class ApiErrorResponse
    //{
    //    public string? Message { get; set; }
    //    public Dictionary<string, string[]>? Errors { get; set; }
    //}


    public async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>();
            return ApiResult<T>.Success(data);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResult<T>.Failure((int)response.StatusCode, errorContent);
        }
    }

    public async Task<ApiResult<T>> PostAsync<T>(string url, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(request);
        request.Content = JsonContent.Create(data);

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return ApiResult<T>.Success(result);
        }

        // Try to pull out validation or general error messages
        try
        {
            var errorObj = JsonSerializer.Deserialize<ApiErrorHelper.ApiErrorResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return ApiResult<T>.Failure((int)response.StatusCode, null, errorObj?.Errors);
        }
        catch
        {
            return ApiResult<T>.Failure((int)response.StatusCode, "Failed to parse API error.");
        }
    }

    public async Task<HttpResponseMessage> PostAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        AddAuthHeaders(request);

        request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        AddAuthHeaders(request);

        return await _httpClient.SendAsync(request);
    }
}