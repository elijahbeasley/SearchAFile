namespace SearchAFile.Web.Classes;

public class FileUploaderOptions
{
    public string DomId { get; set; } = "uploader-" + Guid.NewGuid().ToString("N");
    public string InputName { get; set; } = "Uploads"; // binds to List<IFormFile> Uploads
    public string[] AcceptedTypes { get; set; } = new[] { "image/*", "application/pdf" };
    public int PerFileLimitMB { get; set; } = 10; // per file
    public int TotalLimitMB { get; set; } = 25;   // all files combined
    public bool ShowDiagnostics { get; set; } = false; // show small debug panel
    public int MaxFiles { get; set; } = 0; // 0 or less = unlimited
    public int AlreadyUploadedCount { get; set; } = 0; // how many the user already has on server
    public int MaxFilenameLength { get; set; } = 100;     // 0 or less = unlimited
    public bool TruncateLongFilenames { get; set; } = true; // true = auto-truncate, false = reject
    public bool SimulateUpload { get; set; } = true;   // set false when wiring real uploads
}