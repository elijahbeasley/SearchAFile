using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mappers;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Services;
using System.Data;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace SearchAFile.Web.Pages.FileGroups;

public class IndexModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;

    public IndexModel(TelemetryClient telemetryClient, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public List<FileGroup>? FileGroups { get;set; } = default!;

    public async Task OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain File Groups");

            string url = string.IsNullOrWhiteSpace(search)
                ? "filegroups"
                : $"filegroups?search={Uri.EscapeDataString(search)}";

            var fileGroupsResult = await _api.GetAsync<List<FileGroup>>(url);

            if (!fileGroupsResult.IsSuccess || fileGroupsResult.Data == null)
            {
                throw new Exception(fileGroupsResult.ErrorMessage ?? "Unable to retrieve file group.");
            }

            FileGroups = fileGroupsResult.Data.Where(file_group => file_group.CompanyId == HttpContext.Session.GetObject<Company>("Company").CompanyId).ToList();

            var usersResult = await _api.GetAsync<List<UserDto>>("users");

            if (!usersResult.IsSuccess || usersResult.Data == null)
            {
                throw new Exception(usersResult.ErrorMessage ?? "Unable to retrieve users.");
            }

            List<UserDto> Users = usersResult.Data;

            FileGroupUserMapper.MapUserNamesToFileGroups(FileGroups, Users);

            var filesResult = await _api.GetAsync<List<Core.Domain.Entities.File>>("files");

            if (!filesResult.IsSuccess || filesResult.Data == null)
            {
                throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
            }

            List<Core.Domain.Entities.File> Files = filesResult.Data;

            FileGroupFileCountMapper.MapFilesCountToFileGroups(FileGroups, Files);

            ModelState.Remove("search");
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
