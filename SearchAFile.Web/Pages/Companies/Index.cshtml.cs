using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Companies;

public class IndexModel : PageModel
{
    private readonly TelemetryClient TelemetryClient;
    private readonly AuthenticatedApiClient _api;

    public IndexModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        TelemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public IList<Company>? Companies { get;set; } = default!;

    public async Task OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain Companies");

            string url = string.IsNullOrWhiteSpace(search)
                ? "companies"
                : $"companies?search={Uri.EscapeDataString(search)}";

            var result = await _api.GetAsync<List<Company>>(url);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to retrieve company.");
            }

            Companies = result.Data;

            ModelState.Remove("search");
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
