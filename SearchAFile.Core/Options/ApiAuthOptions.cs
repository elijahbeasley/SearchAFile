namespace SearchAFile.Core.Options;

/// <summary>
/// Binds to the "ApiAuth" section (ClientId/ClientSecret) for your API headers.
/// </summary>
public class ApiAuthOptions
{
    public const string SectionName = "ApiAuth";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}