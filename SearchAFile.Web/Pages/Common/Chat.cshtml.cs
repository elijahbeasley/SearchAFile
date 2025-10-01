using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Core.Interfaces;
using SearchAFile.Web.Services;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChatModel : PageModel
{
    private readonly TelemetryClient _telemetry;
    private readonly AuthenticatedApiClient _api;
    private readonly IOpenAIChatService _chat;

    public Guid? Id { get; set; }
    public Collection Collection { get; set; } = default!;
    [BindProperty] public string? Question { get; set; }
    public string AnswerHtml { get; set; } = "";

    public sealed class ChatBubble
    {
        public string Role { get; set; } = "";     // "user" | "assistant" | "system"
        public string Html { get; set; } = "";     // rendered HTML
        public DateTimeOffset? Timestamp { get; set; }
    }

    public List<ChatBubble> History { get; set; } = new();

    public ChatModel(TelemetryClient telemetry, AuthenticatedApiClient api, IOpenAIChatService chat)
    {
        _telemetry = telemetry;
        _api = api;
        _chat = chat;
    }

    // --------------------- GET ---------------------
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null) return RedirectToPage("/Index");
        Id = id;

        var colRes = await _api.GetAsync<Collection>($"collections/{Id}");
        if (!colRes.IsSuccess || colRes.Data == null) return NotFound();
        Collection = colRes.Data;

        // Cache this collection's files into Session so we can resolve links locally
        HttpContext.Session.Remove("CollectionFiles");
        var filesRes = await _api.GetAsync<List<File>>("files");
        if (!filesRes.IsSuccess || filesRes.Data == null) return NotFound();

        var files = filesRes.Data
            .Where(f => f.CollectionId == Id)
            .ToList();

        HttpContext.Session.SetObject("CollectionFiles", files);

        // Pull the whole conversation (assistant messages already contain HTML)
        if (!string.IsNullOrWhiteSpace(Collection.OpenAiThreadId))
        {
            try
            {
                var msgs = await _chat.GetThreadHistoryHtmlAsync(Collection.OpenAiThreadId!, takeLast: 200);
                History = new();
                foreach (var m in msgs)
                {
                    var html = await ResolveSafCiteAnchorsFromSessionAsync(m.Html); // <a class="saf-cite" ...>
                    html = await ResolveDocTokensFromSessionAsync(html);           // [[doc:"guid.ext"[ , page:N]]]
                    History.Add(new ChatBubble { Role = m.Role, Html = html, Timestamp = m.Timestamp });
                }
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Warning });
                History = new List<ChatBubble>();
            }
        }

        return Page();
    }

    // --------------------- AJAX: POST /?handler=AskAjax ---------------------
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> OnPostAskAjax([FromBody] AskAjaxRequest req)
    {
        try
        {
            if (Id is null) return BadRequest(new { ok = false, message = "Missing collection id." });
            if (string.IsNullOrWhiteSpace(req?.Question))
                return BadRequest(new { ok = false, message = "Question is required." });

            var colRes = await _api.GetAsync<Collection>($"collections/{Id}");
            if (!colRes.IsSuccess || colRes.Data is null)
                return NotFound(new { ok = false, message = "Collection not found." });

            var collection = colRes.Data;
            var threadId = await EnsureThreadForCollectionAsync(collection);

            var assistantId = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["OpenAI:AssistantId"];
            if (string.IsNullOrWhiteSpace(assistantId))
                return StatusCode(500, new { ok = false, message = "AssistantId missing in configuration." });

            var (plain, html) = await _chat.AskAsync(threadId, assistantId!, req.Question!);

            // Convert sources to local links **using session only**
            html = await ResolveSafCiteAnchorsFromSessionAsync(html);
            html = await ResolveDocTokensFromSessionAsync(html);

            return new JsonResult(new { ok = true, assistantPlain = plain, assistantHtml = html });
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error });
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    // --------------------- New Chat ---------------------
    public async Task<IActionResult> OnPostStartNewChatAsync(Guid? id)
    {
        if (id is null) return BadRequest("Missing collection id.");

        var colRes = await _api.GetAsync<Collection>($"collections/{id}");
        if (!colRes.IsSuccess || colRes.Data is null) return NotFound();

        var collection = colRes.Data;

        if (!string.IsNullOrWhiteSpace(collection.OpenAiThreadId))
        {
            try { await _chat.DeleteThreadAsync(collection.OpenAiThreadId!); } catch { /* ignore */ }
            collection.OpenAiThreadId = null;

            var save = await _api.PutAsync<object>($"collections/{collection.CollectionId}", collection);
            if (!save.IsSuccess) throw new Exception("Failed to clear thread id in DB.");
        }

        TempData["StartupJavaScript"] = "ShowSnack('success','Started new chat.',5000,true);";
        return RedirectToPage(new { id });
    }

    // --------------------- Helpers ---------------------
    private async Task<string> EnsureThreadForCollectionAsync(Collection collection, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(collection.OpenAiThreadId))
            return collection.OpenAiThreadId!;

        if (string.IsNullOrWhiteSpace(collection.OpenAiVectorStoreId))
            throw new InvalidOperationException("Collection has no OpenAI vector store.");

        var threadId = await _chat.CreateThreadForVectorStoreAsync(collection.OpenAiVectorStoreId!, ct);
        collection.OpenAiThreadId = threadId;

        var save = await _api.PutAsync<object>($"collections/{collection.CollectionId}", collection);
        if (!save.IsSuccess) throw new Exception("Failed to save thread id to the collection.");

        return threadId;
    }

    // === SESSION-ONLY SOURCE RESOLUTION =======================================
    // Build URL + LABEL dictionaries from files cached in Session: /Files/{FileId}.{Extension}
    private (Dictionary<string, (string Url, string Label)> byOpenAiId,
             Dictionary<string, (string Url, string Label)> byStorageName,
             Dictionary<string, (string Url, string Label)> byDisplayName)
    BuildFileUrlMaps()
    {
        var files = HttpContext.Session.GetObject<List<File>>("CollectionFiles") ?? new List<File>();

        static (string Url, string Label) Info(File f)
        {
            var extDot = string.IsNullOrWhiteSpace(f.Extension) ? "" : "." + f.Extension.TrimStart('.');
            var url = $"/Files/{f.FileId}{extDot}";

            // Prefer the original filename; append extension if it's not already there.
            var baseName = string.IsNullOrWhiteSpace(f.File1) ? f.FileId.ToString() : f.File1!;
            var label = baseName.EndsWith(extDot, StringComparison.OrdinalIgnoreCase) ? baseName : baseName + extDot;

            return (url, label);
        }

        // 1) OpenAI file id -> (Url, Label)
        var byOpenAiId = files
            .Where(f => !string.IsNullOrWhiteSpace(f.OpenAIFileId))
            .ToDictionary(f => f.OpenAIFileId!, f => Info(f), StringComparer.OrdinalIgnoreCase);

        // 2) Stored name "GUID.ext" -> (Url, Label)
        var byStorageName = files
            .ToDictionary(f => $"{f.FileId}{(string.IsNullOrWhiteSpace(f.Extension) ? "" : "." + f.Extension.TrimStart('.'))}",
                          f => Info(f),
                          StringComparer.OrdinalIgnoreCase);

        // 3) Display/original name -> (Url, Label)
        var byDisplayName = files
            .Where(f => !string.IsNullOrWhiteSpace(f.File1))
            .ToDictionary(f => f.File1!, f => Info(f), StringComparer.OrdinalIgnoreCase);

        return (byOpenAiId, byStorageName, byDisplayName);
    }

    // Convert <a class="saf-cite" data-file-id="...">[n]</a> into real links
    private Task<string> ResolveSafCiteAnchorsFromSessionAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return Task.FromResult(html);

        var (byOpenAiId, _, _) = BuildFileUrlMaps();

        var rx = new System.Text.RegularExpressions.Regex(
            "<a\\s+[^>]*class=\"saf-cite\"[^>]*data-file-id=\"([^\"]+)\"[^>]*>(.*?)</a>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Compiled);

        string ReplaceAnchor(System.Text.RegularExpressions.Match m)
        {
            var fileId = m.Groups[1].Value;
            var inner = m.Groups[2].Value;

            if (!byOpenAiId.TryGetValue(fileId, out var info) || string.IsNullOrWhiteSpace(info.Url))
                return m.Value;

            var safeUrl = System.Net.WebUtility.HtmlEncode(info.Url);
            var label = System.Net.WebUtility.HtmlEncode(info.Label ?? inner);
            return $"<a class=\"saf-cite\" href=\"{safeUrl}\" target=\"_blank\" rel=\"noopener\">{label}</a>";
        }

        var result = rx.Replace(html, new System.Text.RegularExpressions.MatchEvaluator(ReplaceAnchor));
        return Task.FromResult(result);
    }

    // Convert [[doc:"GUID.ext"[ , page:N]]] (or with &quot;) to <a href=...>OriginalName.ext</a>
    private Task<string> ResolveDocTokensFromSessionAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return Task.FromResult(html);

        var (byOpenAiId, byStorageName, byDisplayName) = BuildFileUrlMaps();

        // Supports both literal quotes "..." and HTML-encoded quotes &quot;...&quot;
        var rx = new System.Text.RegularExpressions.Regex(
            @"\[\[\s*doc\s*:\s*(?:(?:""(?<t1>[^""]+)"")|(?:&quot;(?<t2>[^&]+)&quot;))" +
            @"(?:\s*,\s*page\s*:\s*(?<p>\d+))?\s*\]\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        string ReplaceTag(System.Text.RegularExpressions.Match m)
        {
            var token = m.Groups["t1"].Success ? m.Groups["t1"].Value.Trim()
                                               : m.Groups["t2"].Value.Trim();
            var page = m.Groups["p"].Success ? m.Groups["p"].Value : null;

            (string Url, string Label)? info = null;

            if (token.StartsWith("file_", StringComparison.OrdinalIgnoreCase))
            {
                if (byOpenAiId.TryGetValue(token, out var i)) info = i;
            }
            else if (token.Contains('.'))
            {
                if (byStorageName.TryGetValue(token, out var i)) info = i;
            }
            else
            {
                if (byDisplayName.TryGetValue(token, out var i)) info = i;
            }

            if (info is null) return m.Value;

            var finalUrl = info.Value.Url;
            if (!string.IsNullOrWhiteSpace(page) &&
                finalUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                !finalUrl.Contains('#'))
            {
                finalUrl += $"#page={page}";
            }

            var safeUrl = System.Net.WebUtility.HtmlEncode(finalUrl);
            var safeLabel = System.Net.WebUtility.HtmlEncode(info.Value.Label);
            return $"<a href=\"{safeUrl}\" target=\"_blank\" rel=\"noopener\">{safeLabel}</a>";
        }

        var output = rx.Replace(html, new System.Text.RegularExpressions.MatchEvaluator(ReplaceTag));
        return Task.FromResult(output);
    }

    public sealed class AskAjaxRequest { public string? Question { get; set; } }
}