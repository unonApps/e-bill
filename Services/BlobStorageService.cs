using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TAB.Web.Options;

namespace TAB.Web.Services;

/// <summary>
/// Azure Blob Storage service implementation for file uploads
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;
    private BlobContainerClient? _containerClient;

    // Allowed file extensions for import files
    private static readonly string[] AllowedExtensions = { ".csv", ".xlsx", ".xls" };

    // Maximum file size: 500 MB (matching Kestrel config)
    private const long MaxFileSize = 500 * 1024 * 1024;

    public BlobStorageService(IOptions<BlobStorageOptions> options, ILogger<BlobStorageService> logger)
    {
        _options = options.Value ?? new BlobStorageOptions();
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.StorageConnection) && !string.IsNullOrEmpty(_options.ContainerName))
        {
            var blobServiceClient = new BlobServiceClient(_options.StorageConnection);
            _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            _logger.LogInformation("Azure Blob Storage configured with container: {Container}", _options.ContainerName);
        }
        else
        {
            _logger.LogWarning("Azure Blob Storage is not configured. Import file uploads will fail until configuration is provided.");
        }
    }

    private BlobContainerClient GetContainerClient()
    {
        if (_containerClient == null)
            throw new InvalidOperationException("Azure Blob Storage is not configured. Please add AzureBlobStorage settings to app configuration or environment variables.");
        return _containerClient;
    }

    /// <summary>
    /// Upload a file to Azure Blob Storage
    /// </summary>
    public async Task<(bool Success, string? Url, string? ErrorMessage)> UploadFileAsync(IFormFile file, string blobPath)
    {
        try
        {
            // Validate file
            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                return (false, null, validationError);
            }

            var container = GetContainerClient();

            // Ensure container exists
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // Get blob client
            var blobClient = container.GetBlobClient(blobPath);

            // Upload with content type
            var contentType = GetContentType(file.FileName);
            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

            var url = blobClient.Uri.ToString();
            _logger.LogInformation("Successfully uploaded file to blob: {BlobPath}", blobPath);

            return (true, url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to blob: {BlobPath}", blobPath);
            return (false, null, $"Upload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload a stream to Azure Blob Storage
    /// </summary>
    public async Task<(bool Success, string? Url, string? ErrorMessage)> UploadStreamAsync(Stream stream, string blobPath, string contentType)
    {
        try
        {
            var container = GetContainerClient();

            // Ensure container exists
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            // Get blob client
            var blobClient = container.GetBlobClient(blobPath);

            // Upload with content type
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

            var url = blobClient.Uri.ToString();
            _logger.LogInformation("Successfully uploaded stream to blob: {BlobPath}", blobPath);

            return (true, url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload stream to blob: {BlobPath}", blobPath);
            return (false, null, $"Upload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Download a blob as a stream
    /// </summary>
    public async Task<(bool Success, Stream? Stream, string? ContentType, string? ErrorMessage)> DownloadAsync(string blobPath)
    {
        try
        {
            var container = GetContainerClient();
            var blobClient = container.GetBlobClient(blobPath);

            if (!await blobClient.ExistsAsync())
            {
                return (false, null, null, "Blob not found");
            }

            var response = await blobClient.DownloadStreamingAsync();
            var contentType = response.Value.Details.ContentType;

            _logger.LogInformation("Successfully downloaded blob: {BlobPath}", blobPath);

            return (true, response.Value.Content, contentType, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {BlobPath}", blobPath);
            return (false, null, null, $"Download failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a blob by its full URL
    /// </summary>
    public async Task<bool> DeleteBlobByUrlAsync(string blobUrl)
    {
        try
        {
            var blobPath = ExtractBlobPath(blobUrl);
            if (string.IsNullOrEmpty(blobPath))
            {
                _logger.LogWarning("Could not extract blob path from URL: {Url}", blobUrl);
                return false;
            }

            return await DeleteBlobAsync(blobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob by URL: {Url}", blobUrl);
            return false;
        }
    }

    /// <summary>
    /// Delete a blob by its path
    /// </summary>
    public async Task<bool> DeleteBlobAsync(string blobPath)
    {
        try
        {
            var container = GetContainerClient();
            var blobClient = container.GetBlobClient(blobPath);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob: {BlobPath}", blobPath);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob: {BlobPath}", blobPath);
            return false;
        }
    }

    /// <summary>
    /// Check if a blob exists
    /// </summary>
    public async Task<bool> ExistsAsync(string blobPath)
    {
        try
        {
            var container = GetContainerClient();
            var blobClient = container.GetBlobClient(blobPath);
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check blob existence: {BlobPath}", blobPath);
            return false;
        }
    }

    /// <summary>
    /// Get the full URL for a blob path
    /// </summary>
    public string GetBlobUrl(string blobPath)
    {
        return $"https://{_options.StorageAccountName}.blob.core.windows.net/{_options.ContainerName}/{blobPath}";
    }

    #region Private Helper Methods

    private string? ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return "No file provided or file is empty";
        }

        if (file.Length > MaxFileSize)
        {
            return $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}";
        }

        return null;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }

    private string? ExtractBlobPath(string blobUrl)
    {
        try
        {
            var uri = new Uri(blobUrl);
            var path = uri.AbsolutePath;

            // Remove container name from path
            var containerPrefix = $"/{_options.ContainerName}/";
            if (path.StartsWith(containerPrefix))
            {
                return path.Substring(containerPrefix.Length);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
