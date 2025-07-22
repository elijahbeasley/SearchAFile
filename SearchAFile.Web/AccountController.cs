using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;

namespace SearchAFile;

public class AccountController : Controller
{
    private readonly IHttpContextAccessor HttpContextAccessor;
    public AccountController(IHttpContextAccessor HCA)
    {
        HttpContextAccessor = HCA;
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> LogOut()
    {
        try
        {
            await HttpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear the session.
            HttpContextAccessor.HttpContext.Session.Clear();

            // Delete all cookies.
            if (HttpContextAccessor.HttpContext.Request != null)
            {
                foreach (var cookie in HttpContextAccessor.HttpContext.Request.Cookies.Where(cookie => cookie.Key.StartsWith("SearchAFile")))
                {
                    Web.Extensions.CookieExtensions.DeleteCookie(cookie.Key); // Delete each cookie by key
                }
            }
        }
        catch
        {
            throw;
        }

        return Redirect("/");
    }

    public async Task<bool> LogInUserAsync(User User)
    {
        bool booSuccess = true;

        try
        {
            // Sign out the current authentication cookie. 
            await HttpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear the current session.
            HttpContextAccessor.HttpContext.Session.Clear();

            // Set the staff object. 
            HttpContextAccessor.HttpContext.Session.SetObject("User", User);

            // Set the staff's role claim.
            List<Claim> objClaimList = new List<Claim>
            {
                new Claim(ClaimTypes.Role, User.Role),
            };

            // Save the role claim.
            ClaimsIdentity objClaimsIdentity = new ClaimsIdentity(objClaimList, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(objClaimsIdentity));

            // Save the staff role and dashboard session variables. 
            HttpContextAccessor.HttpContext.Session.SetString("Role", User.Role);
            HttpContextAccessor.HttpContext.Session.SetString("DashboardURL", SystemFunctions.GetDashboardURL(User.Role));

            switch (User.Role)
            {
                case "System Admin":

                    HttpContextAccessor.HttpContext.Session.SetBoolean("AllowUserImpersonation", true);
                    break;

                case "Global Admin":
                case "Country Admin":

                    HttpContextAccessor.HttpContext.Session.SetBoolean("AllowRoleImpersonation", true);
                    break;

                default:

                    break;
            }
        }
        catch
        {
            booSuccess = false;
            throw;
        }

        return booSuccess;
    }

    public async Task<IActionResult> EndUserImpersonationAsync(string CurrentPage = "~/")
    {
        string strMessage;

        try
        {
            User User = HttpContextAccessor.HttpContext.Session.GetObject<User>("User");
            strMessage = "Impersonation of " + User.FirstName + " " + User.LastName + " has ended.";

            User = HttpContextAccessor.HttpContext.Session.GetObject<User>("OriginalUser");

            if (User == null)
            {
                strMessage = "Unable to end user impersonation. User is null.";
                TempData["StartupJavaScript"] = "ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 10000, false)";

                return Redirect(CurrentPage);
            }
            else
            {
                // Log the staff member in. 
                await LogInUserAsync(User);

                // Reset the AllowUserImpersonation session variable. 
                HttpContextAccessor.HttpContext.Session.SetBoolean("AllowUserImpersonation", true);
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
