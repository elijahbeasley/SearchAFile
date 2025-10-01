using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Web.Classes;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Files;

[BindProperties(SupportsGet = true)]
public class CreateModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IConfiguration _configuration;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly IOpenAIFileService _openAIFileService;          // interface
    private readonly IOpenAIVectorStoreService _vectorStores;        // interface

    public CreateModel(TelemetryClient telemetryClient, 
        IConfiguration configuration,
        AuthenticatedApiClient api, 
        IWebHostEnvironment iWebHostEnvironment,
        IOpenAIFileService openAIFileService,            // interface here
        IOpenAIVectorStoreService vectorStores)
    {
        _telemetryClient = telemetryClient;
        _configuration = configuration;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _openAIFileService = openAIFileService;
        _vectorStores = vectorStores;
    }

    public FileUploaderOptions FileUploaderOptions { get; set; }
    private int maxLength = DataAnnotationHelpers.GetMaxLenFromAttr<File>(f => f.File1);
    private string? _collectionVectorStoreId; // cache for this request

    public Guid? Id { get; set; }
    public int MaxFilesAllowed { get; set; }

    public List<IFormFile> Uploads { get; set; } = new();

    public List<string> FileTypes = new List<string>()
    {
        "pdf","docx","csv","txt","md"
    };

    public async Task<IActionResult> OnGet(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            Id = id;
            // Get the number of files already in existance.
            var filesResult = await _api.GetAsync<List<File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            var filesCountResult = await _api.GetAsync<int>($"files/filescount?id={id}");
            if (!filesCountResult.IsSuccess || filesCountResult.Data == null)
            {
                throw new Exception(filesCountResult.ErrorMessage ?? "Unable to retrieve files count.");
            }
            int filesCount = filesCountResult.Data;

            int maxFilesAllowed = _configuration.GetValue<int>("OpenAI:MaxFilesAllowed");

            FileUploaderOptions = new FileUploaderOptions
            {
                DomId = "uploaderA",
                InputName = "Uploads",
                AcceptedTypes = new[] { ".pdf", ".docx", ".csv", ".txt", ".md" },
                PerFileLimitMB = 10,
                TotalLimitMB = 25,
                MaxFiles = maxFilesAllowed,
                AlreadyUploadedCount = filesCount,
                MaxFilenameLength = maxLength,
                TruncateLongFilenames = true, 
                ShowDiagnostics = false
            };

            ModelState.Remove("IFormFile");

            TempData["StartupJavaScript"] = "if (self !== top) { window.top.StopLoading('#divLoadingBlock'); window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowModal(); }";
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlock'); window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
            
            return NotFound();
        }

        return Page();
    }

    public async Task OnPostAsync(CancellationToken ct)
    {
        try
        {
            // 1) Load collection
            var collectionResult = await _api.GetAsync<Collection>($"collections/{Id}");
            if (!collectionResult.IsSuccess || collectionResult.Data is null)
                throw new Exception("Unable to load the collection.");

            Collection Collection = collectionResult.Data;

            // Hard fail if vector store id is missing (your current policy)
            if (string.IsNullOrWhiteSpace(Collection.OpenAiVectorStoreId))
            {
                string strExceptionMessage = "Collection is missing the OpenAI Vector Store Id.";
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                return;
            }

            // 2) Get known OpenAI file ids for THIS collection (from your DB/API)
            //    We use these ONLY if we must repair an expired/dead vector store.
            var filesRes = await _api.GetAsync<List<File>>($"files?collectionId={Id}");
            var existingOpenAiFileIds = (filesRes.IsSuccess && filesRes.Data != null
                ? filesRes.Data.Where(f => !string.IsNullOrWhiteSpace(f.OpenAIFileId)).Select(f => f.OpenAIFileId!)
                : Enumerable.Empty<string>())
                .Distinct()
                .ToArray();

            // 3) Ensure vector store is healthy (auto-repairs if expired/dead).
            //    If repaired, it returns a NEW id; otherwise it returns the same id.
            var vsName = $"Collection {Collection.CollectionId}";
            var vsMeta = new Dictionary<string, string>
            {
                ["collectionId"] = Collection.CollectionId.ToString(),
                ["companyId"] = Collection.CompanyId.ToString()
            };

            string healthyVectorStoreId = await _vectorStores.EnsureReadyOrRepairAsync(
                Collection.OpenAiVectorStoreId!,
                existingOpenAiFileIds,
                nameIfRecreated: vsName,
                metadataIfRecreated: vsMeta,
                ct);

            // 3b) If the store was recreated, persist the NEW id on the Collection (one-time)
            if (!string.Equals(healthyVectorStoreId, Collection.OpenAiVectorStoreId, StringComparison.Ordinal))
            {
                Collection.OpenAiVectorStoreId = healthyVectorStoreId;
                var update = await _api.PutAsync<object>($"collections/{Collection.CollectionId}", Collection);
                if (!update.IsSuccess)
                    throw new Exception("Failed to save new vector store id to the collection.");
            }

            // 4) Sanitize helper (your existing pattern)
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

            // 5) Process each uploaded file
            foreach (IFormFile IFormFile in Uploads)
            {
                // Basic validations (as you had)
                if (IFormFile is null || IFormFile.Length == 0)
                {
                    string strExceptionMessage = "Please choose a file.";
                    TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                    return;
                }

                // Determine extension from content type (your helper)
                string? strExtension = GetExtensionFromContentTypeHelper.GetExtensionFromContentType(IFormFile);
                if (string.IsNullOrEmpty(strExtension))
                    throw new Exception("Unable to get the file extension.");

                // Create your File entity (id first so we can name the physical file deterministically)
                File File = new File
                {
                    FileId = Guid.NewGuid(),
                    CollectionId = Id,
                    File1 = FileNameHelper.TruncateFileName(objHtmlSanitizer.Sanitize(IFormFile.FileName.Trim()), maxLength),
                    Extension = strExtension.TrimStart('.').ToLower(),
                    Uploaded = DateTime.Now,
                    UploadedByUserId = HttpContext.Session.GetObject<UserDto>("User").UserId
                };

                string strFileName = File.FileId.ToString() + strExtension; // e.g., "{guid}.pdf"
                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Files");

                // 5a) Save physical file (local storage) — keeps your existing helper/structure
                bool uploadSuccess = await FileUploadHelper.TryUploadFileAsync(
                    IFormFile,
                    "File",
                    strPath,
                    FileTypes,
                    null,
                    strFileName);

                if (!uploadSuccess)
                    throw new Exception("Unable to upload the file.");

                // 5b) Upload to OpenAI + attach to (healthy) vector store, wait until indexed
                //     NOTE: use a stream from the uploaded IFormFile to avoid re-reading from disk.
                using (Stream Stream = IFormFile.OpenReadStream())
                {
                    string strOpenAIFileID = await _openAIFileService.UploadAndAttachAsync(
                        healthyVectorStoreId,
                        Stream,
                        strFileName,
                        IFormFile.ContentType,
                        ct);

                    File.OpenAIFileId = strOpenAIFileID; // persist the OAI file id you’ll use for future repairs
                }

                // 5c) Persist your file record in your API/DB
                var result = await _api.PostAsync<File>("files", File);
                if (!result.IsSuccess)
                {
                    ApiErrorHelper.AddErrorsToModelState(result, ModelState, "File");

                    string strExceptionMessage = ApiErrorHelper.GetErrorString(result);
                    TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                    return;
                }
            }

            // Success UI
            TempData["StartupJavaScript"] = "window.top.location.reload(); ShowSnack('success', 'File successfully uploaded.', 7000, true)";
        }
        catch (Exception ex)
        {
            // Log to App Insights (your existing pattern)
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // User-facing error
            string strExceptionMessage = "File NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }
}
