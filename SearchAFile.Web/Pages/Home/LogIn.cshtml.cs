using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Services;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace SearchAFile.Pages.Home;

[BindProperties(SupportsGet = true)]
public class LogInModel : PageModel
{
    private readonly TelemetryClient TelemetryClient;
    private readonly AccountController AccountController;
    private readonly IEmailService IEmailService;
    private readonly LoginService _loginService;
    public LogInModel(TelemetryClient TC, IEmailService IES, AccountController AC, LoginService loginService)
    {
        TelemetryClient = TC;
        IEmailService = IES;
        AccountController = AC;
        _loginService = loginService;
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
                return Redirect(SystemFunctions.GetDashboardURL(HttpContext.Session.GetString("Role")));
            }

            if (!string.IsNullOrEmpty(email))
            {
                EmailAddress = HttpUtility.UrlDecode(email);
            }

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Log In");

            // Set the message.
            HttpContext.Session.SetString("Message", "Use your emaill address and password to log in below.");
            HttpContext.Session.SetString("MessageColor", "default");

            ModelState.Remove("EmailAddress");
            ModelState.Remove("Password");
            HttpContext.Session.Remove("ResetUserID");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

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
                TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'The following errors have occured', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false)";
                return Page();
            }

            var loginResult = await _loginService.LoginAsync(EmailAddress, Password);

            if (loginResult.Success)
            {
                Redirect("/Admins/Dasboard");
            }
            else if (string.IsNullOrEmpty(loginResult.ErrorMessage))
            {
                TempData["StartupJavaScript"] = "ShowSnack('warning', 'Unable to login.', 10000, false)";
            }
            else
            {
                TempData["StartupJavaScript"] = "ShowSnack('warning', '" + loginResult.ErrorMessage.EscapeJsString() + "', 10000, false)";
            }
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            TelemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
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
            //                        Hello " + User.FirstName + " " + User.LastName + @", 
            //                    </td> 
            //                </tr> 
            //                <tr> 
            //                    <td style='padding: 0rem 1rem;'> 
            //                        <br /> 
            //                        Please <a href='" + UrlHelper.Combine(SystemInfo.SystemUrl, "Home", "VerifyEmailAddress") + "?ext=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address. 
            //                    </td> 
            //                </tr> 
            //            </table>";

            //        // To.
            //        List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

            //        // Add service to the email.
            //        lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FirstName + " " + User.LastName));

            //        // CC.
            //        List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

            //        // BCC.
            //        List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

            //        await IEmailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Verify Email Address", objBodyBuilder);
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
            TelemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }
}