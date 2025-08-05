using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Emit;
using MimeKit;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Interfaces;
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
    private readonly IEmailService _emailService;

    public CreateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, IEmailService emailService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _emailService = emailService;
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
            string? emailName;

            var emailResult = await _api.GetAsync<string>($"users/emailexists?companyId={HttpContext.Session.GetObject<Company>("Company").CompanyId}&email={User.EmailAddress}");

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

            var phoneResult = await _api.GetAsync<string?>($"users/phoneexists?companyId={HttpContext.Session.GetObject<Company>("Company").CompanyId}&phone={User.PhoneNumber}");

            if (!phoneResult.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(phoneResult) ?? "Unable to check if phone exists.");

            phoneName = phoneResult.Data;

            if (!string.IsNullOrEmpty(phoneName))
            {
                // Add an error message.
                ModelState.AddModelError(nameof(User.PhoneNumber), "Invalid phone number entered. The entered phone number is being used by " + phoneName + ".");
                TempData["StartupJavaScript"] += "$('#txtPhoneNumber').addClass('input-validation-error');";
            }

            if (!ModelState.IsValid)
            {
                TempData["StartupJavaScript"] += "window.top.ShowToast('danger', 'The following errors have occured', $('#divValidationSummary').html(), 0, false)";
                return Page();
            }

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

            if (!result.IsSuccess && result.Data != null)
            {
                ApiErrorHelper.AddErrorsToModelState(result, ModelState, "User");

                string strExceptionMessage = ApiErrorHelper.GetErrorString(result);
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
                return Page();
            }

            User = result.Data;

            // Send a welcome email.
            await SendEmail();

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

    private async Task SendEmail()
    {
        try
        {
            // Create the email verification info.
            SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

            // Send the password reset email.

            BodyBuilder objBodyBuilder = new BodyBuilder();

            objBodyBuilder.HtmlBody = @"
                <table> 
                    <tr> 
                        <td> 
                            Hello " + User.FullName + @", 
                        </td> 
                    </tr> 
                    <tr> 
                        <td style='padding: 0rem 1rem;'> 
                            <br />";

            if (User.CompanyId != null)
            {
                Company Company = HttpContext.Session.GetObject<Company>("Company");

                if (Company != null)
                {
                    objBodyBuilder.HtmlBody += "A " + SystemInfo.SystemName + " account for " + Company.Company1 + " has been created using this email address. Please <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "VerifyEmailAddress") + "?id=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address.";
                }
                else
                {
                    objBodyBuilder.HtmlBody += "A " + SystemInfo.SystemName + " account has been created using this email address. Please <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "VerifyEmailAddress") + "?id=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address.";
                }
            }
            else
            {
                objBodyBuilder.HtmlBody += "A " + SystemInfo.SystemName + " account has been created using this email address. Please <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "VerifyEmailAddress") + "?id=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address.";
            }

            objBodyBuilder.HtmlBody += @"
                        </td> 
                    </tr> 
                </table> ";

            // To.
            List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

            // Add service to the email.
            lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FullName));

            // CC.
            List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

            // BCC.
            List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

            await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Verify Email Address", objBodyBuilder);
        }
        catch
        {
            throw;
        }
    }
}
