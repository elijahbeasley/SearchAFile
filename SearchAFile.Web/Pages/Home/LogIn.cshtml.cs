using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Web;

namespace SearchAFile.Web.Pages.Home;

[BindProperties(SupportsGet = true)]
public class LogInModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AccountController AccountController;
    private readonly IEmailService _emailService;
    private readonly AuthClient _loginService;
    private readonly AuthenticatedApiClient _api;
    public LogInModel(TelemetryClient telemetryClient, IEmailService emailService, AccountController AC, AuthClient loginService, AuthenticatedApiClient api)
    {
        _telemetryClient = telemetryClient;
        _emailService = emailService;
        AccountController = AC;
        _loginService = loginService;
        _api = api;
    }

    public List<SelectListItem> UserSelectList { get; set; }

    [DisplayName("Email Address")]
    [RegularExpression(@"^([A-z0-9]|\.){2,}@[A-z0-9]{2,}\.[A-z0-9]{2,}$", ErrorMessage = "Invalid email address entered. Please enter an email address in the format: 'email@example.com'.")]
    [Required(ErrorMessage = "Email address is required.")]
    public string EmailAddress { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }

    public IActionResult OnGet(string email)
    {
        try
        {
            if (HttpContext.Session.GetObject<UserDto>("User") != null)
            {
                return Redirect(SystemFunctions.GetDashboardURL(HttpContext.Session.GetObject<UserDto>("User").Role));
            }

            if (!string.IsNullOrEmpty(email))
            {
                EmailAddress = HttpUtility.UrlDecode(email);
            }

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Log In");

            ModelState.Remove("EmailAddress");
            ModelState.Remove("Password");
            HttpContext.Session.Remove("ResetUserID");
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
            if (!ModelState.IsValid)
            {
                strMessage = "<ul><li>" + string.Join("</li><li>", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)) + "</li></ul>";
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'The following errors have occured', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").Replace("'", "\"") + "', 0, false)";
                return Page();
            }

            var loginResult = await _loginService.LoginAsync(EmailAddress, Password);

            if (loginResult.Success)
            {
                UserDto User = HttpContext.Session.GetObject<UserDto>("User");

                // If the user is a system admin, save all companies to a session variable.
                if (User.Role.Equals("System Admin"))
                {
                    // Get the user's company.
                    var result = await _api.GetAsync<List<Company>>("companies");

                    if (!result.IsSuccess || result.Data == null)
                    {
                        throw new Exception(result.ErrorMessage ?? "Unable to retrieve the companies.");
                    }

                    Company? Company = result.Data.FirstOrDefault(c => c.CompanyId == User.CompanyId);

                    if (Company == null)
                    {
                        throw new Exception(result.ErrorMessage ?? "Companies did not contain the user's company.");
                    }

                    List<SelectListItem> Companies = result.Data.Select(c => new SelectListItem
                    {
                        Text = c.Company1,
                        Value = c.CompanyId.ToString(),
                        Selected = c.CompanyId == User.CompanyId
                    }).ToList();

                    // Store in session
                    HttpContext.Session.SetObject("Companies", Companies);
                    HttpContext.Session.SetObject("Company", Company);
                }
                else
                {
                    // Get the user's company.
                    var result = await _api.GetAsync<Company>($"companies/{User.CompanyId}");

                    if (!result.IsSuccess || result.Data == null)
                    {
                        throw new Exception(result.ErrorMessage ?? "Unable to get the user's company.");
                    }

                    HttpContext.Session.SetObject("Company", result.Data);
                }

                TempData["StartupJavaScript"] = "ShowSnack('success', 'Login Successful!', 7000, true)";

                string strDashboardURL = SystemFunctions.GetDashboardURL(User.Role);
                HttpContext.Session.SetString("DashboardURL", strDashboardURL);

                return Redirect(strDashboardURL);
            }
            else if (string.IsNullOrEmpty(loginResult.ErrorMessage))
            {
                TempData["StartupJavaScript"] = "ShowSnack('warning', 'Unable to login.', 7000, true)";
            }
            else
            {
                TempData["StartupJavaScript"] = "ShowToast('warning', 'Login Error', '" + loginResult.ErrorMessage.EscapeJsString() + "', 0, false)";
            }
        }
        catch (Exception ex)
        {
            await _loginService.LogoutAsync();

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Redirect("/");
        }

        return Page();
    }

    public class CustomJsonObject
    {
        public bool booSuccess { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
    }

    public async Task<IActionResult> OnGetSendVerificationEmail(string emailAddress)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            //if (!string.IsNullOrEmpty(EmailAddress))
            //{
            //    EmailAddress = HttpUtility.UrlDecode(EmailAddress);

            //    User User = await SearchAFileContext.User
            //        .AsNoTracking()
            //        .FirstOrDefaultAsync(e => e.EmailAddress.Trim().ToLower().Equals(EmailAddress.Trim().ToLower()));

            //    if (User == null)
            //    {
            //        CustomJsonObject.ErrorMessage = "'" + emailAddress + "' is not a valid email address.";
            //        CustomJsonObject.booSuccess = false;
            //    }
            //    else
            //    {
            //        // Create the email verification info.
            //        User.EmailVerificationUrl = Guid.NewGuid().ToString("N");

            //        SearchAFileContext.Update(User);

            //        // The entered item is unique.
            //        await SearchAFileContext.SaveChangesAsync();

            //        SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

            //        // Send the password reset email.
            //        BodyBuilder objBodyBuilder = new BodyBuilder();

            //        objBodyBuilder.HtmlBody = @"
            //            <table> 
            //                <tr> 
            //                    <td> 
            //                        Hello " + User.FullName + @", 
            //                    </td> 
            //                </tr> 
            //                <tr> 
            //                    <td style='padding: 0rem 1rem;'> 
            //                        <br /> 
            //                        Please <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "VerifyEmailAddress") + "?ext=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address. 
            //                    </td> 
            //                </tr> 
            //            </table>";

            //        // To.
            //        List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

            //        // Add service to the email.
            //        lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FullName));

            //        // CC.
            //        List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

            //        // BCC.
            //        List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

            //        await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Verify Email Address", objBodyBuilder);
            //    }
            //}
            //else
            //{
            //    CustomJsonObject.ErrorMessage = "Email address was empty.";
            //    CustomJsonObject.booSuccess = false;
            //}
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ErrorMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ErrorMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ErrorMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }
}