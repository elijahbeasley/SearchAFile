using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Newtonsoft.Json;
using SearchAFile.Services;
using MimeKit;
using Ganss.Xss;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using SearchAFile.Web.Services;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Extensions;
using System.Collections.Generic;
using SearchAFile.Core.Mappers;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class AccountSettingsModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly IEmailService _emailService;

    public AccountSettingsModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, IEmailService emailService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _emailService = emailService;
    }

    public UserDto? User { get; set; }

    [Required(ErrorMessage = "A file is required.")]
    public IFormFile? IFormFile { get; set; }
    private List<string> FileTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public void OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Account Settings");

            LoadData();
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
    }

    public void OnGetRefresh()
    {
        try
        {
            LoadData();
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
    }

    public async Task OnPostModifyAsync()
    {
        string strMessage = "";

        try
        {
            string? emailName;

            var emailResult = await _api.GetAsync<string>($"users/emailexists?companyId={HttpContext.Session.GetObject<UserDto>("User").CompanyId}&email={User.EmailAddress}&userId={HttpContext.Session.GetObject<UserDto>("User").UserId}");

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

            var phoneResult = await _api.GetAsync<string?>($"users/phoneexists?companyId={HttpContext.Session.GetObject<UserDto>("User").CompanyId}&phone={User.PhoneNumber}&userId={HttpContext.Session.GetObject<UserDto>("User").UserId}");

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
            ModelState.Remove("IFormFile");

            if (!ModelState.IsValid)
            {
                TempData["StartupJavaScript"] += "Modify(); window.top.ShowToast('danger', 'The following errors have occured', $('#divValidationSummary').html(), 0, false)";
                return;
            }

            User? UpdateUser;

            var getUserResult = await _api.GetAsync<User>($"users/{HttpContext.Session.GetObject<UserDto>("User").UserId}");

            if (!getUserResult.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(getUserResult) ?? "Unable to retrieve user.");

            UpdateUser = getUserResult.Data!;

            if (UpdateUser != null)
            {
                HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

                UpdateUser.FirstName = objHtmlSanitizer.Sanitize(User.FirstName.Trim());
                UpdateUser.LastName = objHtmlSanitizer.Sanitize(User.LastName.Trim());
                UpdateUser.EmailAddress = objHtmlSanitizer.Sanitize(User.EmailAddress.Trim());
                UpdateUser.PhoneNumber = objHtmlSanitizer.Sanitize(User.PhoneNumber.Trim());

                var updateUserResult = await _api.PutAsync<bool>($"users/{UpdateUser.UserId}", UpdateUser);

                if (!updateUserResult.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(updateUserResult) ?? "Unable to update user.");

                HttpContext.Session.SetObject("User", DtoMapper.ToDto(UpdateUser));

                // Output a success message.
                strMessage = "Account info successfully modified.";
                TempData["StartupJavaScript"] = "ClearToast();  window.top.ShowToast('success', 'Modify Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true);";
            }
            else
            {
                throw new Exception("Unable to retrieve user.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Account info NOT successfully modified. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
        finally
        {
            LoadData();
        }
    }

    public async Task OnPostUploadImage()
    {
        string strMessage = "";

        // Create the path.
        string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "UserFiles");
        string strFileName = "";

        try
        {
            if (IFormFile != null)
            {
                if (!FileTypes.Contains(Path.GetExtension(IFormFile.FileName).Remove(0, 1).ToLower()))
                {
                    strMessage = "<ul><li>Invalid file selected. File must be of type: " + string.Join(", ", FileTypes) + ".</li></ul>";
                    TempData["StartupJavaScript"] = "$('#fileName').addClass('input-validation-error');  window.top.ShowToast('danger', 'The following errors have occured', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 0, false);";

                    return;
                }

                User? UpdateUser;

                var getUserResult = await _api.GetAsync<User>($"users/{HttpContext.Session.GetObject<UserDto>("User").UserId}");

                if (!getUserResult.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(getUserResult) ?? "Unable to retrieve user.");

                UpdateUser = getUserResult.Data!;

                if (UpdateUser != null)
                {
                    // Create the file name.
                    strFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(IFormFile.FileName);

                    UpdateUser.HeadshotPath = strFileName;

                    var updateUserResult = await _api.PutAsync<bool>($"users/{UpdateUser.UserId}", UpdateUser);

                    if (!updateUserResult.IsSuccess)
                        throw new Exception(ApiErrorHelper.GetErrorString(updateUserResult) ?? "Unable to update user.");

                    // Create the directory, if it does not already exist.
                    if (!Directory.Exists(strPath))
                    {
                        Directory.CreateDirectory(strPath);
                    }

                    // Delete the old file, if it exists. 
                    if (UpdateUser != null
                        && !string.IsNullOrEmpty(UpdateUser.HeadshotPath))
                    {
                        // Delete the old tile image from the folder.
                        string strDeletePath = Path.Combine(strPath, UpdateUser.HeadshotPath);

                        if (System.IO.File.Exists(strDeletePath)
                            && !UpdateUser.HeadshotPath.Equals("Generic.jpg"))
                        {
                            System.IO.File.Delete(strDeletePath);
                        }
                    }

                    // Upload the new file.
                    using (var fileStream = new FileStream(Path.Combine(strPath, strFileName), FileMode.Create))
                    {
                        await IFormFile.CopyToAsync(fileStream);
                    }

                    // Updated the user.
                    HttpContext.Session.SetObject("User", DtoMapper.ToDto(UpdateUser));

                    // Output a success message.
                    strMessage = "Image successfully uploaded.";
                    TempData["StartupJavaScript"] = "RefreshMenu(); window.top.ClearToast();  window.top.ShowToast('success', 'Image Upload Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true);";
                }
                else
                {
                    throw new Exception("Unable to retrieve user.");
                }
            }
            else
            {
                // Output an error message.
                throw new Exception("Image NOT successfully uploaded. Could not get attached image.");
            }

            //ModelState.Remove("User.FirstName");
            //ModelState.Remove("User.LastName");
            //ModelState.Remove("User.PhoneNumber");
            //ModelState.Remove("User.WhatsApp");
            //ModelState.Remove("User.EmailAddress");
            //ModelState.Remove("User.City");
            //ModelState.Remove("User.State");
            //ModelState.Remove("User.ZipCode");
            //ModelState.Remove("User.Company");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Image NOT successfully uploaded. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Upload Image Error!', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
        finally
        {
            LoadData();
        }
    }
    private void LoadData()
    {
        try
        {
            User = HttpContext.Session.GetObject<UserDto>("User");
        }
        catch
        {
            throw;
        }
    }
}