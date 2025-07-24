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
using SearchAFile.Web.Helpers;

namespace SearchAFile.Pages.Common;

[BindProperties(SupportsGet = true)]
public class AccountSettingsModel : PageModel
{
    private readonly TelemetryClient TelemetryClient;
    private readonly AuthenticatedApiClient _api;
    private IWebHostEnvironment IWebHostEnvironment;
    private readonly IEmailService IEmailService;

    public AccountSettingsModel(TelemetryClient TC, AuthenticatedApiClient api, IWebHostEnvironment IWHE, IEmailService IES)
    {
        TelemetryClient = TC;
        _api = api;
        IWebHostEnvironment = IWHE;
        IEmailService = IES;
    }

    public UserDto? User { get; set; }

    [Required(ErrorMessage = "A file is required.")]
    public IFormFile IFormFile { get; set; }
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

            // Set the message.
            HttpContext.Session.SetString("Message", "View and update your account info.");

            LoadData();
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

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
            TelemetryClient.TrackException(ExceptionTelemetry);

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
            LoadData();

            string? emailName;

            var emailResult = await _api.GetAsync<string?>($"users/emailexists?companyId={User.CompanyId}&email={User.EmailAddress}");

            if (emailResult.IsSuccess)
            {
                emailName = emailResult.Data!;
            }
            else
            {
                throw new Exception(emailResult.ErrorMessage ?? "Unable to check if email exists.");
            }

            if (!string.IsNullOrEmpty(emailName))
            {
                // Add an error message.
                ModelState.AddModelError(nameof(User.EmailAddress), "Invalid email address entered. The entered email address is being used by " + emailName + ".");
                TempData["StartupJavaScript"] += "$('#txtEmailAddress').addClass('input-validation-error');";
            }

            string phoneName = null;

            var phoneResult = await _api.GetAsync<string?>($"users/phoneexists?companyId={User.CompanyId}&email={User.PhoneNumber}");

            if (phoneResult.IsSuccess)
            {
                phoneName = phoneResult.Data!;
            }
            else
            {
                throw new Exception(phoneResult.ErrorMessage ?? "Unable to check if phone exists.");
            }

            if (!string.IsNullOrEmpty(phoneName))
            {
                // Add an error message.
                ModelState.AddModelError(nameof(User.PhoneNumber), "Invalid phone number entered. The entered phone number is being used by " + phoneName + ".");
                TempData["StartupJavaScript"] += "$('#txtEmailAddress').addClass('input-validation-error');";
            }

            // Remove the User.Password and IFormFile ModelState attribute so that it does not trigger ModelState.IsValid = false.
            //ModelState.Remove("User.Active");
            //ModelState.Remove("User.Role");
            //ModelState.Remove("IFormFile");

            if (!ModelState.IsValid)
            {
                TempData["StartupJavaScript"] += "Modify(); window.top.ShowToast('danger', 'The following errors have occured', $('#divValidationSummary').html(), 0, false)";
                return;
            }

            User UpdateUser;

            var userResult = await _api.GetAsync<User>($"users/{HttpContext.Session.GetObject<UserDto>("User").UserId}");

            if (userResult.IsSuccess)
            {
                UpdateUser = userResult.Data!;
            }
            else
            {
                throw new Exception(userResult.ErrorMessage ?? "Unable to retrieve user.");
            }

            if (UpdateUser != null)
            {
                HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

                UpdateUser.FirstName = objHtmlSanitizer.Sanitize(User.FirstName.Trim());
                UpdateUser.LastName = objHtmlSanitizer.Sanitize(User.LastName.Trim());
                UpdateUser.EmailAddress = objHtmlSanitizer.Sanitize(User.EmailAddress.Trim());
                UpdateUser.PhoneNumber = objHtmlSanitizer.Sanitize(User.PhoneNumber.Trim());

                var response = await _api.PutAsync($"companies/{UpdateUser.UserId}", UpdateUser);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error updating account info: {errorContent}");
                }

                HttpContext.Session.SetObject("User", UpdateUser);

                // Output a success message.
                strMessage = "Account info successfully modified.";
                TempData["StartupJavaScript"] = "ClearToast();  window.top.ShowToast('success', 'Modify Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 7000, true);";
            }
            else
            {
                throw new Exception("Unable to retrieve user.");
            }

            //ModelState.Remove("User.FirstName");
            //ModelState.Remove("User.LastName");
            //ModelState.Remove("User.EmailAddress");
            //ModelState.Remove("User.PhoneNumber");
            //ModelState.Remove("User.WhatsApp");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Account info NOT successfully modified. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }

    public async Task OnPostUploadImage()
    {
        string strMessage = "";

        // Create the path.
        string strPath = Path.Combine(IWebHostEnvironment.WebRootPath, "UserFiles");
        string strFileName = "";

        try
        {
            LoadData();

            if (IFormFile != null)
            {
                if (!FileTypes.Contains(Path.GetExtension(IFormFile.FileName).Remove(0, 1).ToLower()))
                {
                    strMessage = "<ul><li>Invalid file selected. File must be of type: " + string.Join(", ", FileTypes) + ".</li></ul>";
                    TempData["StartupJavaScript"] = "$('#fileName').addClass('input-validation-error');  window.top.ShowToast('danger', 'The following errors have occured', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false);";

                    return;
                }

                User UpdateUser;

                var result = await _api.GetAsync<User>($"users/{HttpContext.Session.GetObject<UserDto>("User").UserId}");

                if (result.IsSuccess)
                {
                    UpdateUser = result.Data!;
                }
                else
                {
                    // You can inspect result.StatusCode or result.ErrorMessage here
                    throw new Exception(result.ErrorMessage ?? "Unable to retrieve user.");
                }

                if (UpdateUser != null)
                {
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

                    // Create the file name.
                    strFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(IFormFile.FileName);

                    if (!Directory.Exists(strPath))
                    {
                        Directory.CreateDirectory(strPath);
                    }

                    // Upload the new file.
                    using (var fileStream = new FileStream(Path.Combine(strPath, strFileName), FileMode.Create))
                    {
                        await IFormFile.CopyToAsync(fileStream);
                    }

                    UpdateUser.HeadshotPath = strFileName;

                    var response = await _api.PutAsync($"companies/{UpdateUser.UserId}", UpdateUser);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error updating account info: {errorContent}");
                    }

                    HttpContext.Session.SetObject("User", UpdateUser);

                    // Output a success message.
                    strMessage = "Image successfully uploaded.";
                    TempData["StartupJavaScript"] = "RefreshMenu(); window.top.ClearToast();  window.top.ShowToast('success', 'Image Upload Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 7000, true);";
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
            TelemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Image NOT successfully uploaded. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Upload Image Error!', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
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