using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace SearchAFile.Web.Pages.FileGroups;

[BindProperties]
public class EditModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;

    public EditModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
    }

    public FileGroup FileGroup { get; set; } = default!;

    public IFormFile? IFormFile { get; set; }

    private List<string> FileGroupTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Edit FileGroup");

            if (id == null)
                return NotFound();

            var result = await _api.GetAsync<FileGroup>($"filegroups/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve file group.");

            FileGroup = result.Data;

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

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var getResult = await _api.GetAsync<FileGroup>($"filegroups/{FileGroup.FileGroupId}");

            if (!getResult.IsSuccess || getResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(getResult) ?? "Unable to retrieve file group.");

            FileGroup UpdateFileGroup = getResult.Data;

            string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "FileGroups");

            if (IFormFile != null)
            {
                bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFile, "Image", strPath, FileGroupTypes, fileName => UpdateFileGroup.ImageUrl = fileName);
                if (!headerSuccess) throw new Exception("Unable to upload the image.");
            }

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            UpdateFileGroup.FileGroup1 = objHtmlSanitizer.Sanitize(FileGroup.FileGroup1.Trim());
            UpdateFileGroup.Active = FileGroup.Active;

            var updateResult = await _api.PutAsync<FileGroup>($"filegroups/{UpdateFileGroup.FileGroupId}", UpdateFileGroup);

            if (!updateResult.IsSuccess)
            {
                ApiErrorHelper.AddErrorsToModelState(updateResult, ModelState, "FileGroup");
                return Page();
            }

            TempData["StartupJavaScript"] = "ShowSnack('success', 'File group successfully updated.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "File group NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
