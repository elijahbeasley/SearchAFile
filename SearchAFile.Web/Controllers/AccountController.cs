using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Org.BouncyCastle.Asn1.Cmp;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Controllers;
public class AccountController : Controller
{
    private readonly AuthClient _loginService;
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public AccountController(AuthClient loginService, TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _loginService = loginService;
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [HttpPost]
    public async Task<IActionResult> LogOut()
    {
        // Logout
        await _loginService.LogoutAsync();

        // Redirect.
        return RedirectToAction("Default", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> ChangeCompany([FromBody] Guid? id)
    {
        try
        {
            if (id == null)
                throw new Exception("No company ID was recieved.");

            var result = await _api.GetAsync<Company>($"companies/{id}");

            if (!result.IsSuccess || result.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to retrieve company.");

            HttpContext.Session.SetObject("Company", result.Data);

            List<SelectListItem> Companies = HttpContext.Session.GetObject<List<SelectListItem>>("Companies");
            //Companies = Companies?.Select(item => new SelectListItem
            //{
            //    Text = item.Text,
            //    Value = item.Value,
            //    Selected = item.Value == id.ToString()
            //}).ToList();

            Companies = Companies.Select(e => { e.Selected = e.Value.Equals(id.ToString()); return e; }).ToList();

            HttpContext.Session.SetObject("Companies", Companies);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            return BadRequest(strExceptionMessage);
        }
    }
}