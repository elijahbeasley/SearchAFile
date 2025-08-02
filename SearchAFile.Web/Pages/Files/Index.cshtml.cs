using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
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
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly AuthenticatedApiClient _api;
    private readonly HttpClient _httpClient;

    public IndexModel(TelemetryClient telemetryClient, IWebHostEnvironment iWebHostEnvironment, AuthenticatedApiClient api, IHttpClientFactory httpClient)
    {
        _telemetryClient = telemetryClient;
        _iWebHostEnvironment = iWebHostEnvironment;
        _api = api;
        _httpClient = httpClient.CreateClient("SearchAFIleClient");
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public FileGroup FileGroup { get; set; }
    public List<File>? Files { get;set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return Redirect("FileGroups");

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain Files");

            var fileGroupResult = await _api.GetAsync<FileGroup>($"filegroups/{id}");

            if (!fileGroupResult.IsSuccess || fileGroupResult.Data == null)
            {
                throw new Exception(fileGroupResult.ErrorMessage ?? "Unable to retrieve file group.");
            }

            FileGroup = fileGroupResult.Data;

            string url = string.IsNullOrWhiteSpace(search)
                ? "files"
                : $"files?search={Uri.EscapeDataString(search)}";

            var filesResult = await _api.GetAsync<List<File>>(url);

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            Files = filesResult.Data.Where(file => file.FileGroupId == id).OrderBy(file => file.File1).ToList();

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

            return Redirect("FileGroups");
        }
    }

    public async Task<IActionResult> OnGetDeleteAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            // Get the OpenAIFileID.
            var fileResult = await _api.GetAsync<File>($"files/{id}");

            if (!fileResult.IsSuccess || fileResult.Data == null)
            {
                throw new Exception(fileResult.ErrorMessage ?? "Unable to retrieve file.");
            }

            string OpenAIFileID = fileResult.Data.OpenAIFileId;

            if (string.IsNullOrEmpty(OpenAIFileID))
            {
                throw new Exception("Unable to retrieve the OpenAI file ID.");
            }

            // Delete the file from OpenAI.
            var response = await _httpClient.DeleteAsync($"https://api.openai.com/v1/files/{OpenAIFileID}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete OpenAI file: {error}");
            }

            // Delete the file from local storage.
            if (!string.IsNullOrEmpty(fileResult.Data.Path))
            {
                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Files", fileResult.Data.Path);

                // Delete the old tile image from the folder.
                string strDeletePath = Path.Combine(strPath, strPath);

                if (System.IO.File.Exists(strDeletePath))
                {
                    System.IO.File.Delete(strDeletePath);
                }
            }

            // Delete the file from our DB.
            var result = await _api.DeleteAsync<object>($"files/{id}");

            if (!result.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to delete file.");

            TempData["StartupJavaScript"] = "window.top.ShowSnack('success', 'File successfully deleted.', 7000, true)";

            return new JsonResult(new { success = true }) { StatusCode = 200 };
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
