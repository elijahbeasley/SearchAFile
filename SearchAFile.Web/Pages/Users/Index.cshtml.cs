using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Services;

namespace SearchAFile.Web.Pages.Users;

public class IndexModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IEmailService _emailService;

    public IndexModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IEmailService emailService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _emailService = emailService;
    }

    [BindProperty(SupportsGet = true)]
    public string? search { get; set; }
    public IList<User>? Users { get;set; } = default!;

    public async Task OnGetAsync()
    {
        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Maintain Users");

            string url = string.IsNullOrWhiteSpace(search)
                ? "users"
                : $"users?search={Uri.EscapeDataString(search)}";

            var result = await _api.GetAsync<List<User>>(url);

            if (!result.IsSuccess || result.Data == null)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to retrieve user.");
            }

            Users = result.Data
                .Where(u => u.CompanyId == HttpContext.Session.GetObject<Company>("Company").CompanyId)
                .OrderBy(user => user.FullNameReverse)
                .ToList();

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

    public async Task<IActionResult> OnGetResetPassword(Guid? id)
    {
        try
        {
            if (id == null)
                return BadRequest(new { error = "ID was null." });

            var result = await _api.GetAsync<List<User>>("users");

            if (!result.IsSuccess || result.Data == null)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to retrieve user.");
            }

            Users = result.Data
                .Where(u => u.CompanyId == HttpContext.Session.GetObject<Company>("Company").CompanyId)
                .OrderBy(user => user.FullNameReverse)
                .ToList();

            User? User = result.Data.FirstOrDefault(user => user.UserId == id);

            if (User != null)
            {
                // Create the reset info.
                User.ResetUrl = Guid.NewGuid().ToString();
                User.ResetExpiration = DateTime.Now.AddMinutes(10);
                bool booValidResetPin = false;
                do
                {
                    User.ResetPin = SystemFunctions.GenerateRandomString("1234567890", 6);

                    booValidResetPin = !Users.Any(user => user.UserId != User.UserId && !string.IsNullOrEmpty(user.ResetPin) && user.ResetPin.Equals(User.ResetPin));
                }
                while (!booValidResetPin);

                var updateResult = await _api.PutAsync<User>($"users/{User.UserId}", User);

                if (!updateResult.IsSuccess)
                {
                    throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
                }

                SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

                // Send the password reset email.
                string strLeftColumnWidth = "padding: 5px 8px; vertical-align:top; width:30%;";
                string strRightColumnWidth = "padding: 5px 9px; width: 70%;";
                string strRowStyle = "background-color: #DCDCDC;";
                string strAlternateRowStyle = "background-color: white;";

                BodyBuilder objBodyBuilder = new BodyBuilder();

                objBodyBuilder.HtmlBody = @"
                    <table> 
                        <tr> 
                            <td> 
                                Hello " + User.FullName + @", 
                            </td> 
                        </tr> 
                        <tr> 
                            <td style='padding: 1rem;'> 
                                Please follow the link below and enter the PIN to reset your password.
                            </td> 
                        </tr> 
                    </table>
                    <table cellpadding='0' cellspacing='0' style='border: 1px solid " + SystemInfo.PrimaryColor + @"; border-radius: 0.2rem; border-spacing: 0; margin: auto;'>
                        <tr style='" + strRowStyle + @"'>
                            <td style='" + strLeftColumnWidth + @" border-top-left-radius: 0.2rem;'>
                                <b>Link</b>
                            </td>
                            <td style='" + strRightColumnWidth + @" border-top-right-radius: 0.2rem;'>
                                <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "ResetPassword") + "?id=" + User.ResetUrl + @"'>Reset Password</a>
                            </td>
                        </tr>
                        <tr style='" + strAlternateRowStyle + @"'>
                            <td style='" + strLeftColumnWidth + @"'>
                                <b>Reset PIN</b>
                            </td>
                            <td style='" + strRightColumnWidth + @"'>
                                " + User.ResetPin + @"
                            </td>
                        </tr>
                        <tr style='" + strRowStyle + @"'>
                            <td style='" + strLeftColumnWidth + @" border-bottom-left-radius: 0.2rem;'>
                                <b>Expires</b>
                            </td>
                            <td style='" + strRightColumnWidth + @" border-bottom-right-radius: 0.2rem;'>
                                " + User.ResetExpiration?.ToString("dddd, d/M/yyyy 'at' h:mm tt") + @"
                            </td>
                        </tr>
                    </table> ";

                // To.
                List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

                // Add service to the email.
                lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FirstName + " " + User.LastName));

                // CC.
                List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

                // BCC.
                List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

                await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Password Reset Link", objBodyBuilder);
            }
            else
            {
                return BadRequest(new { error = "'" + id.ToString() + "' is not a valid ID." });
            }
        }
        catch (Exception ex)
        {
            string strException = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                strException += ex.Message;
            }
            else // Yes.
            {
                strException += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Return 400 with JSON error
            return BadRequest(new { error = strException });
        }

        return new JsonResult(new { success = true });
    }
}
