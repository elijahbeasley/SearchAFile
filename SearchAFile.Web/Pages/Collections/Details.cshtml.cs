using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.Diagnostics.Metrics;
using File = SearchAFile.Core.Domain.Entities.File;


namespace SearchAFile.Web.Pages.Collections;

public class DetailsModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public DetailsModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    public Collection Collection { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Collection Details");

            if (id == null)
                return NotFound();

            var collectionResult = await _api.GetAsync<Collection>($"collections/{id}");

            if (!collectionResult.IsSuccess || collectionResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(collectionResult) ?? "Unable to retrieve collection.");

            Collection = collectionResult.Data;

            var filesResult = await _api.GetAsync<List<File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(filesResult) ?? "Unable to retrieve collection.");

            List<File> Files = filesResult.Data;

            Collection.Files = Files
                .Where(file => file.CollectionId == Collection.CollectionId)
                .OrderBy(file => file.File1)
                .ToList();

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
