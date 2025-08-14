// ChatModel.cs (clean + single-source-of-truth file-citation mapping)
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChatModel : PageModel
{
    // ---- Injected services --------------------------------------------------
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    // ---- OpenAI -------------------------------------------------------------
    private readonly string _baseOpenAiUrl = "https://api.openai.com/v1/";
    private HttpClient _httpClient; // named client with headers from DI

    // ---- Bound + view model props ------------------------------------------
    public Guid? Id { get; set; }
    public Collection Collection { get; set; }

    //[Required(AllowEmptyStrings = false, ErrorMessage = "Question is required.")]
    public string question { get; set; }

    // Only used by server-rendered history
    public string Answer;

    // ---- Busy lock (server-side) -------------------------------------------
    private const string BusyKey = "ChatBusy";
    private bool IsChatBusy() =>
        string.Equals(HttpContext.Session.GetString(BusyKey), "1", StringComparison.Ordinal);
    private void SetChatBusy(bool busy)
    {
        if (busy) HttpContext.Session.SetString(BusyKey, "1");
        else HttpContext.Session.Remove(BusyKey);
    }

    // ---- DI -----------------------------------------------------------------
    public ChatModel(
        TelemetryClient telemetryClient,
        AuthenticatedApiClient api,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    // ---- DTO for async JSON endpoint ---------------------------------------
    public record AskAjaxDto(string Question);

    // ========================================================================
    //  GET: Page load (loads history if thread exists)                        |
    // ========================================================================
    public async Task<IActionResult> OnGetAsync([FromQuery] Guid? id)
    {
        try
        {
            if (id == null)
                return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");

            var sessionCollection = HttpContext.Session.GetObject<Collection>("Collection");
            if (sessionCollection != null && id != sessionCollection.CollectionId)
                await DeleteThread(); // switching collections → nuke previous thread

            HttpContext.Session.SetString("PageTitle", "Chat");
            HttpContext.Session.SetObject("id", id);

            await LoadData();

            if (HttpContext.Session.GetString("strThreadID") != null)
                await LoadChat(); // fills Answer with HTML (links included)

            ModelState.Remove("Question");
        }
        catch (Exception ex)
        {
            LogAndNotifyException(ex);
            return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");
        }

        return Page();
    }

    // ========================================================================
    //  POST: Async JSON endpoint (?handler=AskAjax)                           |
    //  - Sends user message, starts run, waits, returns latest assistant msg  |
    //  - Returns BOTH plain + html (for typing then swap)                     |
    // ========================================================================
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAskAjaxAsync([FromBody] AskAjaxDto dto)
    {
        if (IsChatBusy())
            return StatusCode(409, "Chat is busy. Please wait for the current response to finish.");

        try
        {
            SetChatBusy(true);

            Id = HttpContext.Session.GetObject<Guid>("id");
            if (Id == null) return BadRequest("Missing collection id.");
            if (string.IsNullOrWhiteSpace(dto?.Question)) return BadRequest("Question is required.");

            await LoadData();
            _httpClient ??= _httpClientFactory.CreateClient("SearchAFileClient");

            var threadId = await EnsureThreadExists();

            // Attach up to 20 known files for file_search
            var fileIds = Collection.Files?
                .Where(f => !string.IsNullOrEmpty(f.OpenAIFileId))
                .Select(f => f.OpenAIFileId!)
                .Take(20)
                .ToList();

            var messagePayload = new
            {
                role = "user",
                content = dto.Question.Trim(),
                attachments = fileIds?.Select(id => new { file_id = id, tools = new[] { new { type = "file_search" } } })
            };

            using (var messageContent = Conversions.CreateStringContentObject(messagePayload))
            using (var msgResp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads/{threadId}/messages", messageContent))
            {
                if (!msgResp.IsSuccessStatusCode)
                    return StatusCode((int)msgResp.StatusCode, await msgResp.Content.ReadAsStringAsync());
            }

            var runPayload = new { assistant_id = _configuration["OpenAI:AssistantId"] };
            using (var runContent = Conversions.CreateStringContentObject(runPayload))
            using (var runResp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads/{threadId}/runs", runContent))
            {
                if (!runResp.IsSuccessStatusCode)
                    return StatusCode((int)runResp.StatusCode, await runResp.Content.ReadAsStringAsync());

                using var runDoc = await Conversions.CreateJsonDocumentObject(runResp);
                var runId = runDoc.RootElement.GetProperty("id").GetString();
                await PollUntilRunCompletes(threadId, runId);
            }

            // Build latest assistant message in both forms
            var (plain, html) = await GetLatestAssistantAsync(threadId);

            return new JsonResult(new { ok = true, assistantPlain = plain, assistantHtml = html });
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            return StatusCode(500, ex.Message);
        }
        finally
        {
            SetChatBusy(false);
        }
    }

    // ========================================================================
    //  POST: Start new chat                                                   |
    // ========================================================================
    public async Task OnPostStartNewChatAsync(Guid? id)
    {
        try
        {
            if (id == null)
                throw new Exception("New Chat: No ID provided.");

            await LoadData();
            await DeleteThread();

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Chat successfully cleared.', 7000, true)";
            ModelState.Remove("Question");
        }
        catch (Exception ex)
        {
            LogAndNotifyException(ex);
        }
    }

    // ========================================================================
    //  Helpers: OpenAI thread/run                                             |
    // ========================================================================
    private async Task<string> EnsureThreadExists()
    {
        var threadId = HttpContext.Session.GetString("strThreadID");
        if (string.IsNullOrEmpty(threadId))
        {
            using var resp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads", content: null);
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Thread creation failed: " + await resp.Content.ReadAsStringAsync());

            using var doc = await Conversions.CreateJsonDocumentObject(resp);
            threadId = doc.RootElement.GetProperty("id").GetString();
            HttpContext.Session.SetString("strThreadID", threadId);
        }
        return threadId;
    }

    private async Task PollUntilRunCompletes(string threadId, string runId)
    {
        for (int i = 0; i < 300; i++) // ~5 minutes
        {
            await Task.Delay(1000);

            using var runStatusResp = await _httpClient.GetAsync($"{_baseOpenAiUrl}threads/{threadId}/runs/{runId}");
            if (!runStatusResp.IsSuccessStatusCode)
            {
                await DeleteThread();
                throw new Exception("Run status check failed: " + await runStatusResp.Content.ReadAsStringAsync());
            }

            using var runStatusDoc = await JsonDocument.ParseAsync(await runStatusResp.Content.ReadAsStreamAsync());
            if (!runStatusDoc.RootElement.TryGetProperty("status", out var statusProp))
            {
                await DeleteThread();
                throw new Exception("Run status missing in response.");
            }

            var status = statusProp.GetString();
            if (status == "completed") return;
            if (status is "failed" or "cancelled" or "expired")
                throw new Exception("OpenAI run ended: " + status);
        }

        await DeleteThread();
        throw new Exception("OpenAI run timed out.");
    }

    private async Task DeleteThread()
    {
        try
        {
            var threadId = HttpContext.Session.GetString("strThreadID");
            if (!string.IsNullOrEmpty(threadId))
            {
                var client = _httpClientFactory.CreateClient("SearchAFileClient");
                await client.DeleteAsync($"{_baseOpenAiUrl}threads/{threadId}");
                HttpContext.Session.Remove("strThreadID");
            }
        }
        catch { throw; }
    }

    // ========================================================================
    //  Assistant message builders (single source of truth)                    |
    // ========================================================================
    private Dictionary<string, (string url, string display)> BuildOpenAiFileLinkMap()
    {
        // OpenAI file_id  ->  (/Files/{FileId}{Extension}, display text)
        var map = new Dictionary<string, (string url, string display)>(StringComparer.Ordinal);
        if (Collection?.Files == null) return map;

        foreach (var f in Collection.Files)
        {
            if (string.IsNullOrWhiteSpace(f.OpenAIFileId)) continue;

            var ext = (f.Extension ?? "").Trim();
            if (!string.IsNullOrEmpty(ext) && ext[0] != '.') ext = "." + ext;

            var physicalName = $"{f.FileId}{ext}";
            var url = $"/Files/{physicalName}";
            var display =
                !string.IsNullOrWhiteSpace(f.File1) ? f.File1 :
                //!string.IsNullOrWhiteSpace(f.OriginalFileName) ? f.OriginalFileName :
                !string.IsNullOrWhiteSpace(f.File1) ? f.File1 :
                physicalName;

            map[f.OpenAIFileId] = (url, display);
        }

        return map;
    }

    private static string HtmlFromTextPart(
    string rawText,
    JsonElement? annotationsOpt,
    Dictionary<string, (string url, string display)> fileLinkMap)
    {
        // Helper: encode text, render **bold**, [label](url), and raw URLs.
        static string EncodeWithSimpleMarkdownAndLinks(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Build output incrementally by scanning for three token types:
            // 1) Markdown links: [label](https://url)
            // 2) Raw URLs (http/https/www.)
            // 3) Bold spans: **text**
            //
            // Priority is important: links are handled before bold so we don't
            // create <strong> inside <a> text accidentally.
            //
            // We process left-to-right with a single alternation regex.
            var pattern = string.Join("|", new[]
            {
            // [label](url) — url stops before whitespace or ')'
            @"\[(?<mdlabel>[^\]]+)\]\((?<mdurl>https?:\/\/[^\s)]+)\)",
            // raw URL (captures trailing punctuation separately so it stays outside the <a>)
            @"(?<raw>(?:https?:\/\/|www\.)[^\s<)]+?)(?<trail>[.,;:!?)]+)?",
            // **bold**
            @"\*\*(?<bold>.+?)\*\*"
        });

            var rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline);
            var sb = new StringBuilder();
            int last = 0;

            foreach (Match m in rx.Matches(text))
            {
                if (m.Index > last)
                    sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(last, m.Index - last)));

                if (m.Groups["mdlabel"].Success && m.Groups["mdurl"].Success)
                {
                    var label = System.Net.WebUtility.HtmlEncode(m.Groups["mdlabel"].Value);
                    var href = m.Groups["mdurl"].Value;
                    var safeHref = System.Net.WebUtility.HtmlEncode(href);
                    sb.Append($"<a href='{safeHref}' target='_blank' rel='noopener noreferrer' class='citation-link'>{label}</a>");
                }
                else if (m.Groups["raw"].Success)
                {
                    var raw = m.Groups["raw"].Value;
                    var display = System.Net.WebUtility.HtmlEncode(raw);
                    // Normalize www. -> https://www.
                    var href = raw.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? $"https://{raw}" : raw;
                    var safeHref = System.Net.WebUtility.HtmlEncode(href);
                    sb.Append($"<a href='{safeHref}' target='_blank' rel='noopener noreferrer' class='citation-link'>{display}</a>");

                    // Put any trailing punctuation *outside* the link
                    if (m.Groups["trail"].Success)
                        sb.Append(System.Net.WebUtility.HtmlEncode(m.Groups["trail"].Value));
                }
                else if (m.Groups["bold"].Success)
                {
                    var inner = System.Net.WebUtility.HtmlEncode(m.Groups["bold"].Value);
                    sb.Append("<strong>").Append(inner).Append("</strong>");
                }

                last = m.Index + m.Length;
            }

            if (last < text.Length)
                sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(last)));

            return sb.ToString();
        }

        // If there are no annotations, just encode with bold + links
        if (annotationsOpt == null || annotationsOpt.Value.ValueKind != JsonValueKind.Array || annotationsOpt.Value.GetArrayLength() == 0)
            return EncodeWithSimpleMarkdownAndLinks(rawText);

        var annotations = annotationsOpt.Value;
        var spans = new List<(int s, int e, string html)>();

        foreach (var ann in annotations.EnumerateArray())
        {
            if (!ann.TryGetProperty("start_index", out var sEl) ||
                !ann.TryGetProperty("end_index", out var eEl))
                continue;

            var s = sEl.GetInt32();
            var e = Math.Min(eEl.GetInt32(), rawText.Length);
            if (s < 0 || e <= s || s >= rawText.Length) continue;

            string? openAiFileId = null;
            if (ann.TryGetProperty("file_citation", out var fc) && fc.TryGetProperty("file_id", out var fcid))
                openAiFileId = fcid.GetString();
            else if (ann.TryGetProperty("file_path", out var fp) && fp.TryGetProperty("file_id", out var fpid))
                openAiFileId = fpid.GetString();

            var snippet = rawText.Substring(s, e - s);

            string html;
            if (!string.IsNullOrWhiteSpace(openAiFileId) && fileLinkMap.TryGetValue(openAiFileId!, out var link))
            {
                var anchorText = System.Net.WebUtility.HtmlEncode(link.display);
                var safeUrl = System.Net.WebUtility.HtmlEncode(link.url);
                html = $" <strong>source:</strong> <a href='{safeUrl}' target='_blank' rel='noopener noreferrer' class='citation-link' title='{anchorText}'>{anchorText}</a>";
            }
            else
            {
                // For annotated-but-unknown spans, still apply bold + links inside them
                html = EncodeWithSimpleMarkdownAndLinks(snippet);
            }

            spans.Add((s, e, html));
        }

        if (spans.Count == 0)
            return EncodeWithSimpleMarkdownAndLinks(rawText);

        spans.Sort((a, b) => a.s.CompareTo(b.s));

        var outSb = new StringBuilder();
        var cur = 0;
        foreach (var (s, e, html) in spans)
        {
            if (cur < s)
                outSb.Append(EncodeWithSimpleMarkdownAndLinks(rawText.Substring(cur, s - cur)));

            outSb.Append(html);
            cur = e;
        }
        if (cur < rawText.Length)
            outSb.Append(EncodeWithSimpleMarkdownAndLinks(rawText.Substring(cur)));

        return outSb.ToString();
    }

    private static (string Plain, string Html) BuildAssistantMessage(JsonElement assistantMessage, Dictionary<string, (string url, string display)> fileLinkMap)
    {
        var plainSb = new System.Text.StringBuilder();
        var htmlSb = new System.Text.StringBuilder();

        foreach (var part in assistantMessage.GetProperty("content").EnumerateArray())
        {
            if (!part.TryGetProperty("text", out var t)) continue;

            var raw = t.GetProperty("value").GetString() ?? string.Empty;
            plainSb.Append(raw);

            JsonElement? anns = null;
            if (t.TryGetProperty("annotations", out var a)) anns = a;

            var htmlPart = HtmlFromTextPart(raw, anns, fileLinkMap);
            htmlSb.Append(htmlPart);
        }

        // Convert newlines only in HTML flavor (plain keeps \n for typing)
        var html = Regex.Replace(htmlSb.ToString(), "\r\n|\n|\r", "<br>");
        return (plainSb.ToString(), html);
    }

    private async Task<(string Plain, string Html)> GetLatestAssistantAsync(string threadId)
    {
        using var resp = await _httpClient.GetAsync($"{_baseOpenAiUrl}threads/{threadId}/messages?limit=10");
        resp.EnsureSuccessStatusCode();

        using var doc = await Conversions.CreateJsonDocumentObject(resp);
        var map = BuildOpenAiFileLinkMap();

        foreach (var msg in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            if (!string.Equals(msg.GetProperty("role").GetString(), "assistant", StringComparison.Ordinal))
                continue;

            return BuildAssistantMessage(msg, map);
        }

        return ("", "");
    }

    // ========================================================================
    //  History loader (server renders bubbles into Answer)                    |
    // ========================================================================
    private async Task LoadChat()
    {
        _httpClient ??= _httpClientFactory.CreateClient("SearchAFileClient");
        var threadId = HttpContext.Session.GetString("strThreadID");
        if (string.IsNullOrEmpty(threadId)) return;

        using var resp = await _httpClient.GetAsync($"{_baseOpenAiUrl}threads/{threadId}/messages");
        if (!resp.IsSuccessStatusCode)
            throw new Exception("Chat retrieval failed: " + await resp.Content.ReadAsStringAsync());

        using var doc = await Conversions.CreateJsonDocumentObject(resp);

        var map = BuildOpenAiFileLinkMap();

        foreach (var msg in doc.RootElement.GetProperty("data").EnumerateArray().Reverse())
        {
            var role = msg.GetProperty("role").GetString();
            if (role != "user" && role != "assistant") continue;

            var timestamp = DateTimeOffset
                .FromUnixTimeSeconds(msg.GetProperty("created_at").GetInt64())
                .ToLocalTime().ToString("h:mm tt");

            string html;
            if (role == "assistant")
            {
                // Use the same converter as live path
                (_, html) = BuildAssistantMessage(msg, map);
            }
            else
            {
                // Encode user content (no annotations) and add <br>
                var userSb = new System.Text.StringBuilder();
                foreach (var part in msg.GetProperty("content").EnumerateArray())
                {
                    if (!part.TryGetProperty("text", out var t)) continue;
                    var raw = t.GetProperty("value").GetString() ?? string.Empty;
                    userSb.Append(System.Net.WebUtility.HtmlEncode(raw));
                }
                html = Regex.Replace(userSb.ToString(), "\r\n|\n|\r", "<br>");
            }

            var cssClass = role == "user" ? "user" : "bot";
            Answer += $"<div class='chat-message {cssClass}'>" +
                      $"  <div class='message-text'>{html}</div>" +
                      $"  <div class='timestamp'>{timestamp}</div>" +
                      $"</div>";
        }
    }

    // ========================================================================
    //  Data + error utils                                                     |
    // ========================================================================
    private async Task LoadData()
    {
        var sessionCollection = HttpContext.Session.GetObject<Collection>("Collection");
        if (sessionCollection == null)
        {
            var collectionResult = await _api.GetAsync<Collection>($"collections/{Id}");
            if (!collectionResult.IsSuccess || collectionResult.Data == null)
                throw new Exception(collectionResult.ErrorMessage ?? "Failed to load collection.");

            var filesResult = await _api.GetAsync<List<File>>("files");
            if (!filesResult.IsSuccess || filesResult.Data == null)
                throw new Exception(filesResult.ErrorMessage ?? "Failed to load files.");

            var files = filesResult.Data.Where(f => f.CollectionId == collectionResult.Data.CollectionId).ToList();
            CollectionFileCountMapper.MapFilesToCollection(collectionResult.Data, files);
            Collection = collectionResult.Data;
            HttpContext.Session.SetObject("Collection", Collection);
        }
        else
        {
            Collection = sessionCollection;
        }
    }

    private void LogAndNotifyException(Exception ex)
    {
        _telemetryClient.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error });
        TempData["StartupJavaScript"] = BuildErrorMessage(ex);
    }

    private string BuildErrorMessage(Exception ex)
    {
        var contact = HttpContext.Session.GetString("ContactInfo") ?? "support";
        var msg = ex.InnerException == null ? ex.Message : ex.Message + " (Inner: " + ex.InnerException.Message + ")";
        return $"window.top.ShowToast('danger', 'Error', 'An error occured. Please report to {contact}: {msg.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString()}', 0, false);";
    }
}