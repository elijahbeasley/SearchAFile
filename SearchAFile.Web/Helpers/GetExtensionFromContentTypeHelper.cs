using Microsoft.AspNetCore.StaticFiles;

namespace SearchAFile.Web.Helpers;

public static class GetExtensionFromContentTypeHelper
{
    public static class CommonFileTypes
    {
        // MIME type to extension mapping
        public static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { "image/jpeg", ".jpg" },  // covers .jpeg too
            { "image/png",  ".png" },
            { "image/gif",  ".gif" },
            { "image/webp", ".webp" },
            { "image/bmp",  ".bmp" },
            { "image/tiff", ".tiff" },
            { "image/svg+xml", ".svg" },

            // Documents
            { "application/pdf", ".pdf" },
            { "application/msword", ".doc" },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
            { "application/vnd.ms-excel", ".xls" },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
            { "application/vnd.ms-powerpoint", ".ppt" },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx" },
            { "text/plain", ".txt" },
            { "text/csv", ".csv" },
            { "application/rtf", ".rtf" },

            // Archives
            { "application/zip", ".zip" },
            { "application/x-7z-compressed", ".7z" },
            { "application/x-rar-compressed", ".rar" },
            { "application/gzip", ".gz" },

            // Code / Markup
            { "text/html", ".html" },
            { "application/json", ".json" },
            { "application/xml", ".xml" },
        };
    }

    public static string? GetExtensionFromContentType(IFormFile file)
    {
        if (CommonFileTypes.MimeToExtension.TryGetValue(file.ContentType, out var ext))
        {
            return ext;
        }

        return null; // or a default like ".bin"
    }
}
