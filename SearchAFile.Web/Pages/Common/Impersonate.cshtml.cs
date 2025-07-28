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

namespace SearchAFile.Pages.Common;

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
    //public IList<Staff> StaffIList { get; set; }

    //[DisplayName("Staff")]
    //[Required(ErrorMessage = "Staff is required.")]
    //public int StaffID { get; set; }
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

    //        await BuildStaffSelectList();

    //        ModelState.Remove("StaffID");
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
    //        await BuildStaffSelectList();

    //        Staff Staff = await SearchAFileContext.Staff
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.StaffId == StaffID);

    //        if (Staff == null)
    //        {
    //            strMessage = "Unable to log in as selected user with ID " + StaffID + ".";
    //            TempData["StartupJavaScript"] = "ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 10000, false)";
    //        }
    //        else
    //        {
    //            // Check to see if there is an OriginalStaff variable that needs to be saved. 
    //            Staff OriginalStaff = null;

    //            if (HttpContext.Session.GetObject<Models.Staff>("OriginalStaff") == default)
    //            {
    //                OriginalStaff = HttpContext.Session.GetObject<Models.Staff>("Staff");
    //            }
    //            else
    //            {
    //                OriginalStaff = HttpContext.Session.GetObject<Models.Staff>("OriginalStaff");
    //            }

    //            // Log the staff member in. 
    //            await AccountController.LogInStaffAsync(Staff);

    //            // Reset the AllowUserImpersonation session variable. 
    //            HttpContext.Session.SetBoolean("AllowUserImpersonation", true);

    //            // If the selected staff member is different from the OriginalStaff member then set the OriginalStaff variable. 
    //            if (Staff.StaffId != OriginalStaff.StaffId)
    //            {
    //                HttpContext.Session.SetObject("OriginalStaff", OriginalStaff);
    //            }

    //            strMessage = "You are now impersonating " + Staff.FirstName + " " + Staff.LastName + ".";
    //            TempData["StartupJavaScript"] = "ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true)";

    //            return Redirect(SystemFunctions.GetDashboardURL(Staff.Role));
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

    //public async Task<IActionResult> OnGetReloadStaff(string strSearch)
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

    //        await BuildStaffSelectList();
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

    //private async Task BuildStaffSelectList()
    //{
    //    try
    //    {
    //        StaffIList = await SearchAFileContext.Staff
    //            .Include(e => e.Country)
    //            .Where(e => (string.IsNullOrEmpty(Search) 
    //                || (e.FirstName + " " + e.LastName).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.LastName + ", " + e.FirstName).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.Role).Trim().ToLower().Contains(Search.Trim().ToLower())
    //                || (e.Country.Country1).Trim().ToLower().Contains(Search.Trim().ToLower()))
    //                && e.StaffId != HttpContext.Session.GetObject<Staff>("Staff").StaffId)
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