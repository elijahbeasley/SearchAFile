using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace SearchAFile.Web.Pages.Companies;

[BindProperties]
public class EditModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly OpenAIFileService _openAIFileService;

    public EditModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, OpenAIFileService openAIFileService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _openAIFileService = openAIFileService;
    }

    public Company Company { get; set; } = default!;

    public IFormFile? IFormFileHeaderLogo { get; set; }
    public IFormFile? IFormFileFooterLogo { get; set; }
    public IFormFile? IFormFileEmailLogo { get; set; }

    private List<string> FileTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Edit Company");

            if (id == null)
                return NotFound();

            var result = await _api.GetAsync<Company>($"companies/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve company.");

            Company = result.Data;

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
            var getResult = await _api.GetAsync<Company>($"companies/{Company.CompanyId}");

            if (!getResult.IsSuccess || getResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(getResult) ?? "Unable to retrieve company.");

            Company UpdateCompany = getResult.Data;

            string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "CompanyFiles");

            if (IFormFileHeaderLogo != null)
            {
                bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileHeaderLogo, "Header Logo", strPath, FileTypes, fileName => UpdateCompany.HeaderLogo = fileName);
                if (!headerSuccess) throw new Exception("Unable to upload the header logo.");
            }

            if (IFormFileFooterLogo != null)
            {
                bool footerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileFooterLogo, "Footer Logo", strPath, FileTypes, fileName => UpdateCompany.FooterLogo = fileName);
                if (!footerSuccess) throw new Exception("Unable to upload the footer logo.");
            }

            if (IFormFileEmailLogo != null)
            {
                bool emailSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileEmailLogo, "Email Logo", strPath, FileTypes, fileName => UpdateCompany.EmailLogo = fileName);
                if (!emailSuccess) throw new Exception("Unable to upload the email logo.");
            }

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            UpdateCompany.Company1 = objHtmlSanitizer.Sanitize(Company.Company1.Trim());
            if (!string.IsNullOrEmpty(Company.Address))
            {
                UpdateCompany.Address = objHtmlSanitizer.Sanitize(Company.Address.Trim().Replace(Environment.NewLine, "<br>"));
            }
            UpdateCompany.ContactName = objHtmlSanitizer.Sanitize(Company.ContactName.Trim());
            UpdateCompany.ContactEmailAddress = objHtmlSanitizer.Sanitize(Company.ContactEmailAddress.Trim());
            UpdateCompany.ContactPhoneNumber = objHtmlSanitizer.Sanitize(Company.ContactPhoneNumber.Trim());
            UpdateCompany.Url = objHtmlSanitizer.Sanitize(Company.Url.Trim());
            UpdateCompany.Active = Company.Active;

            var updateResult = await _api.PutAsync<Company>($"companies/{UpdateCompany.CompanyId}", UpdateCompany);

            if (!updateResult.IsSuccess)
            {
                ApiErrorHelper.AddErrorsToModelState(updateResult, ModelState, "Company");
                return Page();
            }

            if (HttpContext.Session.GetObject<UserDto>("User") != null
                && HttpContext.Session.GetObject<UserDto>("User").Role.Equals("System Admin")
                && HttpContext.Session.GetObject<List<SelectListItem>>("Companies") != null)
            {
                // Get the user's company.
                var getCompaniesResult = await _api.GetAsync<List<Company>>("companies");

                if (!getCompaniesResult.IsSuccess || getCompaniesResult.Data == null)
                {
                    throw new Exception(getCompaniesResult.ErrorMessage ?? "Unable to retrieve the companies.");
                }

                List<SelectListItem> Companies = getCompaniesResult.Data
                    .OrderBy(company => company.Company1)
                    .Select(company => new SelectListItem
                    {
                        Text = company.Company1,
                        Value = company.CompanyId.ToString(),
                        Selected = company.CompanyId.ToString() == HttpContext.Session.GetObject<List<SelectListItem>>("Companies")?.FirstOrDefault(item => item.Selected)?.Value
                    })
                    .ToList();

                // Store in session
                HttpContext.Session.SetObject("Companies", Companies);
            }

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Company successfully updated.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Company NOT successfully updated. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
