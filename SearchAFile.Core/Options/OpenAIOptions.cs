namespace SearchAFile.Core.Options;

/// <summary>
/// Basic OpenAI config. Keep it minimal.
/// </summary>
public class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    public string? ProjectId { get; set; }

    /// <summary>Milliseconds to wait between polling file-batch status.</summary>
    public int PollIntervalMs { get; set; } = 750;

    /// <summary>Seconds to wait for indexing before we fail.</summary>
    public int IndexingTimeoutSeconds { get; set; } = 90;
    public int MaxFilesAllowed { get; set; }
}