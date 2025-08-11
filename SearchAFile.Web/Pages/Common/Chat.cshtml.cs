// Cleaned and Commented ChatModel.cs
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Infrastructure.Mapping;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChatModel : PageModel
{
    // Injected dependencies
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    // OpenAI base URL
    private readonly string _baseOpenAiUrl = "https://api.openai.com/v1/";

    // Bound properties
    public Guid? Id { get; set; }
    public Collection Collection { get; set; }

    // UI messages
    public string MessageColor;
    public string Message;
    public string Answer;

    // HTTP client instance
    private HttpClient _httpClient;

    public ChatModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    // Handle GET page load
    public async Task<IActionResult> OnGetAsync([FromQuery] Guid? id)
    {
        try
        {
            if (id == null)
                return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");

            var sessionCollection = HttpContext.Session.GetObject<Collection>("Collection");
            if (sessionCollection != null && id != sessionCollection.CollectionId)
                await DeleteThread();

            HttpContext.Session.SetString("PageTitle", "Chat");
            HttpContext.Session.SetObject("id", id);

            await LoadData();

            if (HttpContext.Session.GetString("strThreadID") != null)
                await LoadChat();

            ModelState.Remove("Question");
        }
        catch (Exception ex)
        {
            LogAndNotifyException(ex);
            return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");
        }

        return Page();
    }

    // Ask a question via the assistant
    public async Task<IActionResult> OnGetAsk(string question)
    {
        try
        {
            Id = HttpContext.Session.GetObject<Guid>("id");
            if (Id == null || string.IsNullOrWhiteSpace(question))
                throw new Exception("Missing ID or question.");

            question = WebUtility.UrlDecode(question);
            await LoadData();

            _httpClient ??= _httpClientFactory.CreateClient("SearchAFIleClient");

            var threadId = await EnsureThreadExists();
            var fileIds = Collection.Files?.Where(f => !string.IsNullOrEmpty(f.OpenAIFileId)).Select(f => f.OpenAIFileId!).Take(20).ToList();

            var messagePayload = new
            {
                role = "user",
                content = question.Trim(),
                attachments = fileIds?.Select(id => new { file_id = id, tools = new[] { new { type = "file_search" } } })
            };

            using var messageContent = Conversions.CreateStringContentObject(messagePayload);
            using var messageResp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads/{threadId}/messages", messageContent);
            if (!messageResp.IsSuccessStatusCode)
                throw new Exception("Failed to post message: " + await messageResp.Content.ReadAsStringAsync());

            var runPayload = new { assistant_id = _configuration["OpenAI:AssistantId"] };
            using var runContent = Conversions.CreateStringContentObject(runPayload);
            using var runResp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads/{threadId}/runs", runContent);
            if (!runResp.IsSuccessStatusCode)
                throw new Exception("Failed to start run: " + await runResp.Content.ReadAsStringAsync());

            using var runDoc = await Conversions.CreateJsonDocumentObject(runResp);
            string runId = runDoc.RootElement.GetProperty("id").GetString();

            await PollUntilRunCompletes(threadId, runId);
            await LoadChat();
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error });
            return StatusCode(500, BuildErrorMessage(ex));
        }

        return Page();
    }

    // Resets the conversation
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

    // Poll OpenAI until run completes or fails
    private async Task PollUntilRunCompletes(string threadId, string runId)
    {
        for (int i = 0; i < 300; i++) // up to 5 mins
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

    // Create a thread if one doesn't exist
    private async Task<string> EnsureThreadExists()
    {
        var threadId = HttpContext.Session.GetString("strThreadID");
        if (string.IsNullOrEmpty(threadId))
        {
            using var resp = await _httpClient.PostAsync($"{_baseOpenAiUrl}threads", null);
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Thread creation failed: " + await resp.Content.ReadAsStringAsync());

            using var doc = await Conversions.CreateJsonDocumentObject(resp);
            threadId = doc.RootElement.GetProperty("id").GetString();
            HttpContext.Session.SetString("strThreadID", threadId);
        }
        return threadId;
    }

    // Delete the OpenAI thread
    private async Task DeleteThread()
    {
        try
        {
            var threadId = HttpContext.Session.GetString("strThreadID");
            if (!string.IsNullOrEmpty(threadId))
            {
                var client = _httpClientFactory.CreateClient("SearchAFIleClient");
                await client.DeleteAsync($"{_baseOpenAiUrl}threads/{threadId}");
                HttpContext.Session.Remove("strThreadID");
            }
        }
        catch { throw; }
    }

    // Load collection and files
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

    // Load and format assistant response
    private async Task LoadChat()
    {
        _httpClient ??= _httpClientFactory.CreateClient("SearchAFIleClient");
        var threadId = HttpContext.Session.GetString("strThreadID");

        using var resp = await _httpClient.GetAsync($"{_baseOpenAiUrl}threads/{threadId}/messages");
        if (!resp.IsSuccessStatusCode)
            throw new Exception("Chat retrieval failed: " + await resp.Content.ReadAsStringAsync());

        using var doc = await Conversions.CreateJsonDocumentObject(resp);
        foreach (var msg in doc.RootElement.GetProperty("data").EnumerateArray().Reverse())
        {
            var role = msg.GetProperty("role").GetString();
            if (role != "user" && role != "assistant") continue;

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.GetProperty("created_at").GetInt64()).ToLocalTime().ToString("h:mm tt");

            foreach (var part in msg.GetProperty("content").EnumerateArray())
            {
                var text = part.GetProperty("text").GetProperty("value").GetString();
                text = Regex.Replace(text, @"(?<fileId>[0-9a-fA-F\-]{36}\.(pdf|docx|xlsx))", m =>
                {
                    var fileId = Path.GetFileNameWithoutExtension(m.Groups["fileId"].Value);
                    var file = Collection.Files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.Path.ToString()) == fileId);
                    return file != null ? $"<a href='/Files/{file.Path}' target='_blank'>{file.File1}</a>" : m.Groups["fileId"].Value;
                });
                text = Regex.Replace(text, "\r\n|\n|\r", "<br>");

                var cssClass = role == "user" ? "user" : "bot";
                Answer += $"<div class='chat-message {cssClass}'><div class='message-text'>{text}</div><div class='timestamp'>{timestamp}</div></div>";
            }
        }
    }

    // Error tracking and notification
    private void LogAndNotifyException(Exception ex)
    {
        _telemetryClient.TrackException(new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error });
        TempData["StartupJavaScript"] = BuildErrorMessage(ex);
    }

    // Build user-safe error message
    private string BuildErrorMessage(Exception ex)
    {
        var contact = HttpContext.Session.GetString("ContactInfo") ?? "support";
        var msg = ex.InnerException == null ? ex.Message : ex.Message + " (Inner: " + ex.InnerException.Message + ")";
        return $"window.top.ShowToast('danger', 'Error', 'An error occured. Please report to {contact}: {msg.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString()}', 0, false);";
    }
}