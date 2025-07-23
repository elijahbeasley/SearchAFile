using Microsoft.AspNetCore.Mvc;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Controllers;
public class AccountController : Controller
{
    private readonly AuthClient _loginService;

    public AccountController(AuthClient loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public async Task<IActionResult> LogOut()
    {
        // 1) Grab the menu cookie
        var menuValue = Request.Cookies["SearchAFile_menu"];

        // 2) Clear the session & sign out
        await _loginService.LogoutAsync();

        // 3) Re-append the menu cookie if it existed
        if (!string.IsNullOrEmpty(menuValue))
        {
            Response.Cookies.Append(
                "SearchAFile_menu",
                menuValue,
                new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                }
            );
        }

        // Redirect.
        return RedirectToAction("Default", "Home");
    }
}