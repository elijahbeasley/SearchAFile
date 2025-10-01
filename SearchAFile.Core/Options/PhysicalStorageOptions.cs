namespace SearchAFile.Core.Options;

/// <summary>
/// Where to store physical files for your app.
/// </summary>
public class PhysicalStorageOptions
{
    public const string SectionName = "Storage";
    /// <summary>Absolute or path relative to content root (e.g., "wwwroot/uploads").</summary>
    public string RootPath { get; set; } = "wwwroot/uploads";
}