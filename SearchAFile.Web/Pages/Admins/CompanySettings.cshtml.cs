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
public class CompanySettingsModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;

    public CompanySettingsModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
    }

    public Company Company { get; set; }

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
            HttpContext.Session.SetString("PageTitle", "Company Settings");

            LoadData();

            if (!string.IsNullOrEmpty(Company.Address))
            {
                Company.Address = Company.Address.Replace("<br>", Environment.NewLine);
            }

            return Page();
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the Company.
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
            Company? UpdateCompany = HttpContext.Session.GetObject<Company>("Company");

            if (UpdateCompany != null)
            {
                string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "CompanyFiles");

                if (IFormFileHeaderLogo != null)
                {
                    bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileHeaderLogo, "Header Logo", strPath, FileTypes, fileName => UpdateCompany.HeaderLogo = fileName, null, UpdateCompany.HeaderLogo);
                    if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
                }

                if (IFormFileFooterLogo != null)
                {
                    bool footerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileFooterLogo, "Footer Logo", strPath, FileTypes, fileName => UpdateCompany.FooterLogo = fileName, null, UpdateCompany.FooterLogo);
                    if (!footerSuccess) throw new Exception("Unable to upload the footer logo.");
                }

                if (IFormFileEmailLogo != null)
                {
                    bool emailSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileEmailLogo, "Email Logo", strPath, FileTypes, fileName => UpdateCompany.EmailLogo = fileName, null, UpdateCompany.EmailLogo);
                    if (!emailSuccess) throw new Exception("Unable to upload the email logo.");
                }

                HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();

                UpdateCompany.Company1 = objHtmlSanitizer.Sanitize(Company.Company1.Trim());
                UpdateCompany.Url = objHtmlSanitizer.Sanitize(Company.Url.Trim());
                UpdateCompany.ContactName = objHtmlSanitizer.Sanitize(Company.ContactName.Trim());
                UpdateCompany.ContactEmailAddress = objHtmlSanitizer.Sanitize(Company.ContactEmailAddress.Trim());
                UpdateCompany.ContactPhoneNumber = objHtmlSanitizer.Sanitize(Company.ContactPhoneNumber.Trim());
                UpdateCompany.Address = objHtmlSanitizer.Sanitize(Company.Address.Trim().Replace(Environment.NewLine, "<br>"));
                UpdateCompany.PrimaryColor = objHtmlSanitizer.Sanitize(Company.PrimaryColor.Trim());
                UpdateCompany.SecondaryColor = objHtmlSanitizer.Sanitize(Company.SecondaryColor.Trim());
                UpdateCompany.PrimaryTextColor = objHtmlSanitizer.Sanitize(Company.PrimaryTextColor.Trim());
                UpdateCompany.SecondaryTextColor = objHtmlSanitizer.Sanitize(Company.SecondaryTextColor.Trim());

                var updateCompanyResult = await _api.PutAsync<bool>($"companies/{UpdateCompany.CompanyId}", UpdateCompany);

                if (!updateCompanyResult.IsSuccess)
                    throw new Exception(ApiErrorHelper.GetErrorString(updateCompanyResult) ?? "Unable to update Company.");

                HttpContext.Session.SetObject("Company", UpdateCompany);

                // Output a success message.
                strMessage = "Company info successfully updated.";
                TempData["StartupJavaScript"] = "ClearToast();  window.top.ShowToast('success', 'Success!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true);";
            }
            else
            {
                throw new Exception("Unable to retrieve Company.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the Company.
            string strExceptionMessage = "Company info NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
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
            Company = HttpContext.Session.GetObject<Company>("Company");
        }
        catch
        {
            throw;
        }
    }
}
