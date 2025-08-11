using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
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
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly OpenAIFileService _openAIFileService;

    public CreateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, OpenAIFileService openAIFileService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _openAIFileService = openAIFileService;
    }

    public FileUploaderOptions FileUploaderOptions { get; set; }
    private int maxLength = DataAnnotationHelpers.GetMaxLenFromAttr<File>(f => f.File1);

    public Guid? Id { get; set; }

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

            var filesResult = await _api.GetAsync<List<File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            List<File> Files = filesResult.Data
                .Where(file => file.CollectionId == id)
                .ToList();

            FileUploaderOptions = new FileUploaderOptions
            {
                DomId = "uploaderA",
                InputName = "Uploads",
                AcceptedTypes = new[] { ".pdf", ".docx", ".csv", ".txt", ".md" },
                PerFileLimitMB = 10,
                TotalLimitMB = 25,
                MaxFiles = 20,
                AlreadyUploadedCount = Files.Count(),
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

    public async Task OnPostAsync()
    {
        try
        {
            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

            foreach (IFormFile IFormFile in Uploads)
            {
                string? strExtension = GetExtensionFromContentTypeHelper.GetExtensionFromContentType(IFormFile);

                if (string.IsNullOrEmpty(strExtension))
                {
                    throw new Exception("Unable to get the file extension.");
                }

                File File = new File();
                File.FileId = Guid.NewGuid();
                File.CollectionId = Id;
                File.File1 = FileNameHelper.TruncateFileName(objHtmlSanitizer.Sanitize(IFormFile.FileName.Trim()), maxLength);
                File.Extension = strExtension?.TrimStart('.')?.ToLower();

                File.Uploaded = DateTime.Now;
                File.UploadedByUserId = HttpContext.Session.GetObject<UserDto>("User").UserId;

                string strFileName = File.FileId.ToString() + strExtension;

                // Upload the file to OpenAI first, because it gives us the OpenAI file id.
                bool postSuccess = await _openAIFileService.TryPostFileToOpenAIAsync(IFormFile, "File", FileTypes, openAIFileId => File.OpenAIFileId = openAIFileId, strFileName);
                if (!postSuccess) throw new Exception("Unable to post the file to OpenAI.");

                // Save the file to our local storage using the new FileID.
                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "Files");

                bool uploadSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFile, "File", strPath, FileTypes, null, strFileName);
                if (!uploadSuccess) throw new Exception("Unable to upload the file.");

                // POST the file so that we can get it's ID.
                var result = await _api.PostAsync<File>("files", File);

                if (!result.IsSuccess)
                {
                    ApiErrorHelper.AddErrorsToModelState(result, ModelState, "File");

                    string strExceptionMessage = ApiErrorHelper.GetErrorString(result);
                    TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                    return;
                }
            }

            TempData["StartupJavaScript"] = "window.top.location.reload(); ShowSnack('success', 'File successfully uploaded.', 7000, true)";
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "File NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }
}
