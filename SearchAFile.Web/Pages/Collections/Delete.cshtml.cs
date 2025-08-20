using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.Net.Http;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Collections;

public class DeleteModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly AuthenticatedApiClient _api;
    private readonly HttpClient _httpClient;

    public DeleteModel(TelemetryClient telemetryClient, IWebHostEnvironment iWebHostEnvironment, AuthenticatedApiClient api, IHttpClientFactory httpClient)
    {
        _telemetryClient = telemetryClient;
        _iWebHostEnvironment = iWebHostEnvironment;
        _api = api;
        _httpClient = httpClient.CreateClient("SearchAFileClient");
    }

    [BindProperty]
    public Collection Collection { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Delete Collection");

            if (id == null)
                return NotFound();

            var result = await _api.GetAsync<Collection>($"collections/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve collection.");

            Collection = result.Data;

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

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            // Delete all the associated files.
            var filesResult = await _api.GetAsync<List<File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            List<File> Files = filesResult.Data
                .Where(file => file.CollectionId == id)
                .ToList();

            foreach (File File in Files)
            {
                // Get the OpenAIFileID.
                var fileResult = await _api.GetAsync<File>($"files/{File.FileId}");

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
                var deleteResult = await _api.DeleteAsync<object>($"files/{File.FileId}");

                if (!deleteResult.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(deleteResult) ?? "Unable to delete file.");
            }

            var result = await _api.DeleteAsync<object>($"collections/{id}");

            if (!result.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to delete collection.");

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Collection successfully deleted.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Collection NOT successfully deleted. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
