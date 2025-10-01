using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

/// <summary>
/// Super small surface: ensure vector store is healthy,
/// and if it's dead/expired, recreate and reattach your known OpenAI file ids.
/// </summary>
public interface IOpenAIVectorStoreService
{
    /// <summary>
    /// Verifies the vector store is usable. If expired, it will:
    /// 1) Create a new vector store (using provided name/metadata),
    /// 2) Reattach all provided OpenAI file ids,
    /// 3) Return the (possibly updated) vector store id.
    /// </summary>
    Task<string> EnsureReadyOrRepairAsync(
        string vectorStoreId,
        IEnumerable<string> existingOpenAiFileIds,
        string nameIfRecreated,
        IDictionary<string, string> metadataIfRecreated,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a brand new vector store (simple helper).
    /// </summary>
    Task<string> CreateAsync(
        string name,
        IDictionary<string, string> metadata,
        int? expiresAfterDays = null,
        CancellationToken ct = default);

    Task<bool> DetachFileAsync(string vectorStoreId, 
        string openAiFileId, 
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string vectorStoreId, 
        CancellationToken ct = default);
}