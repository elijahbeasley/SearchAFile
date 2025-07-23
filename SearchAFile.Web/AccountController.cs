using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using System.Security.Claims;

namespace SearchAFile;

public class AccountController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AccountController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> EndUserImpersonationAsync(string CurrentPage = "~/")
    {
        string strMessage;

        try
        {
            UserDto User = _httpContextAccessor.HttpContext.Session.GetObject<UserDto>("User");
            strMessage = "Impersonation of " + User.FullName + " has ended.";

            if (User == null)
            {
                strMessage = "Unable to end user impersonation. User is null.";
                TempData["StartupJavaScript"] = "ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 10000, false)";

                return Redirect(CurrentPage);
            }
            else
            {
                // Sign out of cookie authentication
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                User = _httpContextAccessor.HttpContext.Session.GetObject<UserDto>("OriginalUser");
                _httpContextAccessor.HttpContext.Session.SetObject("User", User);

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

                // Reset the AllowUserImpersonation session variable. 
                _httpContextAccessor.HttpContext.Session.SetBoolean("AllowUserImpersonation", true);

                TempData["StartupJavaScript"] = "ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 7000, true)";

                return Redirect(SystemFunctions.GetDashboardURL(User.Role));
            }
        }
        catch
        {
            throw;
        }
    }
}
