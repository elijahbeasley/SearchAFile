// Infrastructure.Services
using Microsoft.Extensions.Options;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Core.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SearchAFile.Infrastructure.Services;

public class OpenAIChatService : IOpenAIChatService
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _opts;
    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web);

    public OpenAIChatService(HttpClient http, IOptions<OpenAIOptions> opts)
    {
        _http = http;
        _opts = opts.Value;

        _http.BaseAddress ??= new Uri(_opts.BaseUrl ?? "https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
        if (!string.IsNullOrWhiteSpace(_opts.OrganizationId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Organization", _opts.OrganizationId);
        if (!string.IsNullOrWhiteSpace(_opts.ProjectId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Project", _opts.ProjectId);

        // Required for Assistants v2 (threads/runs/vector_stores).
        _http.DefaultRequestHeaders.TryAddWithoutValidation("OpenAI-Beta", "assistants=v2");
    }

    public async Task<string> CreateThreadForVectorStoreAsync(string vectorStoreId, CancellationToken ct = default)
    {
        // Attach the vector store at the THREAD level so the same Assistant can work per-collection.
        var payload = new
        {
            tool_resources = new
            {
                file_search = new { vector_store_ids = new[] { vectorStoreId } }
            }
        };

        using var req = new StringContent(JsonSerializer.Serialize(payload, J), Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("threads", req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Thread create failed: {(int)res.StatusCode}. {body}");

        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetString()!;
    }

    public async Task DeleteThreadAsync(string threadId, CancellationToken ct = default)
    {
        using var res = await _http.DeleteAsync($"threads/{threadId}", ct);
        if ((int)res.StatusCode == 404) return; // already gone
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Thread delete failed: {(int)res.StatusCode}. {body}");
        }
    }

    public async Task<(string Plain, string Html)> AskAsync(string threadId, string assistantId, string userQuestion, CancellationToken ct = default)
    {
        // 1) Post user message
        var msgPayload = new { role = "user", content = userQuestion.Trim() };
        using (var msgReq = new StringContent(JsonSerializer.Serialize(msgPayload, J), Encoding.UTF8, "application/json"))
        {
            using var msgRes = await _http.PostAsync($"threads/{threadId}/messages", msgReq, ct);
            if (!msgRes.IsSuccessStatusCode)
                throw new InvalidOperationException($"Message post failed: {(int)msgRes.StatusCode}. {await msgRes.Content.ReadAsStringAsync(ct)}");
        }

        // 2) Start run
        var runPayload = new { assistant_id = assistantId };
        using var runReq = new StringContent(JsonSerializer.Serialize(runPayload, J), Encoding.UTF8, "application/json");
        using var runRes = await _http.PostAsync($"threads/{threadId}/runs", runReq, ct);
        var runBody = await runRes.Content.ReadAsStringAsync(ct);
        if (!runRes.IsSuccessStatusCode)
            throw new InvalidOperationException($"Run create failed: {(int)runRes.StatusCode}. {runBody}");

        var runId = JsonDocument.Parse(runBody).RootElement.GetProperty("id").GetString()!;

        // 3) Poll for completion (simple loop; swap for Polly/backoff later if needed)
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct);

            using var stRes = await _http.GetAsync($"threads/{threadId}/runs/{runId}", ct);
            var stBody = await stRes.Content.ReadAsStringAsync(ct);
            if (!stRes.IsSuccessStatusCode)
                throw new InvalidOperationException($"Run status failed: {(int)stRes.StatusCode}. {stBody}");

            var status = JsonDocument.Parse(stBody).RootElement.GetProperty("status").GetString();
            if (status == "completed") break;
            if (status is "failed" or "cancelled" or "expired")
                throw new InvalidOperationException($"Run {status}. {stBody}");
        }

        // 4) Return latest assistant message (plain + simple HTML)
        return await GetLatestAssistantAsync(threadId, ct);
    }

    public async Task<(string Plain, string Html)> GetLatestAssistantAsync(string threadId, CancellationToken ct = default)
    {
        using var res = await _http.GetAsync($"threads/{threadId}/messages?limit=10&order=desc", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Messages read failed: {(int)res.StatusCode}. {body}");

        var root = JsonDocument.Parse(body).RootElement;
        foreach (var m in root.GetProperty("data").EnumerateArray())
        {
            if (m.GetProperty("role").GetString() != "assistant") continue;
            return BuildFromMessageElement(m);
        }
        return ("", "");
    }

    public async Task<List<ChatMessage>> GetThreadHistoryAsync(string threadId, int takeLast = 100)
    {
        var results = new List<ChatMessage>();
        string? after = null;

        while (true)
        {
            var url = $"threads/{threadId}/messages?order=asc&limit=100" + (after is null ? "" : $"&after={after}");
            using var res = await _http.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Messages read failed: {(int)res.StatusCode}. {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var m in root.GetProperty("data").EnumerateArray())
            {
                var role = m.GetProperty("role").GetString() ?? "";
                var created = m.TryGetProperty("created_at", out var createdEl) ? createdEl.GetInt64() : 0L;

                // Build plain only (Core stays UI-agnostic)
                var (plain, _) = BuildFromMessageElement(m);

                results.Add(new ChatMessage
                {
                    Role = role,
                    Text = plain,
                    Timestamp = created > 0 ? DateTimeOffset.FromUnixTimeSeconds(created) : null
                });
            }

            var hasMore = root.TryGetProperty("has_more", out var hm) && hm.GetBoolean();
            if (!hasMore) break;
            after = root.TryGetProperty("last_id", out var lastIdEl) ? lastIdEl.GetString() : null;
            if (string.IsNullOrEmpty(after)) break;
        }

        if (results.Count > takeLast)
            results = results.Skip(results.Count - takeLast).ToList();

        return results;
    }

    public async Task<List<(string Role, string Html, DateTimeOffset? Timestamp)>> GetThreadHistoryHtmlAsync(
    string threadId, int takeLast = 100)
    {
        var results = new List<(string Role, string Html, DateTimeOffset? Timestamp)>();
        string? after = null;

        while (true)
        {
            var url = $"threads/{threadId}/messages?order=asc&limit=100" + (after is null ? "" : $"&after={after}");
            using var res = await _http.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Messages read failed: {(int)res.StatusCode}. {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            foreach (var m in root.GetProperty("data").EnumerateArray())
            {
                var role = m.GetProperty("role").GetString() ?? "";
                var created = m.TryGetProperty("created_at", out var createdEl) ? createdEl.GetInt64() : 0L;

                // Build HTML (markdown + annotations -> <a class="saf-cite" data-file-id="...">[n]</a>)
                var (_, html) = BuildFromMessageElement(m);

                results.Add((
                    Role: role,
                    Html: html,
                    Timestamp: created > 0 ? DateTimeOffset.FromUnixTimeSeconds(created) : null
                ));
            }

            if (!(root.TryGetProperty("has_more", out var hm) && hm.GetBoolean())) break;
            after = root.TryGetProperty("last_id", out var lastIdEl) ? lastIdEl.GetString() : null;
            if (string.IsNullOrEmpty(after)) break;
        }

        if (results.Count > takeLast)
            results = results.Skip(results.Count - takeLast).ToList();

        return results;
    }

    // OpenAIChatService.cs (add these helpers inside the class)
    private static string MarkdownToHtml(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // Minimal, safe markdown: **bold**, *italic*, `code`, [text](url)
        var enc = System.Net.WebUtility.HtmlEncode(s);

        // link: [text](url)  (do links before italics/bold so brackets don't break)
        enc = System.Text.RegularExpressions.Regex.Replace(
            enc, @"\[(.+?)\]\((https?://[^\s)]+)\)",
            m => $"<a href=\"{m.Groups[2].Value}\" target=\"_blank\" rel=\"noopener\">{m.Groups[1].Value}</a>");

        // code
        enc = System.Text.RegularExpressions.Regex.Replace(enc, "`([^`]+)`", "<code>$1</code>");

        // bold then italic (avoid nested conflicts by using tempered patterns)
        enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\*(.+?)\*", "<em>$1</em>");

        // line breaks
        enc = enc.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>");
        return enc;
    }

    // Build (plain, html) from a single "message" JSON element
    private static (string Plain, string Html) BuildFromMessageElement(JsonElement msg)
    {
        var plain = new StringBuilder();
        var html = new StringBuilder();

        foreach (var part in msg.GetProperty("content").EnumerateArray())
        {
            if (!part.TryGetProperty("type", out var t) || t.GetString() != "text") continue;
            if (!part.TryGetProperty("text", out var textObj)) continue;

            var value = textObj.GetProperty("value").GetString() ?? "";
            plain.Append(value);

            // ---- Markdown → HTML first (bold/italics/links/code + <br>) ----
            string RenderMarkdown(string s)
            {
                var enc = System.Net.WebUtility.HtmlEncode(s);
                enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\[(.+?)\]\((https?://[^\s)]+)\)", m =>
                    $"<a href=\"{m.Groups[2].Value}\" target=\"_blank\" rel=\"noopener\">{m.Groups[1].Value}</a>");
                enc = System.Text.RegularExpressions.Regex.Replace(enc, "`([^`]+)`", "<code>$1</code>");
                enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
                enc = System.Text.RegularExpressions.Regex.Replace(enc, @"\*(.+?)\*", "<em>$1</em>");
                enc = enc.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>");
                return enc;
            }

            // No annotations? Just render markdown.
            if (!textObj.TryGetProperty("annotations", out var anns) || anns.ValueKind != JsonValueKind.Array || anns.GetArrayLength() == 0)
            {
                html.Append(RenderMarkdown(value));
                continue;
            }

            // ---- Place inline markers using start/end indices ----
            // Build an edit list then stitch together.
            var inserts = new List<(int pos, string html)>();
            var seen = new Dictionary<string, int>(); // file_id -> index
            int counter = 0;

            foreach (var ann in anns.EnumerateArray())
            {
                if (!ann.TryGetProperty("type", out var at) || at.GetString() != "file_citation") continue;

                var fc = ann.GetProperty("file_citation");
                var fileId = fc.GetProperty("file_id").GetString() ?? "";
                var endIdx = ann.TryGetProperty("end_index", out var ei) ? ei.GetInt32() : (int?)null;

                if (!seen.TryGetValue(fileId, out var idx))
                    seen[fileId] = idx = ++counter;

                if (endIdx.HasValue && endIdx.Value >= 0 && endIdx.Value <= value.Length)
                {
                    var marker = $"<sup><a href=\"#\" class=\"saf-cite\" data-file-id=\"{System.Net.WebUtility.HtmlEncode(fileId)}\" data-cite-idx=\"{idx}\">[{idx}]</a></sup>";
                    inserts.Add((endIdx.Value, marker));
                }
            }

            inserts.Sort((a, b) => a.pos.CompareTo(b.pos));

            var sb = new StringBuilder();
            int last = 0;
            foreach (var (pos, marker) in inserts)
            {
                if (pos > last)
                {
                    sb.Append(RenderMarkdown(value.Substring(last, pos - last)));
                }
                sb.Append(marker);
                last = pos;
            }
            sb.Append(RenderMarkdown(value.Substring(last)));

            html.Append(sb.ToString());
        }

        return (plain.ToString(), html.ToString());
    }
}