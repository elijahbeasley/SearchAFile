using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;

namespace SearchAFile.Web.Services;
public class LoginService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }


    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("account/login", new
        {
            Email = email,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            // Try to read error message from the API response body
            var error = await response.Content.ReadAsStringAsync();
            return new LoginResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Login failed." : error
            };
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();

        _httpContextAccessor.HttpContext?.Session.SetString("JWT", result!.Token!);
        _httpContextAccessor.HttpContext?.Session.SetObject("User", result!.User!);

        return new LoginResult { Success = true };
    }

}
