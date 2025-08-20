using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using SearchAFile.Core.Domain.Entities;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SearchAFile.Infrastructure.Mapping;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ImpersonateModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly AuthClient _loginService;

    public ImpersonateModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, AuthClient authClient)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _loginService = authClient;
    }
    public List<User>? Users { get; set; }

    [DisplayName("User")]
    [Required(ErrorMessage = "User is required.")]
    public Guid? id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Convert.ToBoolean(HttpContext.Session.GetBoolean("AllowUserImpersonation")))
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }

        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Impersonate User");

            HttpContext.Session.Remove("Users");

            await BuildUserSelectList();
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string strMessage;

        try
        {
            await BuildUserSelectList();

            var userResult = await _api.GetAsync<UserDto>($"users/{id}");

            if (!userResult.IsSuccess || userResult.Data == null)
                throw new Exception(ApiErrorHelper.GetErrorString(userResult) ?? "Unable to retrieve user.");

            UserDto? User = userResult.Data;

            // Check to see if there is an OriginalUser variable that needs to be saved. 
            UserDto? OriginalUser = null;

            if (HttpContext.Session.GetObject<UserDto>("OriginalUser") == default)
            {
                OriginalUser = HttpContext.Session.GetObject<UserDto>("User");
            }
            else
            {
                OriginalUser = HttpContext.Session.GetObject<UserDto>("OriginalUser");
            }

            // Log the user in. 
            var loginResult = await _loginService.LoginAsync("", "", id);

            if (loginResult.Success)
            {
                // Reset the AllowUserImpersonation session variable. 
                HttpContext.Session.SetBoolean("AllowUserImpersonation", true);

                // Get the user's company.
                var companyResult = await _api.GetAsync<Company>($"companies/{User.CompanyId}");

                if (!companyResult.IsSuccess || companyResult.Data == null)
                {
                    throw new Exception(companyResult.ErrorMessage ?? "Unable to get the user's company.");
                }

                HttpContext.Session.SetObject("Company", companyResult.Data);

                // If the selected user is different from the OriginalUser then set the OriginalUser variable. 
                if (User.UserId != OriginalUser.UserId)
                {
                    HttpContext.Session.SetObject("OriginalUser", OriginalUser);
                    strMessage = "You are now impersonating " + User.FullName + " (" + companyResult.Data.Company1 + ").";
                }
                else
                {
                    strMessage = "You are now impersonating " + User.FullName + " (" + companyResult.Data.Company1 + ").";
                }

                TempData["StartupJavaScript"] = "ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 7000, true)";

                return Redirect(SystemFunctions.GetDashboardURL(User?.Role));
            }
            else if (string.IsNullOrEmpty(loginResult.ErrorMessage))
            {
                TempData["StartupJavaScript"] = "ShowSnack('warning', 'Unable to impersonate user.', 7000, true)";
            }
            else
            {
                TempData["StartupJavaScript"] = "ShowToast('warning', 'Login Error', '" + loginResult.ErrorMessage.EscapeJsString() + "', 0, false)";
            }
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

        return Page();
    }

    public IActionResult OnGetReloadUser(string search)
    {
        try
        {
            if (string.IsNullOrEmpty(search))
            {
                Users = HttpContext.Session.GetObject<List<User>>("Users");
            }
            else
            {
                Users = HttpContext.Session.GetObject<List<User>>("Users")
                    .Where(user => !string.IsNullOrEmpty(search) && (user.FullName.Trim().ToLower().Contains(search.Trim().ToLower())
                        || user.FullNameReverse.Trim().ToLower().Contains(search.Trim().ToLower()))
                        || user.Company.Company1.Trim().ToLower().Contains(search.Trim().ToLower()))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");

            return StatusCode(400, strExceptionMessage);
        }

        return Page();
    }

    private async Task BuildUserSelectList()
    {
        try
        {
            if (HttpContext.Session.GetObject<List<User>>("Users") == null)
            {
                var userResult = await _api.GetAsync<List<User>>("users");

                if (!userResult.IsSuccess || userResult.Data == null)
                {
                    throw new Exception(userResult.ErrorMessage ?? "Unable to retrieve users.");
                }

                Users = userResult.Data;

                var companyResult = await _api.GetAsync<List<Company>>("companies");

                if (!companyResult.IsSuccess || companyResult.Data == null)
                {
                    throw new Exception(companyResult.ErrorMessage ?? "Unable to retrieve companies.");
                }

                List<Company>? Companies = companyResult.Data;

                UserCompanyMapper.MapCompaniesToUsers(Users, Companies);

                Users = Users
                    .Where(user => user.UserId != HttpContext.Session.GetObject<UserDto>("User").UserId)
                    .OrderBy(user => user.Company?.Company1)
                    .ThenBy(user => user.FullNameReverse)
                    .ToList();

                HttpContext.Session.SetObject("Users", Users);
            }
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