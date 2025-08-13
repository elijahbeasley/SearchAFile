using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Companies;

public class DeleteModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public DeleteModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty]
    public Company Company { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Delete Company");

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

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            var deleteResult = await _api.DeleteAsync<object>($"companies/{id}");

            if (!deleteResult.IsSuccess)
                throw new Exception(ApiErrorHelper.GetErrorString(deleteResult) ?? "Unable to delete company.");

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

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Company successfully deleted.', 7000, true)";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "Company NOT successfully deleted. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Page();
        }
    }
}
