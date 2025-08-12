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
    private readonly AuthenticatedApiClient _api;
    private readonly AuthClient _loginService;
    public AccountController(IHttpContextAccessor httpContextAccessor, AuthenticatedApiClient api, AuthClient loginService)
    {
        _httpContextAccessor = httpContextAccessor;
        _api = api;
        _loginService = loginService;
    }

    public async Task<IActionResult> EndImpersonation(string CurrentPage = "~/")
    {
        string strMessage;

        try
        {
            UserDto? User = _httpContextAccessor?.HttpContext?.Session.GetObject<UserDto>("User");

            if (User == null)
            {
                strMessage = "Unable to end user impersonation. User is null.";
                TempData["StartupJavaScript"] = "ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 10000, false)";

                return Redirect(CurrentPage);
            }
            else
            {
                strMessage = "Impersonation of " + User?.FullName + " has ended.";

                User = _httpContextAccessor?.HttpContext?.Session.GetObject<UserDto>("OriginalUser");

                // Log the user in. 
                var loginResult = await _loginService.LoginAsync("", "", User.UserId);

                if (loginResult.Success)
                {
                    // Sign out of cookie authentication
                    _httpContextAccessor?.HttpContext?.Session.SetObject("User", User);
                    //_httpContextAccessor?.HttpContext?.Session.Remove("OriginalUser");

                    // Get the user's company.
                    var companyResult = await _api.GetAsync<Company>($"companies/{User?.CompanyId}");

                    if (!companyResult.IsSuccess || companyResult.Data == null)
                    {
                        throw new Exception(companyResult.ErrorMessage ?? "Unable to get the user's company.");
                    }

                    HttpContext.Session.SetObject("Company", companyResult.Data);

                    // Reset the AllowUserImpersonation session variable. 
                    _httpContextAccessor?.HttpContext?.Session.SetBoolean("AllowUserImpersonation", true);

                    TempData["StartupJavaScript"] = "ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true)";

                    return Redirect(SystemFunctions.GetDashboardURL(User?.Role));
                }
                else if (string.IsNullOrEmpty(loginResult.ErrorMessage))
                {
                    throw new Exception("Unable to end impersonation");
                }
                else
                {
                    throw new Exception(loginResult.ErrorMessage.EscapeJsString());
                }
            }
        }
        catch
        {
            throw;
        }
    }
}
