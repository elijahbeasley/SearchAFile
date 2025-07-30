using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Org.BouncyCastle.Asn1.Cmp;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace SearchAFile.Web.Pages.Common;

public class ChatModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public ChatModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }
    public FileGroup FileGroup { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        try
        {
            if (id == null)
                return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Chat");

            var fileGroupResult = await _api.GetAsync<FileGroup>($"filegroups/{id}");

            if (!fileGroupResult.IsSuccess || fileGroupResult.Data == null)
            {
                throw new Exception(fileGroupResult.ErrorMessage ?? "Unable to retrieve file group.");
            }

            FileGroup = fileGroupResult.Data;

        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");
        }

        return Page();
    }

    public void OnPost()
    {

    }
}
