using MimeKit;

namespace SearchAFile.Services;

public interface IEmailService
{
    Task SendEmail(List<KeyValuePair<string, string>> lstTo, List<KeyValuePair<string, string>> lstCC, List<KeyValuePair<string, string>> lstBCC, string strSubject, BodyBuilder objBodyBuilder);
}
