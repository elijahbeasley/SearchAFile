using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Reflection;
using SearchAFile.Web.Extensions;

namespace SearchAFile.Pages.Home;

public class AccessDeniedModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    public AccessDeniedModel(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    public void OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Access Denied");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }
}
