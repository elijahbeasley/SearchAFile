using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchAFile.Core.Interfaces;
using SearchAFile.Core.Options;

namespace SearchAFile.Infrastructure.Services;

/// <summary>
/// Only uploads to OpenAI and attaches to a (ready) vector store.
/// Vector health is handled by VectorStoreService.
/// </summary>
public class OpenAIFileService : IOpenAIFileService
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _opts;
    private readonly ILogger<OpenAIFileService> _log;

    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);

    public OpenAIFileService(HttpClient http, IOptions<OpenAIOptions> opts, ILogger<OpenAIFileService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;

        _http.BaseAddress ??= new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        if (!string.IsNullOrWhiteSpace(_opts.OrganizationId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Organization", _opts.OrganizationId);
        if (!string.IsNullOrWhiteSpace(_opts.ProjectId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Project", _opts.ProjectId);
    }

    public async Task<string> UploadAndAttachAsync(string vectorStoreId, Stream content, string fileName, string? contentType = null, System.Threading.CancellationToken ct = default)
    {
        // 1) Upload the file to OpenAI (purpose=assistants)
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("assistants"), "purpose");

        var streamContent = new StreamContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        form.Add(streamContent, "file", fileName);

        using var uploadRes = await _http.PostAsync("files", form, ct);
        var uploadBody = await uploadRes.Content.ReadAsStringAsync(ct);
        if (!uploadRes.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI upload failed: {(int)uploadRes.StatusCode}. Body: {uploadBody}");

        var openAiFileId = JsonDocument.Parse(uploadBody).RootElement.GetProperty("id").GetString()!;

        // 2) Attach it to the vector store in a small batch and wait
        var payload = new { file_ids = new[] { openAiFileId } };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"vector_stores/{vectorStoreId}/file_batches")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, J), Encoding.UTF8, "application/json")
        };
        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Attach failed: {(int)res.StatusCode}. Body: {body}");

        var batchId = JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;
        var timeoutAt = DateTime.UtcNow.AddSeconds(_opts.IndexingTimeoutSeconds);
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            using var r = await _http.GetAsync($"vector_stores/{vectorStoreId}/file_batches/{batchId}", ct);
            var rb = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Batch poll failed: {(int)r.StatusCode}. Body: {rb}");

            var status = JsonDocument.Parse(rb).RootElement.GetProperty("status").GetString();
            if (status == "completed") return openAiFileId;
            if (status is "failed" or "canceled")
                throw new InvalidOperationException($"Batch {status}. Body: {rb}");

            if (DateTime.UtcNow >= timeoutAt)
                throw new TimeoutException($"Indexing timed out after {_opts.IndexingTimeoutSeconds}s. Last: {status}");

            await Task.Delay(_opts.PollIntervalMs, ct);
        }
    }

    public async Task DeleteAsync(string openAiFileId, CancellationToken ct = default)
    {
        using var res = await _http.DeleteAsync($"files/{openAiFileId}", ct);
        if ((int)res.StatusCode == 404) return; // already gone -> ignore
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"OpenAI file delete failed: {(int)res.StatusCode}. Body: {body}");
        }
    }
}