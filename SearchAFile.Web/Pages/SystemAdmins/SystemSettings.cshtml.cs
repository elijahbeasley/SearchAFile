using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Mappers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

namespace SearchAFile.Web.Pages.Admins;

[BindProperties(SupportsGet = true)]
public class SystemInfoSettingsModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;

    public SystemInfoSettingsModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
    }

    public SystemInfo SystemInfo { get; set; }

    public IFormFile? IFormFileFavicon { get; set; }
    public IFormFile? IFormFileHeaderLogo { get; set; }
    public IFormFile? IFormFileFooterLogo { get; set; }
    public IFormFile? IFormFileEmailLogo { get; set; }

    private List<string> FileTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public IActionResult OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "System Settings");

            LoadData();

            return Page();
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the SystemInfo.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
            return NotFound();
        }
    }

    public async Task OnPostModify()
    {
        string strMessage = "";

        try
        {
            SystemInfo? UpdateSystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

            if (UpdateSystemInfo != null)
            {
                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "SystemFiles");

                if (IFormFileFavicon != null)
                {
                    bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileFavicon, "Header Logo", strPath, FileTypes, fileName => UpdateSystemInfo.Favicon = fileName, null, UpdateSystemInfo.Favicon);
                    if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
                }

                if (IFormFileHeaderLogo != null)
                {
                    bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileHeaderLogo, "Header Logo", strPath, FileTypes, fileName => UpdateSystemInfo.HeaderLogo = fileName, null, UpdateSystemInfo.HeaderLogo);
                    if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
                }

                if (IFormFileFooterLogo != null)
                {
                    bool footerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileFooterLogo, "Footer Logo", strPath, FileTypes, fileName => UpdateSystemInfo.FooterLogo = fileName, null, UpdateSystemInfo.FooterLogo);
                    if (!footerSuccess) throw new Exception("Unable to upload the footer logo.");
                }

                if (IFormFileEmailLogo != null)
                {
                    bool emailSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileEmailLogo, "Email Logo", strPath, FileTypes, fileName => UpdateSystemInfo.EmailLogo = fileName, null, UpdateSystemInfo.EmailLogo);
                    if (!emailSuccess) throw new Exception("Unable to upload the email logo.");
                }

                HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

                UpdateSystemInfo.SystemName = objHtmlSanitizer.Sanitize(SystemInfo.SystemName.Trim());
                UpdateSystemInfo.Url = objHtmlSanitizer.Sanitize(SystemInfo.Url.Trim());
                UpdateSystemInfo.ContactName = objHtmlSanitizer.Sanitize(SystemInfo.ContactName.Trim());
                UpdateSystemInfo.ContactEmailAddress = objHtmlSanitizer.Sanitize(SystemInfo.ContactEmailAddress.Trim());
                UpdateSystemInfo.ContactPhoneNumber = objHtmlSanitizer.Sanitize(SystemInfo.ContactPhoneNumber.Trim());
                UpdateSystemInfo.PrimaryColor = objHtmlSanitizer.Sanitize(SystemInfo.PrimaryColor.Trim());
                UpdateSystemInfo.SecondaryColor = objHtmlSanitizer.Sanitize(SystemInfo.SecondaryColor.Trim());
                UpdateSystemInfo.PrimaryTextColor = objHtmlSanitizer.Sanitize(SystemInfo.PrimaryTextColor.Trim());
                UpdateSystemInfo.SecondaryTextColor = objHtmlSanitizer.Sanitize(SystemInfo.SecondaryTextColor.Trim());
                UpdateSystemInfo.Version = objHtmlSanitizer.Sanitize(SystemInfo.Version.Trim());

                var updateSystemInfoResult = await _api.PutAsync<bool>($"systeminfos/{UpdateSystemInfo.SystemInfoId}", UpdateSystemInfo);

                if (!updateSystemInfoResult.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(updateSystemInfoResult) ?? "Unable to update SystemInfo.");

                HttpContext.Session.SetObject("SystemInfo", UpdateSystemInfo);

                // Output a success message.
                strMessage = "System info successfully updated.";
                TempData["StartupJavaScript"] = "ClearToast();  window.top.ShowToast('success', 'Success!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true);";
            }
            else
            {
                throw new Exception("Unable to retrieve SystemInfo.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the SystemInfo.
            string strExceptionMessage = "System info NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
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
            SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");
        }
        catch
        {
            throw;
        }
    }
}
