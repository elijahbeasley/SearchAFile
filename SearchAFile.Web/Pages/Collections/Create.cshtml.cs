using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.IdentityModel.Abstractions;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SearchAFile.Web.Pages.Collections;

[BindProperties]
public class CreateModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly AuthenticatedApiClient _api;
    private readonly IOpenAIVectorStoreService _vectorStores; // <— add this

    public CreateModel(
        TelemetryClient telemetryClient,
        IWebHostEnvironment iWebHostEnvironment,
        AuthenticatedApiClient api,
        IOpenAIVectorStoreService vectorStores) // <— add this
    {
        _telemetryClient = telemetryClient;
        _iWebHostEnvironment = iWebHostEnvironment;
        _api = api;
        _vectorStores = vectorStores;
    }

    public Collection Collection { get; set; } = default!;

    public IFormFile? IFormFile { get; set; }

    private List<string> CollectionTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public IActionResult OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Create Collection");

            ModelState.Remove("IFormFile");
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // ============================================================
            // 0) QUICK VALIDATION
            // ============================================================
            if (Collection == null)
            {
                ModelState.AddModelError("", "Invalid request. No collection payload was provided.");
                return Page();
            }

            // ============================================================
            // 1) OPTIONAL IMAGE UPLOAD (keeps your existing helper)
            // ============================================================
            string collectionsRoot = Path.Combine(_iWebHostEnvironment.WebRootPath, "Collections");

            if (IFormFile != null)
            {
                bool imageSuccess = await FileUploadHelper.TryUploadFileAsync(
                    IFormFile,
                    "Image",
                    collectionsRoot,
                    CollectionTypes,
                    fileName => Collection.ImageUrl = fileName);

                if (!imageSuccess)
                    throw new Exception("Unable to upload the image.");
            }

            // ============================================================
            // 2) SANITIZE + METADATA
            // ============================================================
            var sanitizer = new Ganss.Xss.HtmlSanitizer();

            // Sanitize any user-entered strings
            Collection.Collection1 = sanitizer.Sanitize(Collection.Collection1?.Trim() ?? string.Empty);

            // Stamp metadata from session (server-authoritative)
            var company = HttpContext.Session.GetObject<Company>("Company");
            var user = HttpContext.Session.GetObject<UserDto>("User");

            Collection.CompanyId = company.CompanyId;
            Collection.Created = DateTime.Now;
            Collection.CreatedByUserId = user.UserId;

            // New collection shouldn't carry IDs yet
            Collection.OpenAiVectorStoreId = null;

            // ============================================================
            // 3) CREATE COLLECTION IN YOUR API (we need the new ID)
            // ============================================================
            var createResult = await _api.PostAsync<Collection>("collections", Collection);
            if (!createResult.IsSuccess || createResult.Data == null)
            {
                ApiErrorHelper.AddErrorsToModelState(createResult, ModelState, "Collection");

                string err = ApiErrorHelper.GetErrorString(createResult);
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>"
                    + err.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                return Page();
            }

            var created = createResult.Data; // has CollectionId & CompanyId

            // ============================================================
            // 4) CREATE OPENAI VECTOR STORE (via your service)
            //    Use metadata to make audits/searching easier.
            // ============================================================
            var vsId = await _vectorStores.CreateAsync(
                name: $"Collection {created.CollectionId}",
                metadata: new Dictionary<string, string>
                {
                    ["collectionId"] = created.CollectionId.ToString(),
                    ["companyId"] = created.CompanyId.ToString(),
                    ["createdBy"] = user.UserId.ToString()
                },
                expiresAfterDays: null,              // keep alive indefinitely (or set a policy)
                ct: CancellationToken.None);

            if (string.IsNullOrWhiteSpace(vsId))
            {
                // Rollback collection if vector store creation failed
                await _api.DeleteAsync<object>($"collections/{created.CollectionId}");
                throw new Exception("Failed to create OpenAI Vector Store. The collection was rolled back.");
            }

            // ============================================================
            // 5) SAVE VECTOR STORE ID BACK TO COLLECTION
            // ============================================================
            created.OpenAiVectorStoreId = vsId;

            var updateResult = await _api.PutAsync<object>($"collections/{created.CollectionId}", created);
            if (!updateResult.IsSuccess)
            {
                // (Optional best-effort) Try to delete the vector store we just created so we don’t orphan it.
                try
                { /* if your service exposes a delete, use it; otherwise raw HTTP in service */
                    // Example (if you add DeleteAsync in the service later):
                    // await _vectorStores.DeleteAsync(vsId);
                }
                catch (Exception cleanupEx)
                {
                    // Log but don’t hide the real error
                    _telemetryClient.TrackException(new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(cleanupEx)
                    { SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning });
                }

                // Roll back the API record for consistency
                await _api.DeleteAsync<object>($"collections/{created.CollectionId}");

                string err = ApiErrorHelper.GetErrorString(updateResult) ?? "Unable to update collection with vector store ID.";
                throw new Exception(err);
            }

            // ============================================================
            // 6) SUCCESS
            // ============================================================
            TempData["StartupJavaScript"] = "ShowSnack('success', 'Collection successfully created.', 7000, true)";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Collection NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }

    //public async Task<IActionResult> OnPostAsync()
    //{
    //    try
    //    {
    //        // Create a new OpenAI Assistant and get the Assistant ID to put in the Collection.


    //        string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Collections");

    //        if (IFormFile != null)
    //        {
    //            bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFile, "Image", strPath, CollectionTypes, fileName => Collection.ImageUrl = fileName);
    //            if (!headerSuccess) throw new Exception("Unable to upload the image.");
    //        }

    //        // Sanitize the data.
    //        HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
    //        Collection.Collection1 = objHtmlSanitizer.Sanitize(Collection.Collection1.Trim());

    //        Collection.CompanyId = HttpContext.Session.GetObject<Company>("Company").CompanyId;
    //        Collection.Created = DateTime.Now;
    //        Collection.CreatedByUserId = HttpContext.Session.GetObject<UserDto>("User").UserId;

    //        var result = await _api.PostAsync<Collection>("collections", Collection);

    //        if (!result.IsSuccess)
    //        {
    //            ApiErrorHelper.AddErrorsToModelState(result, ModelState, "Collection");

    //            string strExceptionMessage = ApiErrorHelper.GetErrorString(result);
    //            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
    //            return Page();
    //        }

    //        TempData["StartupJavaScript"] = "ShowSnack('success', 'Collection successfully created.', 7000, true)";

    //        return RedirectToPage("./Index");
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        _telemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "Collection NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

    //        return Page();
    //    }
    //}
}
