using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Pages.Home;

public class DefaultModel : PageModel
{
    private readonly TelemetryClient TelemetryClient;
    private readonly AuthenticatedApiClient _api;
    public DefaultModel(TelemetryClient TC, AuthenticatedApiClient api)
    {
        TelemetryClient = TC;
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
                var SystemInfos = await _api.GetAsync<IEnumerable<SystemInfo>>("systeminfos");

                if (SystemInfos != null
                    && SystemInfos.Any())
                {

                    SystemInfo SystemInfo = SystemInfos.ElementAt(0);

                    if (SystemInfo != null)
                    {
                        HttpContext.Session.SetObject("SystemInfo", SystemInfo);
                        HttpContext.Session.SetString("ContactInfo", SystemInfo.ContactName + " at <a href='mailto:" + SystemInfo.ContactEmailAddress + "?subject=" + SystemInfo.SystemName + " Error'>" + SystemInfo.ContactEmailAddress + "</a>");
                    }
                    else
                    {
                        HttpContext.Session.SetString("Message", "Unable to initialize system.");
                        HttpContext.Session.SetString("MessageColor", "red");
                        booSuccess = false;
                    }
                }
                else
                {
                    HttpContext.Session.SetString("Message", "Unable to initialize system.");
                    HttpContext.Session.SetString("MessageColor", "red");
                    booSuccess = false;
                }
            }
        }
        catch (Exception ex)
        {
            booSuccess = false;

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
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
