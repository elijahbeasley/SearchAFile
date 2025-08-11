using SearchAFile.Core.Domain.Entities;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SearchAFile.Web.Services;

public class OpenAIFileService
{
    private readonly HttpClient _httpClient;
    public OpenAIFileService(IHttpClientFactory httpClient)
    {
        _httpClient = httpClient.CreateClient("SearchAFIleClient");
    }

    public async Task<bool> TryPostFileToOpenAIAsync(IFormFile file, string fileKey, List<string> allowedFileTypes, Action<string> assignOpenAIFileIdToModel, string newFileName)
    {
        try
        {
            if (file == null)
                throw new Exception($"{fileKey} is required.");

            string? extension = GetExtensionFromContentTypeHelper.GetExtensionFromContentType(file)?.TrimStart('.')?.ToLower();

            if (string.IsNullOrEmpty(extension)
                || !allowedFileTypes.Contains(extension))
            {
                string allowedTypesMsg = string.Join(", ", allowedFileTypes);
                throw new Exception($"Invalid file type. File must be of type: {allowedTypesMsg}.");
            }

            // Upload file to OpenAI
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var byteContent = new ByteArrayContent(memoryStream.ToArray());
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            using var multipart = new MultipartFormDataContent
            {
                { byteContent, "file", newFileName },
                { new StringContent("assistants"), "purpose" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/files")
            {
                Content = multipart
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI upload failed: {errorText}");
            }

            var json = await response.Content.ReadAsStringAsync();
            string? fileId = JsonDocument.Parse(json).RootElement.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(fileId))
            {
                throw new Exception("Unable to retrieve OpenAI file ID.");
            }

            // Assign to model
            assignOpenAIFileIdToModel(fileId);

            return true;
        }
        catch
        {
            throw;
        }
    }
    public async Task<List<OpenAIFile>> GetAllOpenAIFilesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.openai.com/v1/files");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to list OpenAI files: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json);
            var files = new List<OpenAIFile>();

            foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                files.Add(new OpenAIFile
                {
                    Id = element.GetProperty("id").GetString(),
                    FileName = element.GetProperty("filename").GetString(),
                    Purpose = element.GetProperty("purpose").GetString(),
                    Bytes = element.GetProperty("bytes").GetInt64(),
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(element.GetProperty("created_at").GetInt64()).UtcDateTime
                });
            }

            return files;
        }
        catch
        {
            throw;
        }
    }
}