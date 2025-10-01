using Microsoft.AspNetCore.Http;

namespace SearchAFile.Core.Interfaces;

/// <summary>
/// Saves the uploaded file to your app's physical storage.
/// </summary>
public interface IPhysicalFileService
{
    /// <summary>
    /// Saves an IFormFile to disk (creates directories as needed) and returns the absolute path.
    /// </summary>
    Task<string> SaveAsync(
        IFormFile file,
        string subfolder, // e.g. "companies/{companyId}/collections/{collectionId}"
        CancellationToken ct = default);

    /// <summary>
    /// Opens a readable stream for a previously-saved physical file path.
    /// </summary>
    Stream OpenRead(string absolutePath);
}