using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public class DocumentManagementService : IDocumentManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentManagementService> _logger;
        private readonly IConfiguration _configuration;

        // File validation settings
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;
        private readonly string _uploadPath;

        public DocumentManagementService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<DocumentManagementService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;

            // Load settings from configuration
            _maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSize", 10485760); // 10MB default
            _allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                ?? new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx" };
            _uploadPath = _configuration.GetValue<string>("FileStorage:DocumentsPath", "wwwroot/uploads/call-log-documents")
                ?? "wwwroot/uploads/call-log-documents";
        }

        public async Task<CallLogDocument> UploadDocumentAsync(
            int verificationId,
            IFormFile file,
            DocumentType documentType,
            string uploadedBy,
            string? description = null)
        {
            try
            {
                // Validate file
                var validation = await ValidateFileAsync(file);
                if (!validation.IsValid)
                {
                    throw new ArgumentException($"File validation failed: {string.Join(", ", validation.Errors)}");
                }

                // Verify verification exists
                var verification = await _context.CallLogVerifications.FindAsync(verificationId);
                if (verification == null)
                {
                    throw new ArgumentException($"Verification with ID {verificationId} not found");
                }

                // Ensure user has permission
                if (verification.VerifiedBy != uploadedBy)
                {
                    throw new UnauthorizedAccessException("You can only upload documents for your own verifications");
                }

                // Create upload directory if it doesn't exist
                var uploadDirectory = Path.Combine(_environment.ContentRootPath, _uploadPath);
                if (!Directory.Exists(uploadDirectory))
                {
                    Directory.CreateDirectory(uploadDirectory);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create document record
                var document = new CallLogDocument
                {
                    CallLogVerificationId = verificationId,
                    FileName = file.FileName,
                    FilePath = uniqueFileName, // Store only the filename, not full path
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    DocumentType = documentType.ToString(),
                    Description = description,
                    UploadedBy = uploadedBy,
                    UploadedDate = DateTime.UtcNow
                };

                await _context.CallLogDocuments.AddAsync(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {FileName} uploaded for verification {VerificationId} by {UploadedBy}",
                    file.FileName, verificationId, uploadedBy);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for verification {VerificationId}", verificationId);
                throw;
            }
        }

        public async Task<(Stream FileStream, string FileName, string ContentType)> DownloadDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.CallLogDocuments.FindAsync(documentId);
                if (document == null)
                {
                    throw new FileNotFoundException($"Document with ID {documentId} not found");
                }

                var uploadDirectory = Path.Combine(_environment.ContentRootPath, _uploadPath);
                var filePath = Path.Combine(uploadDirectory, document.FilePath);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Physical file not found: {document.FilePath}");
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                return (fileStream, document.FileName, document.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId, string indexNumber)
        {
            try
            {
                var document = await _context.CallLogDocuments
                    .Include(d => d.CallLogVerification)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                    return false;

                // Verify user has permission
                if (document.CallLogVerification.VerifiedBy != indexNumber)
                {
                    _logger.LogWarning("User {IndexNumber} attempted to delete document {DocumentId} they don't own",
                        indexNumber, documentId);
                    return false;
                }

                // Cannot delete if verification is submitted
                if (document.CallLogVerification.SubmittedToSupervisor)
                {
                    _logger.LogWarning("Cannot delete document {DocumentId} - verification already submitted", documentId);
                    return false;
                }

                // Delete physical file
                var uploadDirectory = Path.Combine(_environment.ContentRootPath, _uploadPath);
                var filePath = Path.Combine(uploadDirectory, document.FilePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Delete database record
                _context.CallLogDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} deleted by {IndexNumber}", documentId, indexNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return false;
            }
        }

        public async Task<List<CallLogDocument>> GetDocumentsAsync(int verificationId)
        {
            return await _context.CallLogDocuments
                .Where(d => d.CallLogVerificationId == verificationId)
                .OrderBy(d => d.UploadedDate)
                .ToListAsync();
        }

        public async Task<CallLogDocument?> GetDocumentByIdAsync(int documentId)
        {
            return await _context.CallLogDocuments
                .Include(d => d.CallLogVerification)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public Task<ValidationResult> ValidateFileAsync(IFormFile file)
        {
            var result = new ValidationResult { IsValid = true };

            if (file == null || file.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("File is empty or not provided");
                return Task.FromResult(result);
            }

            // Check file size
            if (file.Length > _maxFileSize)
            {
                result.IsValid = false;
                result.Errors.Add($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024}MB");
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                result.IsValid = false;
                result.Errors.Add($"File type '{fileExtension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Check for potentially malicious filenames
            var fileName = Path.GetFileName(file.FileName);
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                result.IsValid = false;
                result.Errors.Add("File name contains invalid characters");
            }

            return Task.FromResult(result);
        }

        public async Task<long> GetUserStorageUsageAsync(string indexNumber)
        {
            return await _context.CallLogDocuments
                .Where(d => d.UploadedBy == indexNumber)
                .SumAsync(d => d.FileSize);
        }

        public async Task<int> CleanupOrphanedDocumentsAsync()
        {
            try
            {
                // Find documents whose verifications have been deleted
                var orphanedDocuments = await _context.CallLogDocuments
                    .Where(d => !_context.CallLogVerifications.Any(v => v.Id == d.CallLogVerificationId))
                    .ToListAsync();

                var uploadDirectory = Path.Combine(_environment.ContentRootPath, _uploadPath);
                int deletedCount = 0;

                foreach (var document in orphanedDocuments)
                {
                    // Delete physical file
                    var filePath = Path.Combine(uploadDirectory, document.FilePath);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // Delete database record
                    _context.CallLogDocuments.Remove(document);
                    deletedCount++;
                }

                if (deletedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} orphaned documents", deletedCount);
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned documents");
                return 0;
            }
        }
    }
}
