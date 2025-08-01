using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.SystemAdmins;

public class DashboardModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public DashboardModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public List<FileGroup>? FileGroups { get; set; } = default!;
    public async Task OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Dashboard");

            string url = string.IsNullOrWhiteSpace(search)
                ? "filegroups"
                : $"filegroups?search={Uri.EscapeDataString(search)}";

            var fileGroupsResult = await _api.GetAsync<List<FileGroup>>(url);

            if (!fileGroupsResult.IsSuccess || fileGroupsResult.Data == null)
            {
                throw new Exception(fileGroupsResult.ErrorMessage ?? "Unable to retrieve file group.");
            }

            FileGroups = fileGroupsResult.Data
                .Where(file_group => file_group.CompanyId == HttpContext.Session.GetObject<Company>("Company").CompanyId)
                .OrderBy(file_group => file_group.FileGroup1)
                .ToList();

            var filesResult = await _api.GetAsync<List<Core.Domain.Entities.File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            List<Core.Domain.Entities.File> Files = filesResult.Data;

            FileGroupFileCountMapper.MapFilesCountToFileGroups(FileGroups, Files);
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
}
