using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Mappers;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.Data;
using System.Net.Http;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Files;

public class IndexModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly AuthenticatedApiClient _api;
    private readonly IOpenAIVectorStoreService _vectorStores;
    private readonly IOpenAIFileService _openAIFileService;

    public IndexModel(
        TelemetryClient telemetryClient, 
        IConfiguration configuration, 
        IWebHostEnvironment iWebHostEnvironment,
        AuthenticatedApiClient api,
        IOpenAIVectorStoreService vectorStores,
        IOpenAIFileService openAIFileService)
    {
        _telemetryClient = telemetryClient;
        _configuration = configuration;
        _iWebHostEnvironment = iWebHostEnvironment;
        _api = api;
        _vectorStores = vectorStores;
        _openAIFileService = openAIFileService;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public Collection Collection { get; set; }
    public List<File>? Files { get;set; } = default!;
    public int MaxFilesAllowed { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return Redirect("Collections");

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain Files");

            MaxFilesAllowed = _configuration.GetValue<int>("OpenAI:MaxFilesAllowed");

            var collectionResult = await _api.GetAsync<Collection>($"collections/{id}");

            if (!collectionResult.IsSuccess || collectionResult.Data == null)
            {
                throw new Exception(collectionResult.ErrorMessage ?? "Unable to retrieve collection.");
            }

            Collection = collectionResult.Data;

            string url = string.IsNullOrWhiteSpace(search)
                ? "files"
                : $"files?search={Uri.EscapeDataString(search)}";

            var filesResult = await _api.GetAsync<List<File>>(url);

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            Files = filesResult.Data
                .Where(file => file.CollectionId == id)
                .OrderBy(file => file.File1)
                .ToList();

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

            return Redirect("Collections");
        }
    }

    public async Task<IActionResult> OnGetDeleteAsync(Guid? id, CancellationToken ct = default)
    {
        try
        {
            if (id is null) return NotFound();

            // 1) Load file record (has OpenAIFileId and CollectionId)
            var fileRes = await _api.GetAsync<File>($"files/{id}");
            if (!fileRes.IsSuccess || fileRes.Data is null)
                throw new Exception(fileRes.ErrorMessage ?? "Unable to retrieve file.");

            var file = fileRes.Data;

            // 2) Load collection to get its Vector Store id (for detach)
            var collRes = await _api.GetAsync<Collection>($"collections/{file.CollectionId}");
            if (!collRes.IsSuccess || collRes.Data is null)
                throw new Exception(collRes.ErrorMessage ?? "Unable to retrieve collection.");

            var collection = collRes.Data;

            // 3) Detach from vector store (if we have both ids)
            if (!string.IsNullOrWhiteSpace(collection.OpenAiVectorStoreId) &&
                !string.IsNullOrWhiteSpace(file.OpenAIFileId))
            {
                // ignore 404s: file might already be gone or not attached
                try
                {
                    await _vectorStores.DetachFileAsync(collection.OpenAiVectorStoreId!, file.OpenAIFileId!, ct);
                }
                catch (Exception) { /* log if you want; keep delete best-effort */ }
            }

            // 4) Delete from OpenAI Files (global)
            if (!string.IsNullOrWhiteSpace(file.OpenAIFileId))
            {
                try
                {
                    await _openAIFileService.DeleteAsync(file.OpenAIFileId!, ct);
                }
                catch (Exception) { /* log; continue */ }
            }

            // 5) Delete local physical file (build absolute path safely)
            if (!string.IsNullOrWhiteSpace(file.Path))
            {
                // If Path is already absolute, use it; otherwise resolve under wwwroot/Files
                var absolutePath = Path.IsPathFullyQualified(file.Path)
                    ? file.Path
                    : Path.Combine(_iWebHostEnvironment.WebRootPath, "Files", file.Path);

                if (System.IO.File.Exists(absolutePath))
                    System.IO.File.Delete(absolutePath);
            }

            // 6) Delete DB record
            var delRes = await _api.DeleteAsync<object>($"files/{id}");
            if (!delRes.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(delRes) ?? "Unable to delete file.");

            TempData["StartupJavaScript"] = "window.top.ShowSnack('success', 'File successfully deleted.', 7000, true)";
            return new JsonResult(new { success = true }) { StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error });
            string msg = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " +
                         (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + msg.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
            return NotFound();
        }
    }
}
