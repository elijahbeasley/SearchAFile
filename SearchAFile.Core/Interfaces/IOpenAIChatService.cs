using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Core.Interfaces;

public interface IOpenAIChatService
{
    /// <summary>Create a thread bound to a vector store (attached at thread level).</summary>
    Task<string> CreateThreadForVectorStoreAsync(string vectorStoreId, CancellationToken ct = default);

    /// <summary>Delete a thread (idempotent; ignore 404).</summary>
    Task DeleteThreadAsync(string threadId, CancellationToken ct = default);

    /// <summary>
    /// Send a user message to a thread, start a run with the given assistant, poll until complete,
    /// and return the assistant's latest text (plain + simple HTML).
    /// </summary>
    Task<(string Plain, string Html)> AskAsync(string threadId, string assistantId, string userQuestion, CancellationToken ct = default);

    /// <summary>Fetch the latest assistant message without sending a new one (optional).</summary>
    Task<(string Plain, string Html)> GetLatestAssistantAsync(string threadId, CancellationToken ct = default);

    Task<List<ChatMessage>> GetThreadHistoryAsync(string threadId, int takeLast = 100);

    // NEW: returns HTML built from OpenAI annotations (has <a class="saf-cite" data-file-id="...">)
    Task<List<(string Role, string Html, DateTimeOffset? Timestamp)>> GetThreadHistoryHtmlAsync(string threadId, int takeLast = 100);
}