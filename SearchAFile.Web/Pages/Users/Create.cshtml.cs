using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Emit;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace SearchAFile.Web.Pages.Users;

[BindProperties]
public class CreateModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;

    public CreateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
    }

    public User User { get; set; } = default!;

    public IFormFile? IFormFile { get; set; }

    private List<string> FileTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public IActionResult OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Create User");

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
            string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "UserFiles");

            if (IFormFile != null)
            {
                bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFile, "Headshot", strPath, FileTypes, fileName => User.HeadshotPath = fileName);
                if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
            }

            // Set the CompanyID.

            User.CompanyId = HttpContext.Session.GetObject<Company>("Company").CompanyId;

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            User.FirstName = objHtmlSanitizer.Sanitize(User.FirstName.Trim());
            User.LastName = objHtmlSanitizer.Sanitize(User.LastName.Trim());
            User.EmailAddress = objHtmlSanitizer.Sanitize(User.EmailAddress.Trim());
            User.PhoneNumber = objHtmlSanitizer.Sanitize(User.PhoneNumber.Trim());
            User.Password = BCrypt.Net.BCrypt.HashPassword(objHtmlSanitizer.Sanitize(User.Password.Trim()));

            var result = await _api.PostAsync<User>("users", User);

            if (!result.IsSuccess)
            {
                ApiErrorHelper.AddErrorsToModelState(result, ModelState, "User");

                string strExceptionMessage = ApiErrorHelper.GetErrorString(result);
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                return Page();
            }

            TempData["StartupJavaScript"] = "ShowSnack('success', '" + User.FullName + " successfully created.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "User NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
