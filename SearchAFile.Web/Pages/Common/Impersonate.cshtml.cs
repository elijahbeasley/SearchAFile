using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using SearchAFile.Core.Domain.Entities;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ImpersonateModel : PageModel
{
    //private readonly TelemetryClient _telemetryClient;
    //private readonly AuthenticatedApiClient _api;
    //private readonly AccountController AccountController;

    //public ImpersonateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, AccountController AC)
    //{
    //    _telemetryClient = telemetryClient;
    //    _api = api;
    //    AccountController = AC;
    //}
    //public IList<User> UserIList { get; set; }

    //[DisplayName("User")]
    //[Required(ErrorMessage = "User is required.")]
    //public int UserID { get; set; }
    //public string Search { get; set; }

    //public async Task<IActionResult> OnGetAsync()
    //{
    //    if (!Convert.ToBoolean(HttpContext.Session.GetBoolean("AllowUserImpersonation")))
    //    {
    //        HttpContext.Session.Clear();
    //        return Redirect("~/");
    //    }

    //    try
    //    {
    //        // Set the page title.
    //        HttpContext.Session.SetString("PageTitle", "Impersonate User");

    //        await BuildUserSelectList();

    //        ModelState.Remove("UserID");
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
    //    }

    //    return Page();
    //}

    //public async Task<IActionResult> OnPostAsync()
    //{
    //    string strMessage;

    //    try
    //    {
    //        await BuildUserSelectList();

    //        User User = await SearchAFileContext.User
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.UserId == UserID);

    //        if (User == null)
    //        {
    //            strMessage = "Unable to log in as selected user with ID " + UserID + ".";
    //            TempData["StartupJavaScript"] = "ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 10000, false)";
    //        }
    //        else
    //        {
    //            // Check to see if there is an OriginalUser variable that needs to be saved. 
    //            User OriginalUser = null;

    //            if (HttpContext.Session.GetObject<Models.User>("OriginalUser") == default)
    //            {
    //                OriginalUser = HttpContext.Session.GetObject<Models.User>("User");
    //            }
    //            else
    //            {
    //                OriginalUser = HttpContext.Session.GetObject<Models.User>("OriginalUser");
    //            }

    //            // Log the user member in. 
    //            await AccountController.LogInUserAsync(User);

    //            // Reset the AllowUserImpersonation session variable. 
    //            HttpContext.Session.SetBoolean("AllowUserImpersonation", true);

    //            // If the selected user member is different from the OriginalUser member then set the OriginalUser variable. 
    //            if (User.UserId != OriginalUser.UserId)
    //            {
    //                HttpContext.Session.SetObject("OriginalUser", OriginalUser);
    //            }

    //            strMessage = "You are now impersonating " + User.FullName + ".";
    //            TempData["StartupJavaScript"] = "ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true)";

    //            return Redirect(SystemFunctions.GetDashboardURL(User.Role));
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
    //    }

    //    return Page();
    //}

    //public async Task<IActionResult> OnGetReloadUser(string strSearch)
    //{
    //    try
    //    {
    //        if (string.IsNullOrEmpty(strSearch))
    //        {
    //            Search = "";
    //        }
    //        else
    //        {
    //            Search = HttpUtility.UrlDecode(strSearch).Trim();
    //        }

    //        await BuildUserSelectList();
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");

    //        return StatusCode(400, strExceptionMessage);
    //    }

    //    return Page();
    //}

    //private async Task BuildUserSelectList()
    //{
    //    try
    //    {
    //        UserIList = await SearchAFileContext.User
    //            .Include(e => e.Country)
    //            .Where(e => (string.IsNullOrEmpty(Search) 
    //                || (e.FirstName + " " + e.LastName).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.LastName + ", " + e.FirstName).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.Role).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.Country.Country1).Trim().ToLower().Contains(Search.Trim().ToLower()))
    //                && e.UserId != HttpContext.Session.GetObject<User>("User").UserId)
    //            .OrderBy(e => e.LastName)
    //            .ThenBy(e => e.FirstName)
    //            .AsNoTracking()
    //            .ToListAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
    //    }
    //}
}