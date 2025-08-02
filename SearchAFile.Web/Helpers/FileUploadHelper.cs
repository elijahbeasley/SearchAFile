using Microsoft.AspNetCore.Http.Metadata;

namespace SearchAFile.Web.Helpers;

public static class FileUploadHelper
{
    public static async Task<bool> TryUploadFileAsync(IFormFile file, string fileKey, string strPath, List<string> allowedFileTypes, Action<string>? assignFileNameToModel = null, string? newFileName = null, string? deleteFile = null)
    {
        try
        {
            if (file == null)
            {
                throw new Exception(fileKey + " is required.");
            }

            string extension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (!allowedFileTypes.Contains(extension))
            {
                string allowedTypesMsg = string.Join(", ", allowedFileTypes);
                throw new Exception($"Invalid file type. File must be of type: {allowedTypesMsg}.");
            }

            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }

            if (!string.IsNullOrEmpty(deleteFile)
                && !deleteFile.Equals("Generic.jpg"))
            {
                // Delete the old tile image from the folder.
                string strDeletePath = Path.Combine(strPath, deleteFile);

                if (System.IO.File.Exists(strDeletePath))
                {
                    System.IO.File.Delete(strDeletePath);
                }
            }

            if (string.IsNullOrEmpty(newFileName))
            {
                newFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            }

            using var stream = new FileStream(Path.Combine(strPath, newFileName), FileMode.Create);
            await file.CopyToAsync(stream);

            if (assignFileNameToModel != null)
            {
                assignFileNameToModel(newFileName);
            }

            return true;
        }
        catch
        {
            throw;
        }
    }
}