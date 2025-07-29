namespace SearchAFile.Web.Services;

public interface ISMSService
{
    Task SendSMS(string strToPhoneNumber, string strMessage, string strTag = "");
}
