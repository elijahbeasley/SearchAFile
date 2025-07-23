using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Web.Extensions;
using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Web.Pages.Admins;

public class DashboardModel : PageModel
{
    private readonly TelemetryClient TelemetryClient;
    public DashboardModel(TelemetryClient TC)
    {
        TelemetryClient = TC;
    }
    public void OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Dashboard");

            UserDto User = HttpContext.Session.GetObject<UserDto>("User");

            // Set the message.
            HttpContext.Session.SetString("Message", "Hello " + User.FullName + "!");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }
}
