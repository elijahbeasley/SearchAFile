using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Home;

public class DefaultModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    public DefaultModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        bool booSuccess = true;

        try
        {
            // Delete all cookies.
            if (Request != null)
            {
                foreach (var cookie in Request.Cookies.Where(e => e.Key.StartsWith("SearchAFile")))
                {
                    Web.Extensions.CookieExtensions.DeleteCookie(cookie.Key); // Delete each cookie by key
                }
            }

            if (HttpContext.Session.GetObject<SystemInfo>("SystemInfo") == null)
            {
                var result = await _api.GetAsync<IEnumerable<SystemInfo>>("systeminfos");

                if (!result.IsSuccess || result.Data == null)
                {
                    throw new Exception(result.ErrorMessage ?? "Unable to initiate the system.");
                }

                SystemInfo SystemInfo = result.Data.ElementAt(0);

                if (SystemInfo != null)
                {
                    HttpContext.Session.SetObject("SystemInfo", SystemInfo);
                    HttpContext.Session.SetString("ContactInfo", SystemInfo.ContactName + " at <a href='mailto:" + SystemInfo.ContactEmailAddress + "?subject=" + SystemInfo.SystemName + " Error'>" + SystemInfo.ContactEmailAddress + "</a>");
                }
                else
                {
                    booSuccess = false;
                }
            }
        }
        catch (Exception ex)
        {
            booSuccess = false;

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Redirect("~/Home/SystemDown");
        }

        if (booSuccess)
        {
            return Redirect("~/Home/LogIn");
        }
        else
        {
            return Page();
        }
    }
}
