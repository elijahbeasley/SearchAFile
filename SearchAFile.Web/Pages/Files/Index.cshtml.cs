using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mappers;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;
using System.Data;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Files;

public class IndexModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public IndexModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public List<File>? Files { get;set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain Files");

            string url = string.IsNullOrWhiteSpace(search)
                ? "files"
                : $"files?search={Uri.EscapeDataString(search)}";

            var fileGroupsResult = await _api.GetAsync<List<File>>(url);

            if (!fileGroupsResult.IsSuccess || fileGroupsResult.Data == null)
            {
                throw new Exception(fileGroupsResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            Files = fileGroupsResult.Data;

            ModelState.Remove("search");

            return Page();
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return NotFound();
        }
    }
}
