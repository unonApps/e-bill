using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public interface IDocumentManagementService
    {
        /// <summary>
        /// Uploads a document for a call log verification
        /// </summary>
        Task<CallLogDocument> UploadDocumentAsync(
            int verificationId,
            IFormFile file,
            DocumentType documentType,
            string uploadedBy,
            string? description = null);

        /// <summary>
        /// Downloads a document by ID
        /// </summary>
        Task<(Stream FileStream, string FileName, string ContentType)> DownloadDocumentAsync(int documentId);

        /// <summary>
        /// Deletes a document
        /// </summary>
        Task<bool> DeleteDocumentAsync(int documentId, string indexNumber);

        /// <summary>
        /// Gets all documents for a verification
        /// </summary>
        Task<List<CallLogDocument>> GetDocumentsAsync(int verificationId);

        /// <summary>
        /// Gets a single document by ID
        /// </summary>
        Task<CallLogDocument?> GetDocumentByIdAsync(int documentId);

        /// <summary>
        /// Validates file before upload
        /// </summary>
        Task<ValidationResult> ValidateFileAsync(IFormFile file);

        /// <summary>
        /// Gets total storage used by a user
        /// </summary>
        Task<long> GetUserStorageUsageAsync(string indexNumber);

        /// <summary>
        /// Cleans up orphaned documents (verifications deleted but documents remain)
        /// </summary>
        Task<int> CleanupOrphanedDocumentsAsync();
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
