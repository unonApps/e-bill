using Microsoft.AspNetCore.Http;

namespace TAB.Web.Services;

/// <summary>
/// Interface for Azure Blob Storage operations
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file to Azure Blob Storage at the specified path
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="blobPath">The full path including filename in the container</param>
    /// <returns>Tuple with success status, URL, and error message if any</returns>
    Task<(bool Success, string? Url, string? ErrorMessage)> UploadFileAsync(IFormFile file, string blobPath);

    /// <summary>
    /// Upload a file stream to Azure Blob Storage at the specified path
    /// </summary>
    /// <param name="stream">The file stream to upload</param>
    /// <param name="blobPath">The full path including filename in the container</param>
    /// <param name="contentType">The content type of the file</param>
    /// <returns>Tuple with success status, URL, and error message if any</returns>
    Task<(bool Success, string? Url, string? ErrorMessage)> UploadStreamAsync(Stream stream, string blobPath, string contentType);

    /// <summary>
    /// Download a file from Azure Blob Storage
    /// </summary>
    /// <param name="blobPath">The full path of the blob in the container</param>
    /// <returns>Tuple with success status, stream, content type, and error message if any</returns>
    Task<(bool Success, Stream? Stream, string? ContentType, string? ErrorMessage)> DownloadAsync(string blobPath);

    /// <summary>
    /// Delete a blob by its URL
    /// </summary>
    /// <param name="blobUrl">The full URL of the blob</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteBlobByUrlAsync(string blobUrl);

    /// <summary>
    /// Delete a blob by its path
    /// </summary>
    /// <param name="blobPath">The path of the blob in the container</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteBlobAsync(string blobPath);

    /// <summary>
    /// Check if a blob exists
    /// </summary>
    /// <param name="blobPath">The path of the blob in the container</param>
    /// <returns>True if the blob exists</returns>
    Task<bool> ExistsAsync(string blobPath);

    /// <summary>
    /// Get the full URL for a blob path
    /// </summary>
    /// <param name="blobPath">The path of the blob in the container</param>
    /// <returns>The full URL to the blob</returns>
    string GetBlobUrl(string blobPath);
}
