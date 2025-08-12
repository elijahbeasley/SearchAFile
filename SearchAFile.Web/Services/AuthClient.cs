using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using System.Security.Claims;

namespace SearchAFile.Web.Services;
public class AuthClient
{
    private readonly AuthenticatedApiClient _api;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthClient(AuthenticatedApiClient api, IHttpContextAccessor httpContextAccessor)
    {
        _api = api;
        _httpContextAccessor = httpContextAccessor;
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public async Task<LoginResult> LoginAsync(string email, string password, Guid? id = null)
    {
        try
        {
            if (id == null)
            {
                var response = await _api.PostAsync<AuthResult>("account/login", new
                {
                    Email = email,
                    Password = password
                });

                if (!response.IsSuccess || response.Data == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                            ? "Login failed."
                            : response.ErrorMessage
                    };
                }

                var result = response.Data;

                if (!result.Success || result.User == null || string.IsNullOrWhiteSpace(result.Token))
                {
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Login failed: Invalid server response."
                    };
                }

                // Store token and user in session
                _httpContextAccessor.HttpContext?.Session.SetString("JWT", result.Token);
                _httpContextAccessor.HttpContext?.Session.SetObject("User", result.User);

                // Add claims and sign in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                    new Claim(ClaimTypes.Name, result.User.FullName ?? ""),
                    new Claim(ClaimTypes.Email, result.User.EmailAddress),
                    new Claim(ClaimTypes.Role, result.User.Role ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await _httpContextAccessor.HttpContext!.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                    });
            }
            else
            {
                var result = await _api.GetAsync<UserDto>($"users/{id}");

                if (!result.IsSuccess || result.Data == null)
                    throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve user.");

                UserDto User = result.Data;

                // Store token and user in session
                _httpContextAccessor.HttpContext?.Session.SetObject("User", User);

                // Add claims and sign in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, User.UserId.ToString()),
                    new Claim(ClaimTypes.Name, User.FullName ?? ""),
                    new Claim(ClaimTypes.Email, User.EmailAddress),
                    new Claim(ClaimTypes.Role, User.Role ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await _httpContextAccessor.HttpContext!.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                    });
            }

            return new LoginResult { Success = true };
        }
        catch (Exception ex)
        {
            string strExceptionMessage = "An error occured. Please report the following error to " + _httpContextAccessor?.HttpContext?.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            return new LoginResult { Success = false, ErrorMessage = strExceptionMessage };
        }
    }

    public async Task LogoutAsync()
    {
        // Optional: call the API logout endpoint
        try
        {
            var result = await _api.PostAsync<object>("account/logout");

            if (!result.IsSuccess)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to delete user.");
            }
        }
        catch
        {
            // Log failure but proceed with local logout anyway
        }

        if (_httpContextAccessor.HttpContext != null)
        {
            // Clear session data
            _httpContextAccessor.HttpContext.Session.Clear();

            // Sign out of cookie authentication
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
