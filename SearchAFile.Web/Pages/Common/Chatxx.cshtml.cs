using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Org.BouncyCastle.Asn1.Cmp;
using SearchAFile.Core.Domain.Entities;
using SearchAFile.Web.Extensions;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Pages.Common;

[BindProperties(SupportsGet = true)]
public class ChatxxModel : PageModel
{
    private readonly TelemetryClient _telemetryClient;
    private readonly AuthenticatedApiClient _api;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatxxModel(TelemetryClient telemetryClient, AuthenticatedApiClient api, IConfiguration configration, IHttpClientFactory httpClientFactory)
    {
        _telemetryClient = telemetryClient;
        _api = api;
        _configuration = configration;
        _httpClientFactory = httpClientFactory;
    }

    public string MessageColor;
    public string Message;
    public string Answer;


    // Set the base endpoint URL.
    private readonly string strBaseEndpointUrl = "https://api.openai.com/v1/";

    public Guid? Id { get; set; }
    public Collection Collection { get; set; }

    private HttpClient objHttpClient;

    public async Task<IActionResult> OnGetAsync([FromQuery] Guid? id)
    {
        try
        {
            if (Id == null)
                return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");

            // Set the page title.
            HttpContext.Session.SetString("PageTitle", "Chat");

            HttpContext.Session.SetObject("id", id);

            await LoadData();

            if (HttpContext.Session.GetString("strThreadID") != null)
            {
                await LoadChat();
            }

            ModelState.Remove("Question");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";

            return Redirect(HttpContext.Session.GetString("DashboardURL") ?? "/");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetAsk(string Question)
    {
        try
        {
            Id = HttpContext.Session.GetObject<Guid>("id");

            if (Id == null)
            {
                throw new Exception("Ask: No ID provided.");
            }

            if (string.IsNullOrEmpty(Question))
            {
                throw new Exception("Ask: No Question provided.");
            }

            Question = WebUtility.UrlDecode(Question);

            await LoadData();

            // Create the HttpClient object.
            if (objHttpClient == null)
            {
                objHttpClient = _httpClientFactory.CreateClient("SearchAFIleClient");
            }

            // Define the ResponseMessageContent string.
            string strResponseMessageContent;

            // If a thread has not been created yet, create one.
            string strThreadID = HttpContext.Session.GetString("strThreadID");

            if (strThreadID == null)
            {
                // Create the thread.
                using HttpResponseMessage objHttpResponseMessage1 = await objHttpClient.PostAsync($"{strBaseEndpointUrl}threads", null);
                if (!objHttpResponseMessage1.IsSuccessStatusCode)
                {
                    string errorContent = await objHttpResponseMessage1.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to create run: {errorContent}");
                }
                // Create the JsonDocument object.
                using JsonDocument objJsonDocument1 = await Conversions.CreateJsonDocumentObject(objHttpResponseMessage1);

                // Get the Thread ID from the JsonDocument object.
                strThreadID = objJsonDocument1.RootElement.GetProperty("id").GetString();

                // Save the thread in a session variable.
                HttpContext.Session.SetString("strThreadID", strThreadID);
            }

            // Get the file IDs:
            var fileIds = HttpContext.Session.GetObject<List<File>>("Files")?
                .Where(file => !string.IsNullOrEmpty(file.OpenAIFileId))
                .Select(file => file.OpenAIFileId!)
                .Take(20) // OpenAI limit
                .ToList();

            // Define the JsonRequestBody object.
            var objJsonRequestBody1 = new
            {
                role = "user",
                content = Question.Trim(),
                attachments = fileIds?.Select(id => new
                {
                    file_id = id,
                    tools = new[] { new { type = "file_search" } }
                }).ToList()
            };

            // Create the StringContent object.
            using StringContent objStringContent1 = Conversions.CreateStringContentObject(objJsonRequestBody1);

            // Add the message to the thread.
            using HttpResponseMessage objHttpResponseMessage2 = await objHttpClient.PostAsync($"{strBaseEndpointUrl}threads/{strThreadID}/messages", objStringContent1);
            if (!objHttpResponseMessage2.IsSuccessStatusCode)
            {
                string errorContent = await objHttpResponseMessage2.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create run: {errorContent}");
            }

            // Define the JsonRequestBody object.
            var objJsonRequestBody2 = new { assistant_id = _configuration["OpenAI:AssistantId"] };

            // Create the StringContent object.
            using StringContent objStringContent2 = Conversions.CreateStringContentObject(objJsonRequestBody2);

            // Create the run and initiate its execution.
            using HttpResponseMessage objHttpResponseMessage3 = await objHttpClient.PostAsync($"{strBaseEndpointUrl}threads/{strThreadID}/runs", objStringContent2);
            if (!objHttpResponseMessage3.IsSuccessStatusCode)
            {
                string errorContent = await objHttpResponseMessage3.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create run: {errorContent}");
            }

            // Create the JsonDocument object.
            using JsonDocument objJsonDocument2 = await Conversions.CreateJsonDocumentObject(objHttpResponseMessage3);

            // Get the Run ID from the JsonDocument object.
            string strRunID = objJsonDocument2.RootElement.GetProperty("id").GetString();

            // Every one seconds, check to see if the run has completed.
            int maxTries = 300; // 5 Minutes
            int currentTry = 0;
            string strRunStatus = "";

            while (strRunStatus != "completed")
            {
                currentTry++;

                if (currentTry > maxTries)
                {
                    await DeleteThread();
                    throw new Exception("Call to OpenAI timed out while waiting for run completion.");
                }

                await Task.Delay(1000);

                using HttpResponseMessage objHttpResponseMessage4 = await objHttpClient.GetAsync($"{strBaseEndpointUrl}threads/{strThreadID}/runs/{strRunID}");

                if (!objHttpResponseMessage4.IsSuccessStatusCode)
                {
                    await DeleteThread();
                    string errorContent = await objHttpResponseMessage4.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to check run status (Attempt {currentTry}): {errorContent}");
                }

                using JsonDocument objJsonDocument3 = await JsonDocument.ParseAsync(await objHttpResponseMessage4.Content.ReadAsStreamAsync());

                if (!objJsonDocument3.RootElement.TryGetProperty("status", out JsonElement statusElement))
                {
                    await DeleteThread();
                    throw new Exception($"Run status missing in response (Attempt {currentTry}).");
                }

                strRunStatus = statusElement.GetString();

                if (strRunStatus == "failed" || strRunStatus == "cancelled" || strRunStatus == "expired")
                {
                    throw new Exception($"OpenAI run ended unexpectedly with status: {strRunStatus}");
                }
            }

            await LoadChat();
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            return StatusCode(500, strExceptionMessage);
        }

        return Page();
    }

    public async Task OnPostStartNewChatAsync(Guid? id)
    {
        try
        {
            if (id == null)
            {
                await DeleteThread();
                throw new Exception("New Chat: No ID provided.");
            }

            await LoadData();

            await DeleteThread();

            TempData["StartupJavaScript"] = "ShowSnack('success', 'Chat successfully cleared.', 7000, true)";

            ModelState.Remove("Question");
        }
        catch (Exception ex)
        {
            // Log the exception to Application Insights.
            ExceptionTelemetry ExceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Error };
            _telemetryClient.TrackException(ExceptionTelemetry);

            // Display an error for the user.
            string strExceptionMessage = "An error occured. Please report the following error to " + HttpContext.Session.GetString("ContactInfo") + ": " + (ex.InnerException == null ? ex.Message : ex.Message + " (Inner Exception: " + ex.InnerException.Message + ")");
            TempData["StartupJavaScript"] = "window.top.ShowToast('danger', 'Error', '" + strExceptionMessage.Replace("\r", " ").Replace("\n", "<br>").EscapeJsString() + "', 0, false);";
        }
    }

    private async Task DeleteThread()
    {
        try
        {
            // Create the HttpClient object.
            HttpClient objHttpClient = _httpClientFactory.CreateClient("SearchAFIleClient");

            // Send the delete request to the threads endpoint.
            string strThreadID = HttpContext.Session.GetString("strThreadID");
            using HttpResponseMessage objHttpResponseMessage = await objHttpClient.DeleteAsync($"{strBaseEndpointUrl}threads/{strThreadID}");

            // Remove the session variable.
            HttpContext.Session.Remove("strThreadID");

        }
        catch
        {
            throw;
        }
    }

    private async Task LoadData()
    {
        try
        {
            var collectionResult = await _api.GetAsync<Collection>($"collections/{Id}");

            if (!collectionResult.IsSuccess || collectionResult.Data == null)
            {
                throw new Exception(collectionResult.ErrorMessage ?? "Unable to retrieve collection.");
            }

            Collection = collectionResult.Data;

            // Load the files.
            if (HttpContext.Session.GetObject<List<File>>("Files") == null)
            {
                var filesResult = await _api.GetAsync<List<File>>("files");

                if (!filesResult.IsSuccess || filesResult.Data == null)
                {
                    throw new Exception(filesResult.ErrorMessage ?? "Unable to retrieve files.");
                }

                List<File> Files = filesResult.Data.Where(file => file.CollectionId == Collection.CollectionId).ToList();

                HttpContext.Session.SetObject("Files", Files);
            }
        }
        catch
        {
            throw;
        }
    }

    private async Task LoadChat()
    {
        try
        {
            // Create the HttpClient object.
            if (objHttpClient == null)
            {
                objHttpClient = _httpClientFactory.CreateClient("SearchAFIleClient");
            }

            // If a thread has not been created yet, create one.
            string strThreadID = HttpContext.Session.GetString("strThreadID");

            // Get the Assistant's message.
            using HttpResponseMessage objHttpResponseMessage5 = await objHttpClient.GetAsync($"{strBaseEndpointUrl}threads/{strThreadID}/messages");

            // Create the JsonDocument object.
            using JsonDocument objJsonDocument4 = await Conversions.CreateJsonDocumentObject(objHttpResponseMessage5);

            // Format the answer to include the previous messages.
            foreach (JsonElement objJsonElement in objJsonDocument4.RootElement.GetProperty("data").EnumerateArray().Reverse())
            {
                string strRole = objJsonElement.GetProperty("role").GetString();
                if (strRole == "user" || strRole == "assistant")
                {
                    foreach (JsonElement objJsonElement2 in objJsonElement.GetProperty("content").EnumerateArray())
                    {
                        // Get the timestamp
                        long createdAtUnix = objJsonElement.GetProperty("created_at").GetInt64();
                        DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeSeconds(createdAtUnix).ToLocalTime();
                        string formattedTime = createdAt.ToString("h:mm tt");

                        string strMessageText = objJsonElement2.GetProperty("text").GetProperty("value").GetString();

                        string roleClass = strRole == "user" ? "user" : "bot";

                        Answer += $@"
                            <div class='chat-message {roleClass}'>
                                {strMessageText}
                                <div class='timestamp'>{formattedTime}</div>
                            </div>";
                    }
                }
            }
        }
        catch
        {
            throw;
        }
    }
}