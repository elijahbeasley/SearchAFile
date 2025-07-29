using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SearchAFile.Web.Services;

public class AuthenticatedApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public AuthenticatedApiClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _clientId = config["ApiAuth:ClientId"] ?? throw new ArgumentNullException("ClientId");
        _clientSecret = config["ApiAuth:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret");
    }

    /// <summary>Adds custom headers to the request.</summary>
    private void AddAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Client-Id", _clientId);
        request.Headers.Add("X-Client-Secret", _clientSecret);
    }

    /// <summary>Handles deserialization and error extraction from an HTTP response.</summary>
    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResult<T>.Success(default, response); // e.g., null
            }

            try
            {
                object data;

                if (typeof(T) == typeof(string))
                {
                    data = content.Trim('"');
                }
                else if (typeof(T) == typeof(bool))
                {
                    data = bool.Parse(content);
                }
                else if (typeof(T) == typeof(int))
                {
                    data = int.Parse(content);
                }
                else if (typeof(T) == typeof(Guid))
                {
                    data = Guid.Parse(content);
                }
                else if (typeof(T).IsEnum)
                {
                    data = Enum.Parse(typeof(T), content, ignoreCase: true);
                }
                else
                {
                    data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return ApiResult<T>.Success((T)data, response);
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Failure((int)response.StatusCode, $"Failed to deserialize success response: {ex.Message}", null, response);
            }
        }

        // Error handling logic (leave as-is)
        try
        {
            var errorObj = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return ApiResult<T>.Failure((int)response.StatusCode, errorObj?.Message ?? "Unknown error", errorObj?.Errors, response);
        }
        catch
        {
            return ApiResult<T>.Failure((int)response.StatusCode, "Failed to parse API error.", null, response);
        }
    }

    private class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    // ----------------------------
    // HTTP METHODS
    // ----------------------------

    public async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<ApiResult<T>> PostAsync<T>(string url, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data)
        };
        AddAuthHeaders(request);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<ApiResult<T>> PostAsync<T>(string url) =>
        await PostAsync<T>(url, new { });

    public async Task<ApiResult<T>> PutAsync<T>(string url, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<ApiResult<T>> PutAsync<T>(string url) =>
        await PutAsync<T>(url, new { });

    public async Task<ApiResult<T>> DeleteAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        AddAuthHeaders(request);
        var response = await _httpClient.SendAsync(request);
        return await HandleResponseAsync<T>(response);
    }
}