using Azure.Communication.Sms;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Helpers;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Web;

namespace SearchAFile.Web.Pages.Home;

[BindProperties(SupportsGet = true)]
public class ResetPasswordModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IEmailService _emailService;
    private readonly ISMSService _smsService;
    private readonly IConfiguration _configuration;
    public ResetPasswordModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IEmailService emailService, ISMSService smsService, IConfiguration configuration)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _emailService = emailService;
        _smsService = smsService;
        _configuration = configuration;
    }

    [DisplayName("Email Address")]
    [RegularExpression(@"^([A-z0-9]|\.){2,}@[A-z0-9]{2,}.[A-z0-9]{2,}$", ErrorMessage = "Invalid email address entered. Please enter an email address in the format: 'email@example.com'.")]
    [Required(ErrorMessage = "Email address is required.")]
    public string EmailAddress { get; set; }

    [DisplayName("Reset PIN")]
    [Required(ErrorMessage = "Reset PIN is required.")]
    [StringLength(6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid reset PIN entered. The reset PIN must be 6 characters long.")]
    public string ResetPin { get; set; }

    [DisplayName("New Password")]
    [RegularExpression(@"^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Invalid password entered. Password must be between 8 and 30 characters long, contain at least one upper case letter, at least one lower case letter, and at least one number.")]
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(30)]
    public string NewPassword { get; set; }

    [DisplayName("Repeat Password")]
    [Required(ErrorMessage = "Repeat password is required.")]
    [StringLength(30)]
    public string RepeatPassword { get; set; }

    private IList<User> Users { get; set; }
    private User? User { get; set; }

    public async Task OnGet(string email, string id)
    {
        try
        {
            if (!string.IsNullOrEmpty(id))
            {
                await LoadData();

                User = Users.FirstOrDefault(user => !string.IsNullOrEmpty(user.ResetUrl) && user.ResetUrl.Equals(id));
                
                if (User == null)
                {
                    string strMessage = "Invalid reset password link.";
                    TempData["StartupJavaScript"] = "window.top.ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
                }
                else if (!Convert.ToBoolean(User.Active))
                {
                    string strMessage = "Your account is inactive. If you believe this is a mistake, please contact " + HttpContext.Session.GetString("ContactInfo") + ".";
                    TempData["StartupJavaScript"] = "window.top.ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
                }
                else if (!User.EmailVerified)
                {
                    string strMessage = "Your email address has not been verified. <button id='btnSendVerificationEmail' class='btn btn-link p-0 m-0 cus-no-box-shadow fs-6' onclick='SendVerificationEmail();' style='cursor: pointer;'>Click here</button> to resend the email address verification email.";
                    TempData["StartupJavaScript"] = "window.top.ShowSnack('warning', '" + strMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
                }
                else 
                {
                    HttpContext.Session.SetObject("ResetUserID", User.UserId);
                    TempData["StartupJavaScript"] = "NextStep(1); $('.cus-auth-code-char').focus();";
                }
            }
            else if (!string.IsNullOrEmpty(email))
            {
                EmailAddress = HttpUtility.HtmlDecode(email).Trim();
            }

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Reset Password");

            ModelState.Remove("EmailAddress");
            ModelState.Remove("ResetPin");
            ModelState.Remove("NewPassword");
            ModelState.Remove("RepeatPassword");
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

    private async Task LoadData()
    {
        try
        {
            var result = await _api.GetAsync<List<User>>("users");

            if (!result.IsSuccess || result.Data == null)
            {
                throw new Exception(result.ErrorMessage ?? "Unable to retrieve user.");
            }

            Users = result.Data;

            User = Users.FirstOrDefault(user => user.UserId == HttpContext.Session.GetObject<Guid>("ResetUserID"));
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

    public class CustomJsonObject
    {
        public bool booSuccess { get; set; } = false;
        public string MessageType { get; set; } = "Snack";
        public string MessageDisplay { get; set; } = "success";
        public string ResponseMessage { get; set; } = "";
    }

    public async Task<IActionResult> OnGetFindAccount(string EmailAddress, string DeliveryType)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            await LoadData();

            if (!string.IsNullOrEmpty(EmailAddress))
            {
                EmailAddress = HttpUtility.UrlDecode(EmailAddress);

                User = Users.FirstOrDefault(user => user.EmailAddress.Trim().ToLower().Equals(EmailAddress.Trim().ToLower()));

                HttpContext.Session.SetObject("ResetUserID", User.UserId);

                if (User == null)
                {
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.ResponseMessage = "No user with the email address '" + EmailAddress + "' was found.";
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else if (!Convert.ToBoolean(User.Active))
                {
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.ResponseMessage = "Your account is inactive. If you believe this is a mistake, please contact " + HttpContext.Session.GetString("ContactInfo") + ".";
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else if (!User.EmailVerified)
                {
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.ResponseMessage = "Your email address has not been verified. <button id='btnSendVerificationEmail' class='btn btn-link p-0 m-0 cus-no-box-shadow fs-6' onclick='SendVerificationEmail();' style='cursor: pointer;'>Click here</button> to resend the email address verification email.";
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else if (string.IsNullOrEmpty(DeliveryType))
                {
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.ResponseMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": PIN delivery type is null.";
                    CustomJsonObject.MessageType = "danger";
                    CustomJsonObject.MessageDisplay = "Toast";
                }
                else 
                {
                    if (DeliveryType.Equals("S"))
                    {
                        if (string.IsNullOrEmpty(User.PhoneNumber))
                        {
                            CustomJsonObject.booSuccess = false;
                            CustomJsonObject.ResponseMessage = "The entered email address does not have a phone number associated with it. To reset your password, please select to receive the PIN via Email.";
                            CustomJsonObject.MessageType = "warning";
                            CustomJsonObject.MessageDisplay = "Snack";
                        }
                        else if (PhoneNumberHelper.CleanPhoneNumber(User.PhoneNumber).Length != 10)
                        {
                            CustomJsonObject.booSuccess = false;
                            CustomJsonObject.ResponseMessage = "The entered email address does not have a valid phone number associated with it. To reset your password, please select to receive the PIN via Email.";
                            CustomJsonObject.MessageType = "warning";
                            CustomJsonObject.MessageDisplay = "Snack";
                        }
                        else
                        {
                            // Create the reset info.
                            bool booValidResetPin = false;
                            do
                            {
                                User.ResetPin = SystemFunctions.GenerateRandomString("1234567890", 6);

                                booValidResetPin = !Users.Any(user => user.UserId != User.UserId && !string.IsNullOrEmpty(user.ResetPin) && user.ResetPin.Equals(User.ResetPin));
                            }
                            while (!booValidResetPin);

                            User.ResetExpiration = DateTime.Now.AddMinutes(10);

                            // Update the user.
                            var result = await _api.PutAsync<User>($"users/{User.UserId}", User);

                            if (!result.IsSuccess)
                            {
                                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
                            }

                            SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

                            // Send a SMS message.
                            string strToPhoneNumber = "+1" + PhoneNumberHelper.CleanPhoneNumber(User.PhoneNumber);

                            string strMessage = "Your " + SystemInfo.SystemName + " password reset pin is " + User.ResetPin.ToString() + ". It will expire in 10 minutes (" + User.ResetExpiration?.ToString("dddd, M/d/yyyy 'at' h:mm tt") + "). Please do not share this with anyone.";

                            await _smsService.SendSMS(strToPhoneNumber, strMessage, "Password reset");

                            CustomJsonObject.ResponseMessage = "Your password reset PIN has been sent to <b>" + User.PhoneNumber + "</b>. The reset PIN will expire in 10 minutes (" + User.ResetExpiration?.ToString("dddd, d/M/yyyy 'at' h:mm tt") + ") and may only be used once.";

                            CustomJsonObject.booSuccess = true;
                            CustomJsonObject.MessageType = "success";
                            CustomJsonObject.MessageDisplay = "Toast";
                        }
                    }
                    else
                    {
                        // Create the reset info.
                        bool booValidResetPin = false;
                        do
                        {
                            User.ResetPin = SystemFunctions.GenerateRandomString("1234567890", 6);

                            booValidResetPin = !Users.Any(user => user.UserId != User.UserId && !string.IsNullOrEmpty(user.ResetPin) && user.ResetPin.Equals(User.ResetPin));
                        }
                        while (!booValidResetPin);

                        User.ResetExpiration = DateTime.Now.AddMinutes(10);

                        // Update the user.
                        var result = await _api.PutAsync<User>($"users/{User.UserId}", User);

                        if (!result.IsSuccess)
                        {
                            throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
                        }

                        SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

                        string strMessage = "Your " + SystemInfo.SystemName + " password reset pin is <b>" + User.ResetPin.ToString() + "</b>. It will expire in 10 minutes (" + User.ResetExpiration?.ToString("dddd, d/M/yyyy 'at' h:mm tt") + "). Please do not share this with anyone.";

                        // Send the password reset email.
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
                                        " + strMessage + @"
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

                        await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Password Reset PIN", objBodyBuilder);

                        CustomJsonObject.ResponseMessage = "Your password reset PIN has been sent to <b>" + User.EmailAddress + "</b>. The reset PIN will expire in 10 minutes (" + User.ResetExpiration?.ToString("dddd, d/M/yyyy 'at' h:mm tt") + ") and may only be used once.";

                        CustomJsonObject.booSuccess = true;
                        CustomJsonObject.MessageType = "success";
                        CustomJsonObject.MessageDisplay = "Toast";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ResponseMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";
            CustomJsonObject.MessageType = "danger";
            CustomJsonObject.MessageDisplay = "Toast";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ResponseMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ResponseMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }

    public async Task<IActionResult> OnGetSendVerificationEmail(string emailAddress)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            if (string.IsNullOrEmpty(EmailAddress))
            {
                CustomJsonObject.ResponseMessage = "Email address is empty.";
                CustomJsonObject.booSuccess = false;
                CustomJsonObject.MessageType = "warning";
                CustomJsonObject.MessageDisplay = "Snack";
            }
            else 
            {
                await LoadData();

                EmailAddress = HttpUtility.UrlDecode(EmailAddress).Trim().ToLower();

                User = Users.FirstOrDefault(user => user.EmailAddress.Trim().ToLower().Equals(EmailAddress));

                if (User == null)
                {
                    CustomJsonObject.ResponseMessage = "No user member exists with email address '" + emailAddress + "'.";
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else
                {
                    // Create the email verification info.
                    User.EmailVerificationUrl = Guid.NewGuid();

                    // Update the user.
                    var result = await _api.PutAsync<User>($"users/{User.UserId}", User);

                    if (!result.IsSuccess)
                    {
                        throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
                    }

                    SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

                    // Send the password reset email.
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
                                    Please <a href='" + UrlHelper.Combine(SystemInfo.Url, "Home", "VerifyEmailAddress") + "?id=" + User.EmailVerificationUrl + @"'>click here</a> to verify your email address. 
                                </td> 
                            </tr> 
                        </table>";

                    // To.
                    List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

                    // Add service to the email.
                    lstTo.Add(new KeyValuePair<string, string>(User.EmailAddress, User.FullName));

                    // CC.
                    List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

                    // BCC.
                    List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

                    await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Verify Email Address", objBodyBuilder);

                    CustomJsonObject.ResponseMessage = "Email address verification email successfully sent to '" + EmailAddress + "'.";
                    CustomJsonObject.booSuccess = true;
                    CustomJsonObject.MessageType = "success";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
            }
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ResponseMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";
            CustomJsonObject.MessageType = "danger";
            CustomJsonObject.MessageDisplay = "Toast";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ResponseMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ResponseMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }

    public async Task<IActionResult> OnGetVerifyPin(string ResetPin)
    {
        CustomJsonObject CustomJsonObject = new CustomJsonObject();

        try
        {
            await LoadData();

            if (User != null)
            {
                if (!User.ResetPin.Equals(ResetPin))
                {
                    CustomJsonObject.ResponseMessage = "Invalid reset PIN entered";
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else if (DateTime.Now > User.ResetExpiration)
                {
                    CustomJsonObject.ResponseMessage = "Reset PIN has expired. Please request a new reset PIN.";
                    CustomJsonObject.booSuccess = false;
                    CustomJsonObject.MessageType = "warning";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
                else
                {
                    CustomJsonObject.booSuccess = true;
                    CustomJsonObject.ResponseMessage = "PIN successfully validated";
                    CustomJsonObject.MessageType = "success";
                    CustomJsonObject.MessageDisplay = "Snack";
                }
            }
            else
            {
                throw new Exception("Unable to load user member.");
            }
        }
        catch (Exception ex)
        {
            CustomJsonObject.booSuccess = false;
            CustomJsonObject.ResponseMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";
            CustomJsonObject.MessageType = "danger";
            CustomJsonObject.MessageDisplay = "Toast";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                CustomJsonObject.ResponseMessage += ex.Message;
            }
            else // Yes.
            {
                CustomJsonObject.ResponseMessage += ex.InnerException.Message;
            }

            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);
        }

        return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        bool booSuccess = false;
        string strMessage = "";

        try
        {
            await LoadData();

            User.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            User.ResetUrl = null;
            User.ResetExpiration = null;
            User.ResetPin = null;

            // Update the user.
            var result = await _api.PutAsync<User>($"users/{User.UserId}", User);

            if (!result.IsSuccess)
            {
                throw new Exception(ApiErrorHelper.GetErrorString(result) ?? "Unable to update user.");
            }

            booSuccess = true;
            HttpContext.Session.Remove("ResetUserID");

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
                            Your password has been successfully reset. If you did not request a password change, please contact " + HttpContext.Session.GetString("ContactInfo") + @" immediately.
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

            await _emailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Password Successfully Reset", objBodyBuilder);

            // Output a success message.
            strMessage = "Your password has been successfully reset";
            TempData["StartupJavaScript"] = "ClearToast(); window.top.ShowSnack('success', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 10000, true);";
        }
        catch (Exception ex)
        {
            // Create the message.
            strMessage = "Password NOT successfully reset. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": ";

            // Is there an inner exception?
            if (ex.InnerException == null) // No.
            {
                // Append on the exception message.
                strMessage += ex.Message;
            }
            else // Yes.
            {
                // Append on the inner exception message.
                strMessage += ex.InnerException.Message;
            }

            // Output an error message.
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Password Reset Error!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false);";
        }

        if (booSuccess)
        {
            return Redirect("~/Home/LogIn");
        }
        else
        {
            return Page();
        }
    }
}