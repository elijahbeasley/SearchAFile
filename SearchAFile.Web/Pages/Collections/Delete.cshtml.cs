using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
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
    private readonly IOpenAIFileService _openAiFiles;                // NEW
    private readonly IOpenAIVectorStoreService _vectorStores;        // NEW

    public DeleteModel(
        TelemetryClient telemetryClient,
        IWebHostEnvironment iWebHostEnvironment,
        AuthenticatedApiClient api,
        IOpenAIFileService openAiFiles,               // NEW
        IOpenAIVectorStoreService vectorStores)       // NEW
    {
        _telemetryClient = telemetryClient;
        _iWebHostEnvironment = iWebHostEnvironment;
        _api = api;
        _openAiFiles = openAiFiles;
        _vectorStores = vectorStores;
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

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        try
        {
            if (id == null) return NotFound();

            // 1) Load collection (to get OpenAiVectorStoreId)
            var collectionResult = await _api.GetAsync<Collection>($"collections/{id}");
            if (!collectionResult.IsSuccess || collectionResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(collectionResult) ?? "Unable to load the collection.");
            var collection = collectionResult.Data;

            // 2) Load files (prefer a dedicated endpoint like GET collections/{id}/files if you have it)
            var filesResult = await _api.GetAsync<List<File>>("files");
            if (!filesResult.IsSuccess || filesResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(filesResult) ?? "Unable to retrieve files.");
            var filesForCollection = filesResult.Data.Where(f => f.CollectionId == id).ToList();

            // 2a) Delete each file: OpenAI → local disk → DB
            foreach (var file in filesForCollection)
            {
                // A) OpenAI file delete (service has headers + 404 handling)
                if (!string.IsNullOrWhiteSpace(file.OpenAIFileId))
                    await _openAiFiles.DeleteAsync(file.OpenAIFileId);

                // B) Local file system delete (best-effort)
                if (!string.IsNullOrWhiteSpace(file.Path))
                {
                    var physicalPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Files", file.Path);
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                // C) DB record delete
                var dbDelete = await _api.DeleteAsync<object>($"files/{file.FileId}");
                if (!dbDelete.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(dbDelete) ?? $"Unable to delete file record {file.FileId}.");
            }

            // 3) Delete vector store (via service)
            if (!string.IsNullOrWhiteSpace(collection.OpenAiVectorStoreId))
                await _vectorStores.DeleteAsync(collection.OpenAiVectorStoreId);

            // 4) Delete the collection record
            var collectionDelete = await _api.DeleteAsync<object>($"collections/{id}");
            if (!collectionDelete.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(collectionDelete) ?? "Unable to delete collection.");

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

    //public async Task<IActionResult> OnPostAsync(Guid? id)
    //{
    //    try
    //    {
    //        if (id == null)
    //            return NotFound();

    //        // Instead of deleting all the associated Open AI Files, just delete the whole OpenAI Assistant?



    //        // Delete all the associated files.
    //        var filesResult = await _api.GetAsync<List<File>>("files");

    //        if (!filesResult.IsSuccess || filesResult.Data == null)
    //        {
    //            throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
    //        }

    //        List<File> Files = filesResult.Data
    //            .Where(file => file.CollectionId == id)
    //            .ToList();

    //        foreach (File File in Files)
    //        {
    //            // Get the OpenAIFileID.
    //            var fileResult = await _api.GetAsync<File>($"files/{File.FileId}");

    //            if (!fileResult.IsSuccess || fileResult.Data == null)
    //            {
    //                throw new Exception(fileResult.ErrorMessage ?? "Unable to retrieve file.");
    //            }

    //            string OpenAIFileID = fileResult.Data.OpenAIFileId;

    //            if (string.IsNullOrEmpty(OpenAIFileID))
    //            {
    //                throw new Exception("Unable to retrieve the OpenAI file ID.");
    //            }

    //            // Delete the file from OpenAI.
    //            var response = await _httpClient.DeleteAsync($"https://api.openai.com/v1/files/{OpenAIFileID}");

    //            if (!response.IsSuccessStatusCode)
    //            {
    //                var error = await response.Content.ReadAsStringAsync();
    //                throw new Exception($"Failed to delete OpenAI file: {error}");
    //            }

    //            // Delete the file from local storage.
    //            if (!string.IsNullOrEmpty(fileResult.Data.Path))
    //            {
    //                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Files", fileResult.Data.Path);

    //                // Delete the old tile image from the folder.
    //                string strDeletePath = Path.Combine(strPath, strPath);

    //                if (System.IO.File.Exists(strDeletePath))
    //                {
    //                    System.IO.File.Delete(strDeletePath);
    //                }
    //            }

    //            // Delete the file from our DB.
    //            var deleteResult = await _api.DeleteAsync<object>($"files/{File.FileId}");

    //            if (!deleteResult.IsSuccess)
    //                throw new Exception(ApiErrorHelper.GetErrorString(deleteResult) ?? "Unable to delete file.");
    //        }

    //        var result = await _api.DeleteAsync<object>($"collections/{id}");

    //        if (!result.IsSuccess)
    //            throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to delete collection.");

    //        TempData["StartupJavaScript"] = "ShowSnack('success', 'Collection successfully deleted.', 7000, true)";

    //        return RedirectToPage("./Index");
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "Collection NOT successfully deleted. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

    //        return Page();
    //    }
    //}
}