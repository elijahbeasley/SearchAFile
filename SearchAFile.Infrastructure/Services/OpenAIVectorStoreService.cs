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
/// Keeps a vector store alive with a single "ensure-or-repair" method.
/// - If the store is fine, returns the same id.
/// - If it's expired, we create a new store and reattach the provided file ids.
/// </summary>
public class OpenAIVectorStoreService : IOpenAIVectorStoreService
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _opts;
    private readonly ILogger<OpenAIVectorStoreService> _log;

    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);

    public OpenAIVectorStoreService(HttpClient http, IOptions<OpenAIOptions> opts, ILogger<OpenAIVectorStoreService> log)
    {
        _http = http;
        _opts = opts.Value;
        _log = log;

        // Minimal client setup
        _http.BaseAddress ??= new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        if (!string.IsNullOrWhiteSpace(_opts.OrganizationId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Organization", _opts.OrganizationId);
        if (!string.IsNullOrWhiteSpace(_opts.ProjectId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Project", _opts.ProjectId);
    }

    public async Task<string> EnsureReadyOrRepairAsync(
        string vectorStoreId,
        IEnumerable<string> existingOpenAiFileIds,
        string nameIfRecreated,
        IDictionary<string, string> metadataIfRecreated,
        CancellationToken ct = default)
    {
        // 1) Read store status
        using var get = await _http.GetAsync($"vector_stores/{vectorStoreId}", ct);
        var body = await get.Content.ReadAsStringAsync(ct);

        if (!get.IsSuccessStatusCode)
        {
            // If we can't read it (deleted/never existed), just recreate.
            _log.LogWarning("Vector store read failed ({Status}). Recreating.", (int)get.StatusCode);
            return await RecreateAndReattachAsync(nameIfRecreated, metadataIfRecreated, existingOpenAiFileIds, ct);
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var status = root.TryGetProperty("status", out var s) ? s.GetString() : "unknown";
        var expired = status == "expired";

        // If it exposes an expires_at, respect it (epoch seconds).
        if (root.TryGetProperty("expires_at", out var ea) && ea.ValueKind == JsonValueKind.Number)
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(ea.GetInt64());
            if (exp <= DateTimeOffset.UtcNow) expired = true;
        }

        // 2) If expired → recreate (and reattach all file ids you already track in your DB).
        if (expired == true)
        {
            _log.LogInformation("Vector store {VS} expired. Recreating and reattaching files.", vectorStoreId);
            return await RecreateAndReattachAsync(nameIfRecreated, metadataIfRecreated, existingOpenAiFileIds, ct);
        }

        // 3) If still indexing, we just tell caller to try again later (simple & safe).
        if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Vector store not ready. Current status: {status}");

        // 4) Good to go: return the same id.
        return vectorStoreId;
    }

    public async Task<string> CreateAsync(string name, IDictionary<string, string> metadata, int? expiresAfterDays = null, CancellationToken ct = default)
    {
        var payload = new
        {
            name,
            metadata,
            // Keep it simple—no expiry by default (adjust if you want).
            // Set to null = provider default; or use new { anchor="last_active_at", days = 90 }
            expires_after = expiresAfterDays is null ? null : new { anchor = "last_active_at", days = expiresAfterDays }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "vector_stores")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, J), Encoding.UTF8, "application/json")
        };
        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create vector store failed: {(int)res.StatusCode}. Body: {body}");

        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;
    }

    // --- tiny helper that does both: create + attach + poll ---
    private async Task<string> RecreateAndReattachAsync(string name, IDictionary<string, string> metadata, IEnumerable<string> openAiFileIds, CancellationToken ct)
    {
        var newId = await CreateAsync(name, metadata, null, ct);

        var ids = openAiFileIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ?? Array.Empty<string>();
        if (ids.Length == 0) return newId; // nothing to reattach

        // Single batch attach, then poll. Keep it tiny.
        var payload = new { file_ids = ids };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"vector_stores/{newId}/file_batches")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, J), Encoding.UTF8, "application/json")
        };
        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Attach batch failed: {(int)res.StatusCode}. Body: {body}");

        var batchId = JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;

        var timeoutAt = DateTime.UtcNow.AddSeconds(_opts.IndexingTimeoutSeconds);
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            using var r = await _http.GetAsync($"vector_stores/{newId}/file_batches/{batchId}", ct);
            var rb = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Batch poll failed: {(int)r.StatusCode}. Body: {rb}");

            var status = JsonDocument.Parse(rb).RootElement.GetProperty("status").GetString();
            if (status == "completed") return newId;
            if (status is "failed" or "canceled")
                throw new InvalidOperationException($"Batch {status}. Body: {rb}");

            if (DateTime.UtcNow >= timeoutAt)
                throw new TimeoutException($"Indexing timed out after {_opts.IndexingTimeoutSeconds}s. Last: {status}");

            await Task.Delay(_opts.PollIntervalMs, ct);
        }
    }

    public async Task<bool> DetachFileAsync(string vectorStoreId, string openAiFileId, CancellationToken ct = default)
    {
        using var res = await _http.DeleteAsync($"vector_stores/{vectorStoreId}/files/{openAiFileId}", ct);
        // Return true on 2xx, false on 404 (already detached); throw for other failures if you prefer
        if ((int)res.StatusCode == 404) return false;
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Detach failed: {(int)res.StatusCode}. Body: {body}");
        }
        return true;
    }
    

    public async Task<bool> DeleteAsync(string vectorStoreId, CancellationToken ct = default)
    {
        // Some endpoints require this beta header; harmless if already present
        _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Beta", "assistants=v2");

        using var res = await _http.DeleteAsync($"vector_stores/{vectorStoreId}", ct);
        if ((int)res.StatusCode == 404) return false; // already gone
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Vector store delete failed: {(int)res.StatusCode}. Body: {body}");
        }
        return true;
    }
}