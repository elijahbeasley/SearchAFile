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

    public async Task<T?> GetAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }

        return default;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeaders(request);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

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