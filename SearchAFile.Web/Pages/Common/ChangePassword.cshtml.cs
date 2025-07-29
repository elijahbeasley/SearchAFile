using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using SearchAFile.Web.Services;
using SearchAFile.Web.Interfaces;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SearchAFile.Web.Extensions;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChangePasswordModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly IEmailService _emailService;

    public ChangePasswordModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, IEmailService emailService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _emailService = emailService;
    }

    public User User { get; set; }

    [DisplayName("New Password")]
    [RegularExpression(@"^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Invalid password entered. Password must be between 8 and 30 characters long, contain at least one upper case letter, at least one lower case letter, and at least one number.")]
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(30)]
    public string NewPassword { get; set; }

    [DisplayName("Repeat Password")]
    [Required(ErrorMessage = "Repeat password is required.")]
    [StringLength(30)]
    public string RepeatPassword { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            HttpContext.Session.SetString("UserID", id.ToString());

            await LoadUser();

            ModelState.Remove("NewPassword");
            ModelState.Remove("RepeatPassword");

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

            return StatusCode(500, new { message = "Failed to initialize page", detail = strExceptionMessage });
        }

        return Page();
    }

    public class CustomJsonObject
    {
        public bool booSuccess { get; set; } = true;
        public bool booIsEqual { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
    }

    public async Task<IActionResult> OnGetIsCurrentPasswordEqual(string CurrentPassword)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            await LoadUser();

            CustomJsonObject.booIsEqual = BCrypt.Net.BCrypt.Verify(CurrentPassword, User.Password);
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ErrorMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ErrorMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ErrorMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }

    public async Task<IActionResult> OnGetIsNewPasswordEqual(string NewPassword)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            await LoadUser();

            CustomJsonObject.booIsEqual = BCrypt.Net.BCrypt.Verify(NewPassword, User.Password);
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ErrorMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ErrorMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ErrorMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }

    public async Task OnPostAsync()
    {
        string strMessage = "";

        try
        {
            await LoadUser();

            User.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);

            var result = await _api.PutAsync<User>($"users/{User.UserId}", User);

            if (!result.IsSuccess)
            {
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
            }

            SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

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
                            <br /> 
                            Your password has been successfully changed. If you did not request a password change, please contact " + HttpContext.Session.GetString("ContactInfo") + @" immediately.
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

            await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Password Successfully Changed", objBodyBuilder);

            // Output a success message.
            strMessage = "Password successfully changed.";
            TempData["StartupJavaScript"] = "window.top.CloseModal(); window.top.ClearToast(); window.top.ShowToast('success', 'Password Change Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true);";
        }
        catch (Exception ex)
        {
            // Create the message.
            strMessage = "Password NOT successfully modified. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                // Append on the exception message.
                strMessage += ex.Message;
            }
            else // Yes.
            {
                // Append on the inner exception message.
                strMessage += ex.InnerException.Message;
            }

            // Output an error message.
            TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowToast('danger', 'Password Change Error!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 0, false);";
        }
    }

    private async Task LoadUser()
    {
        try
        {
            var result = await _api.GetAsync<User>($"users/{HttpContext.Session.GetString("UserID")}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve user.");

            User = result.Data;
        }
        catch
        {
            throw;
        }
    }
}