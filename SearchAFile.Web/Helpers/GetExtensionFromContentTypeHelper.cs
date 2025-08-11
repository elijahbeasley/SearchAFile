using Microsoft.AspNetCore.StaticFiles;

namespace SearchAFile.Web.Helpers;

public static class GetExtensionFromContentTypeHelper
{
    public static string? GetExtensionFromContentType(IFormFile file)
    {
        // Reverse-lookup: ext by MIME (ContentType)
        var map = new FileExtensionContentTypeProvider();
        // provider is ext -> mime; we need the reverse
        var ext = map.Mappings
                     .FirstOrDefault(kv => string.Equals(kv.Value, file.ContentType, StringComparison.OrdinalIgnoreCase))
                     .Key;
        return ext; // e.g. ".png" for "image/png", or null if unknown
    }
}
