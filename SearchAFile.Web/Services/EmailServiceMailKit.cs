using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using SearchAFile.Helpers;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Interfaces;

namespace SearchAFile.Services;

public class EmailServiceMailKit : IEmailService
{
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    public EmailServiceMailKit(IWebHostEnvironment iWebHostEnvironment)
    {
        _iWebHostEnvironment = iWebHostEnvironment;
    }
    public async Task SendEmail(List<KeyValuePair<string, string>> lstTo, List<KeyValuePair<string, string>> lstCC, List<KeyValuePair<string, string>> lstBCC, string strSubject, BodyBuilder objBodyBuilder)
    {
        try
        {
            if (_iWebHostEnvironment.IsDevelopment())
            {
                // BEGIN ***** TEST ***** BEGIN
                lstTo.Clear();
                lstCC.Clear();
                lstBCC.Clear();

                lstTo.Add(new KeyValuePair<string, string>("elijahmbeasley@gmail.com", "Elijah Beasley"));
                // END ***** TEST ***** END
            }

            IConfigurationRoot appSettingsJson = AppSettingsJson.GetAppSettings();

            //Build the email message.
            MimeMessage objMimeMessage = new MimeMessage();

            objMimeMessage.From.Add(MailboxAddress.Parse(appSettingsJson["Email:FromEmailAddress"]));
            objMimeMessage.Subject = strSubject;
            objMimeMessage.Body = BuildEmailBody(objBodyBuilder.HtmlBody).ToMessageBody();

            // To
            foreach (KeyValuePair<string, string> kvp in lstTo)
            {
                objMimeMessage.To.Add(new MailboxAddress(kvp.Value, kvp.Key));
            }

            // CC
            foreach (KeyValuePair<string, string> kvp in lstCC)
            {
                objMimeMessage.Cc.Add(new MailboxAddress(kvp.Value, kvp.Key));
            }

            // BCC
            foreach (KeyValuePair<string, string> kvp in lstBCC)
            {
                objMimeMessage.Bcc.Add(new MailboxAddress(kvp.Value, kvp.Key));
            }

            // Send the email message.
            using (SmtpClient objSmptClient = new SmtpClient())
            {
                await objSmptClient.ConnectAsync(appSettingsJson["Email:Host"], Convert.ToInt32(appSettingsJson["Email:Port"]), SecureSocketOptions.StartTls);
                await objSmptClient.AuthenticateAsync(appSettingsJson["Email:Username"], appSettingsJson["Email:Password"]);
                await objSmptClient.SendAsync(objMimeMessage);
                await objSmptClient.DisconnectAsync(true);
            }
        }
        catch
        {
            throw;
        }
    }
    public BodyBuilder BuildEmailBody(string strHTML)
    {
        BodyBuilder objBodyBuilder = new BodyBuilder();

        try
        {
            SystemInfo SystemInfo = HttpContextHelper.Current.Session.GetObject<SystemInfo>("SystemInfo");

            // Email Template 4:
            objBodyBuilder.HtmlBody = @"
                <html>
                <head>
                    <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
                    <style type='text/css'>
                        a { text-decoration: none; }
                        td { font-size: 15px; }
                    </style>
                </head>
                <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                        <tr>
                            <td align='center' style='padding: 15px;'>
                                <table role='presentation' width='600' cellspacing='0' cellpadding='0' border='0' style='width: 100%; max-width: 600px; background-color: #ffffff;'>
                                    <!-- Main Content -->
                                    <tr>
                                        <td style='padding: 15px;'>
                                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f8f8f8; border: 2px solid " + SystemInfo.SecondaryColor + @"; border-radius: 8px; padding: 15px;'>
                                                <tr>
                                                    <td align='center' style='padding-bottom: 15px;'>
                                                        <a href='" + SystemInfo.Url + @"'>
                                                            <img src='" + UrlHelper.Combine(SystemInfo.Url, "SystemFiles", SystemInfo.EmailLogo) + @"' alt='Logo' height='25'>
                                                        </a>
                                                    </td>
                                                </tr> 
                                                <tr> 
                                                    <td> 
                                                        " + strHTML + @"
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align='left' style='padding-top: 15px;'>
                                                        Please do not reply to this email. It is an unmonitored mailbox.
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align='left' style='padding-top: 15px;'>
                                                        Best regards,
                                                        <br>
                                                        The " + SystemInfo.SystemName + @" Team
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Footer -->
                                    <tr>
                                        <td align='center' style='padding-bottom: 15px; font-size: 11px; color: #666;'>
                                            <b>Note:</b> If you believe this message was not intended for you, please contact " + SystemInfo.ContactName + @" at <a href='mailto:" + SystemInfo.ContactEmailAddress + @"' style='color: #0f3063; text-decoration: none;'>" + SystemInfo.ContactEmailAddress + @"</a>.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";
        }
        catch
        {
            throw;
        }

        return objBodyBuilder;
    }
}