using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MimeKit;
using Newtonsoft.Json;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;

namespace SearchAFile.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChangePasswordModel : PageModel
{
    //private readonly TelemetryClient TelemetryClient;
    //private readonly AuthenticatedApiClient _api;
    //private IWebHostEnvironment IWebHostEnvironment;
    //private readonly IEmailService IEmailService;

    //public ChangePasswordModel(TelemetryClient TC, AuthenticatedApiClient api, IWebHostEnvironment IWHE, IEmailService IES)
    //{
    //    TelemetryClient = TC;
    //    _api = api;
    //    IWebHostEnvironment = IWHE;
    //    IEmailService = IES;
    //}

    //public Staff Staff { get; set; }

    //[DisplayName("New Password")]
    //[RegularExpression(@"^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Invalid password entered. Password must be between 8 and 30 characters long, contain at least one upper case letter, at least one lower case letter, and at least one number.")]
    //[Required(ErrorMessage = "New password is required.")]
    //[StringLength(30)]
    //public string NewPassword { get; set; }

    //[DisplayName("Repeat Password")]
    //[Required(ErrorMessage = "Repeat password is required.")]
    //[StringLength(30)]
    //public string RepeatPassword { get; set; }
    //public async Task OnGetAsync()
    //{
    //    try
    //    {
    //        string strMessage;

    //        if (HttpContext.Session.GetInt32("StaffID") == null)
    //        {
    //            // Output an error message.
    //            strMessage = "Unable to update password. Please report the following to " + HttpContext.Session.GetString("ContactInfo") + ": The Staff ID was null.";
    //            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Modify Error!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false);";
    //            return;
    //        }

    //        Staff = await SearchAFileContext.Staff
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.StaffId == HttpContext.Session.GetInt32("StaffID"));

    //        if (Staff == null)
    //        {
    //            // Output an error message.
    //            strMessage = "Unable to update password. Please report the following to " + HttpContext.Session.GetString("ContactInfo") + ": Staff was not found.";
    //            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Modify Error!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false);";
    //            return;
    //        }

    //        ModelState.Remove("NewPassword");
    //        ModelState.Remove("RepeatPassword");

    //        TempData["StartupJavaScript"] = "if (self !== top) { window.top.StopLoading('#divLoadingBlock'); window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowModal(); }";
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        TelemetryClient.TrackException(ExceptionTelemetry);

    //        // Display an error for the user.
    //        string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
    //        TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
    //    }
    //}

    //public class CustomJsonObject
    //{
    //    public bool booSuccess { get; set; } = true;
    //    public bool booIsEqual { get; set; } = true;
    //    public string ErrorMessage { get; set; } = "";
    //}

    //public async Task<IActionResult> OnGetIsCurrentPasswordEqual(string CurrentPassword)
    //{
    //    CustomJsonObject CustomJsonObject = new CustomJsonObject();

    //    try
    //    {
    //        Staff = await SearchAFileContext.Staff
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.StaffId == HttpContext.Session.GetObject<Staff>("Staff").StaffId);

    //        CustomJsonObject.booIsEqual = BCrypt.Net.BCrypt.Verify(CurrentPassword, Staff.Password);
    //    }
    //    catch (Exception ex)
    //    {
    //        CustomJsonObject.booSuccess = false;
    //        CustomJsonObject.ErrorMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

    //        // Is there an inner exception?
    //        if (ex.InnerException == null) // No.
    //        {
    //            CustomJsonObject.ErrorMessage += ex.Message;
    //        }
    //        else // Yes.
    //        {
    //            CustomJsonObject.ErrorMessage += ex.InnerException.Message;
    //        }

    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        TelemetryClient.TrackException(ExceptionTelemetry);
    //    }

    //    return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    //}

    //public async Task<IActionResult> OnGetIsNewPasswordEqual(string NewPassword)
    //{
    //    CustomJsonObject CustomJsonObject = new CustomJsonObject();

    //    try
    //    {
    //        Staff = await SearchAFileContext.Staff
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.StaffId == HttpContext.Session.GetObject<Staff>("Staff").StaffId);

    //        CustomJsonObject.booIsEqual = BCrypt.Net.BCrypt.Verify(NewPassword, Staff.Password);
    //    }
    //    catch (Exception ex)
    //    {
    //        CustomJsonObject.booSuccess = false;
    //        CustomJsonObject.ErrorMessage = "An error has occured. Please contact " + HttpContext.Session.GetString("ContactInfo") + " and report the following error: ";

    //        // Is there an inner exception?
    //        if (ex.InnerException == null) // No.
    //        {
    //            CustomJsonObject.ErrorMessage += ex.Message;
    //        }
    //        else // Yes.
    //        {
    //            CustomJsonObject.ErrorMessage += ex.InnerException.Message;
    //        }

    //        // Log the exception to Application Insights.
    //        ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
    //        TelemetryClient.TrackException(ExceptionTelemetry);
    //    }

    //    return new JsonResult(JsonConvert.SerializeObject(CustomJsonObject));
    //}

    //public async Task OnPostAsync()
    //{
    //    string strMessage = "";

    //    try
    //    {
    //        Staff = await SearchAFileContext.Staff
    //            .AsNoTracking()
    //            .FirstOrDefaultAsync(e => e.StaffId == HttpContext.Session.GetObject<Staff>("Staff").StaffId);


    //        Staff.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);

    //        SearchAFileContext.Update(Staff);

    //        // The entered item is unique.
    //        await SearchAFileContext.SaveChangesAsync();

    //        SystemInfo SystemInfo = HttpContext.Session.GetObject<SystemInfo>("SystemInfo");

    //        if (SystemInfo == null)
    //        {
    //            SystemInfo = await SearchAFileContext.SystemInfo.FirstOrDefaultAsync();
    //        }

    //        BodyBuilder objBodyBuilder = new BodyBuilder();

    //        objBodyBuilder.HtmlBody = @"
    //            <table> 
    //                <tr> 
    //                    <td> 
    //                        Hello " + Staff.FirstName + " " + Staff.LastName + @", 
    //                    </td> 
    //                </tr> 
    //                <tr> 
    //                    <td style='padding: 0rem 1rem;'> 
    //                        <br /> 
    //                        Your password has been successfully changed.
    //                    </td> 
    //                </tr> 
    //            </table> ";

    //        // To.
    //        List<KeyValuePair<string, string>> lstTo = new List<KeyValuePair<string, string>>();

    //        // Add service to the email.
    //        lstTo.Add(new KeyValuePair<string, string>(Staff.EmailAddress, Staff.FirstName + " " + Staff.LastName));

    //        // CC.
    //        List<KeyValuePair<string, string>> lstCC = new List<KeyValuePair<string, string>>();

    //        // BCC.
    //        List<KeyValuePair<string, string>> lstBCC = new List<KeyValuePair<string, string>>();

    //        await IEmailService.SendEmail(lstTo, lstCC, lstBCC, SystemInfo.SystemName + " - Password Successfully Changed", objBodyBuilder);

    //        // Output a success message.
    //        strMessage = "Password successfully changed.";
    //        TempData["StartupJavaScript"] = "window.top.CloseModal(); window.top.ClearToast(); window.top.ShowToast('success', 'Password Change Successful!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 7000, true);";
    //    }
    //    catch (Exception ex)
    //    {
    //        // Create the message.
    //        strMessage = "Password NOT successfully modified. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": ";

    //        // Is there an inner exception?
    //        if (ex.InnerException == null) // No.
    //        {
    //            // Append on the exception message.
    //            strMessage += ex.Message;
    //        }
    //        else // Yes.
    //        {
    //            // Append on the inner exception message.
    //            strMessage += ex.InnerException.Message;
    //        }

    //        // Output an error message.
    //        TempData["StartupJavaScript"] = "window.top.StopLoading('#divLoadingBlockModal'); window.top.ShowToast('danger', 'Password Change Error!', '" + strMessage.Replace("\r", " ").Replace("\n", "<br />").Replace("'", "\"") + "', 0, false);";
    //    }
    //}
}