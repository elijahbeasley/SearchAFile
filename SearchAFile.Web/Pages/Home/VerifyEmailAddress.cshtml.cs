using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Services;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Services;
using System.Diagnostics;
using System.Reflection;

namespace SearchAFile.Web.Pages.Home;

[BindProperties(SupportsGet = true)]
public class VerifyEmailAddressModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IEmailService _emailService;
    public VerifyEmailAddressModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IEmailService emailService)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _emailService = emailService;
    }

    public User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        string strMessage = "";

        try
        {
            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Verify Email Address");

            if (id == null)
            {
                return Redirect("/");
            }

            var result = await _api.GetAsync<List<User>>("users");

            if (!result.IsSuccess || result.Data == null)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to retrieve user.");
            }

            List<User> Users = result.Data;

            User = Users.FirstOrDefault(user => user.EmailVerificationUrl == id);

            if (User == null)
            {
                // Output an error message.
                strMessage = "Invalid email address verification link";
                TempData["StartupJavaScript"] = "ClearToast(); window.top.ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 10000, true);";
                return Redirect("~/Home/LogIn");
            }

            if (!User.EmailVerified)
            {
                User.EmailVerified = true;

                // Update the user.
                var updateResult = await _api.PutAsync<User>($"users/{User.UserId}", User);

                if (!updateResult.IsSuccess)
                {
                    throw new Exception(ApiErrorHelper.GetErrorString(updateResult) ?? "Unable to update user.");
                }

                SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

                BodyBuilder objBodyBuilder = new BodyBuilder();

                objBodyBuilder.HtmlBody = @"
                    <table> 
                        <tr> 
                            <td> 
                                Hello " + User.FullName + @", 
                            </td> 
                        </tr> 
                        <tr> 
                            <td style='padding: 0rem 1rem;'> 
                                <br /> 
                                Your email address has been <b>successfully verified</b>.
                            </td> 
                        </tr> 
                    </table> ";

                // To.
                List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

                // Add service to the email.
                lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FullName));

                // CC.
                List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

                // BCC.
                List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

                await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Email Address Successfully Verified", objBodyBuilder);
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
}
