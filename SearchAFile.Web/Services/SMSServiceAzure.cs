using Azure.Communication.Sms;
using Microsoft.AspNetCore.Hosting;
using SearchAFile.Web.Services;

namespace SearchAFile.Services;

public class SMSServiceAzure : ISMSService
{
    private readonly IWebHostEnvironment _iWebHostEnvironment;
    public SMSServiceAzure(IWebHostEnvironment iWebHostEnvironment)
    {
        _iWebHostEnvironment = iWebHostEnvironment;
    }
    public async Task SendSMS(string strToPhoneNumber, string strMessage, string strTag = "")
    {
        try
        {
            if (_iWebHostEnvironment.IsDevelopment())
            {
                // BEGIN ***** TEST ***** BEGIN
                strToPhoneNumber = "+17653411464";
                // END ***** TEST ***** END
            }

            IConfigurationRoot appSettingsJson = AppSettingsJson.GetAppSettings();
            string strFromPhoneNumber = appSettingsJson["AzureSMS:PhoneNumber"];
            string strConnectionString = appSettingsJson["AzureSMS:APIKey"];

            SmsClient SmsClient = new SmsClient(strConnectionString);

            await SmsClient.SendAsync(
                from: strFromPhoneNumber,
                to: new string[] { strToPhoneNumber },
                message: strMessage,
                options: new SmsSendOptions(enableDeliveryReport: true)
                {
                    Tag = strTag
                }
            );
        }
        catch
        {
            throw;
        }
    }
}
