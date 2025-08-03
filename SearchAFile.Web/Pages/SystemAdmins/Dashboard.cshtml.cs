using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.SystemAdmins;

public class DashboardModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public DashboardModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public List<Collection>? Collections { get; set; } = default!;
    public async Task OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Dashboard");

            string url = string.IsNullOrWhiteSpace(search)
                ? "collections"
                : $"collections?search={Uri.EscapeDataString(search)}";

            var collectionResult = await _api.GetAsync<List<Collection>>(url);

            if (!collectionResult.IsSuccess || collectionResult.Data == null)
            {
                throw new Exception(collectionResult.ErrorMessage ?? "Unable to retrieve collection.");
            }

            Collections = collectionResult.Data
                .Where(collection => collection.CompanyId == HttpContext.Session.GetObject<Company>("Company").CompanyId)
                .OrderBy(collection => collection.Collection1)
                .ToList();

            var filesResult = await _api.GetAsync<List<Core.Domain.Entities.File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            List<Core.Domain.Entities.File> Files = filesResult.Data;

            CollectionFileCountMapper.MapFilesCountToCollections(Collections, Files);
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
