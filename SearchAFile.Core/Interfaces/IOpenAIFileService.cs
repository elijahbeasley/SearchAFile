using SearchAFile.Core.Domain.Entities;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SearchAFile.Core.Interfaces;

/// <summary>
/// Uploads bytes to OpenAI (purpose=assistants) and attaches to a vector store.
/// </summary>
public interface IOpenAIFileService
{
    /// <summary>
    /// Upload a file to OpenAI and attach it to the vector store (waits until indexed).
    /// Returns the OpenAI file id.
    /// </summary>
    Task<string> UploadAndAttachAsync(
        string vectorStoreId,
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken ct = default);

    Task DeleteAsync(string openAiFileId, CancellationToken ct = default);
}