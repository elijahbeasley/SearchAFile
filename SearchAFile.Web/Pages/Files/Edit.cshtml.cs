using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Web.Classes;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Files;

[BindProperties]
public class EditModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public EditModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    public File? File { get; set; }

    public async Task<IActionResult> OnGet(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            var result = await _api.GetAsync<File>($"files/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve file.");

            File = result.Data;

            TempData["StartupJavaScript"] = "if (self !== top) { window.top.StopLoading('#divLoadingBlock'); window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowModal(); }";
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

        return Page();
    }

    public async Task OnPostAsync()
    {
        try
        {
            var getResult = await _api.GetAsync<File>($"files/{File?.FileId}");

            if (!getResult.IsSuccess || getResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(getResult) ?? "Unable to retrieve File.");

            File UpdateFile = getResult.Data;

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            UpdateFile.File1 = objHtmlSanitizer.Sanitize(File?.File1?.Trim());

            var updateResult = await _api.PutAsync<File>($"files/{UpdateFile.FileId}", UpdateFile);

            if (!updateResult.IsSuccess)
            {
                string strExceptionMessage = ApiErrorHelper.GetErrorString(updateResult);
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                return;
            }

            TempData["StartupJavaScript"] = "window.top.location.reload(); ShowSnack('success', 'File successfully updated.', 7000, true)";
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "File NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlock'); window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }
}
