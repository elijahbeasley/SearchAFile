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
        // Logout
        await _loginService.LogoutAsync();

        // Redirect.
        return RedirectToAction("Default", "Home");
    }
}