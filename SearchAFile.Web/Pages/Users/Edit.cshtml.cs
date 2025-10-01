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

namespace SearchAFile.Web.Pages.Users;

[BindProperties]
public class EditModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;

    public EditModel(TelemetryClient telemetryClient, 
        AuthenticatedApiClient api, 
        IWebHostEnvironment iWebHostEnvironment)
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

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Edit User");

            if (id == null)
                return NotFound();

            var result = await _api.GetAsync<User>($"users/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve user.");

            User = result.Data;

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
            string? emailName;

            var emailResult = await _api.GetAsync<string>($"users/emailexists?companyId={HttpContext.Session.GetObject<Company>("Company").CompanyId}&email={User.EmailAddress}&userId={User.UserId}");

            if (!emailResult.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(emailResult) ?? "Unable to check if email exists.");

            emailName = emailResult.Data;

            if (!string.IsNullOrEmpty(emailName))
            {
                // Add an error message.
                ModelState.AddModelError(nameof(User.EmailAddress), "Invalid email address entered. The entered email address is being used by " + emailName + ".");
                TempData["StartupJavaScript"] += "$('#txtEmailAddress').addClass('input-validation-error');";
            }

            string? phoneName;

            var phoneResult = await _api.GetAsync<string?>($"users/phoneexists?companyId={HttpContext.Session.GetObject<Company>("Company").CompanyId}&phone={User.PhoneNumber}&userId={User.UserId}");

            if (!phoneResult.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(phoneResult) ?? "Unable to check if phone exists.");

            phoneName = phoneResult.Data;

            if (!string.IsNullOrEmpty(phoneName))
            {
                // Add an error message.
                ModelState.AddModelError(nameof(User.PhoneNumber), "Invalid phone number entered. The entered phone number is being used by " + phoneName + ".");
                TempData["StartupJavaScript"] += "$('#txtPhoneNumber').addClass('input-validation-error');";
            }

            // Remove the unused ModelState attribute so that it does not trigger ModelState.IsValid = false.
            ModelState.Remove("User.Password");

            if (!ModelState.IsValid)
            {
                TempData["StartupJavaScript"] += "window.top.ShowToast('danger', 'The following errors have occured', $('#divValidationSummary').html(), 0, false)";
                return Page();
            }

            var getResult = await _api.GetAsync<User>($"users/{User.UserId}");

            if (!getResult.IsSuccess || getResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(getResult) ?? "Unable to retrieve user.");

            User UpdateUser = getResult.Data;

            string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "UserFiles");

            if (IFormFile != null)
            {
                bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFile, "Headshot", strPath, FileTypes, fileName => UpdateUser.HeadshotPath = fileName, UpdateUser.HeadshotPath);
                if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
            }

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            UpdateUser.FirstName = objHtmlSanitizer.Sanitize(User.FirstName.Trim());
            UpdateUser.LastName = objHtmlSanitizer.Sanitize(User.LastName.Trim());
            UpdateUser.EmailAddress = objHtmlSanitizer.Sanitize(User.EmailAddress.Trim());
            UpdateUser.PhoneNumber = objHtmlSanitizer.Sanitize(User.PhoneNumber.Trim());
            UpdateUser.Role = User.Role;
            UpdateUser.Active = User.Active;

            var updateResult = await _api.PutAsync<User>($"users/{UpdateUser.UserId}", UpdateUser);

            if (!updateResult.IsSuccess)
            {
                ApiErrorHelper.AddErrorsToModelState(updateResult, ModelState, "User");
                return Page();
            }

            TempData["StartupJavaScript"] = "ShowSnack('success',  '" + UpdateUser.FullName + " successfully updated.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "User NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
