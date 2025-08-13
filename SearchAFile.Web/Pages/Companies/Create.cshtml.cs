using Ganss.Xss;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Emit;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace SearchAFile.Web.Pages.Companies;

[BindProperties]
public class CreateModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    private readonly OpenAIFileService _openAIFileService;

    public CreateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IWebHostEnvironment iWebHostEnvironment, OpenAIFileService openAIFileService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _iWebHostEnvironment = iWebHostEnvironment;
        _openAIFileService = openAIFileService;
    }

    public Company Company { get; set; } = default!;

    [Required(ErrorMessage = "Header logo is required.")]
    public IFormFile IFormFileHeaderLogo { get; set; }

    [Required(ErrorMessage = "Footer logo is required.")]
    public IFormFile IFormFileFooterLogo { get; set; }

    [Required(ErrorMessage = "Email logo is required.")]
    public IFormFile IFormFileEmailLogo { get; set; }

    private List<string> FileTypes = new List<string>()
    {
        "png","jpeg","jpg"
    };

    public IActionResult OnGet()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Create Company");

            ModelState.Remove("IFormFileHeaderLogo");
            ModelState.Remove("IFormFileFooterLogo");
            ModelState.Remove("IFormFileEmailLogo");
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

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            string strPath = Path.Combine(_iWebHostEnvironment.WebRootPath, "CompanyFiles");

            bool headerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileHeaderLogo, "Header Logo", strPath, FileTypes, fileName => Company.HeaderLogo = fileName);
            if (!headerSuccess) throw new Exception("Unable to upload the header logo.");

            bool footerSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileFooterLogo, "Footer Logo", strPath, FileTypes, fileName => Company.FooterLogo = fileName);
            if (!footerSuccess) throw new Exception("Unable to upload the footer logo.");

            bool emailSuccess = await FileUploadHelper.TryUploadFileAsync(IFormFileEmailLogo, "Email Logo", strPath, FileTypes, fileName => Company.EmailLogo = fileName);
            if (!emailSuccess) throw new Exception("Unable to upload the email logo.");

            // Sanitize the data.
            HtmlSanitizer objHtmlSanitizer = new HtmlSanitizer();
            Company.Company1 = objHtmlSanitizer.Sanitize(Company.Company1.Trim());
            if (!string.IsNullOrEmpty(Company.Address))
            {
                Company.Address = objHtmlSanitizer.Sanitize(Company.Address.Trim().Replace(Environment.NewLine, "<br>"));
            }
            Company.ContactName = objHtmlSanitizer.Sanitize(Company.ContactName.Trim());
            Company.ContactEmailAddress = objHtmlSanitizer.Sanitize(Company.ContactEmailAddress.Trim());
            Company.ContactPhoneNumber = objHtmlSanitizer.Sanitize(Company.ContactPhoneNumber.Trim());
            Company.Url = objHtmlSanitizer.Sanitize(Company.Url.Trim());

            var createResult = await _api.PostAsync<Company>("companies", Company);

            if (!createResult.IsSuccess)
            {
                ApiErrorHelper.AddErrorsToModelState(createResult, ModelState, "Company");

                string strExceptionMessage = ApiErrorHelper.GetErrorString(createResult);
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '<ul>" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<li>").EscapeJsString() + "</ul>', 0, false);";
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

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Company successfully created.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Company NOT successfully created. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
