using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SearchAFile.Core.Interfaces;
using SearchAFile.Core.Options;

namespace SearchAFile.Infrastructure.Services;

/// <summary>
/// Tiny helper to save files to disk under a configured root.
/// </summary>
public class PhysicalFileService : IPhysicalFileService
{
    private readonly string _root;

    public PhysicalFileService(IOptions<PhysicalStorageOptions> opts, IHostEnvironment env)
    {
        // If you pass a relative path like "wwwroot/uploads", make it absolute.
        _root = System.IO.Path.IsPathFullyQualified(opts.Value.RootPath)
            ? opts.Value.RootPath
            : System.IO.Path.Combine(env.ContentRootPath, opts.Value.RootPath);

        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken ct = default)
    {
        // Ensure folder exists: e.g., "companies/{companyId}/collections/{collectionId}"
        var folder = Path.Combine(_root, subfolder);
        Directory.CreateDirectory(folder);

        // Generate a safe unique filename (original name preserved at the end).
        var safeName = string.Join("_", file.FileName.Split(Path.GetInvalidFileNameChars()));
        var unique = $"{Guid.NewGuid():N}_{safeName}";
        var path = Path.Combine(folder, unique);

        // Stream to disk
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(fs, ct);
        return path; // absolute
    }

    public Stream OpenRead(string absolutePath) => new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
}