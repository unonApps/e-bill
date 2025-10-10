using System;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class ImportAudit
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ImportType { get; set; } = string.Empty; // "CallLogs", "EbillUsers", etc.
        
        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public int UpdatedCount { get; set; }
        
        [Required]
        public DateTime ImportDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ImportedBy { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        public TimeSpan ProcessingTime { get; set; }
        
        // Store detailed results as JSON
        public string? DetailedResults { get; set; } // JSON containing errors, skipped records, etc.
        
        // Summary message shown to user
        [StringLength(500)]
        public string? SummaryMessage { get; set; }
        
        // Import options used
        public string? ImportOptions { get; set; } // JSON containing options like updateExisting, skipUnmatched, etc.

        // Date format preferences for this import type
        [StringLength(500)]
        public string? DateFormatPreferences { get; set; } // JSON containing detected/selected date formats per column
    }
    
    public class ImportDetailedResult
    {
        public List<ImportErrorDetail> Errors { get; set; } = new();
        public List<ImportSkippedDetail> Skipped { get; set; } = new();
        public List<ImportSuccessDetail> Successes { get; set; } = new();
        public List<ImportUpdatedDetail> Updated { get; set; } = new();
    }
    
    public class ImportErrorDetail
    {
        public int LineNumber { get; set; }
        public string? OriginalData { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? FieldName { get; set; }
        public string? FieldValue { get; set; } // The specific value that caused the error
        public string? ErrorType { get; set; } // "ValidationError", "DateFormatError", "RequiredFieldMissing", etc.
        public string? SuggestedFix { get; set; } // Optional suggested correction
    }
    
    public class ImportSkippedDetail
    {
        public int LineNumber { get; set; }
        public string? OriginalData { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? LookupValue { get; set; } // e.g., phone number that couldn't be matched
    }
    
    public class ImportSuccessDetail
    {
        public int LineNumber { get; set; }
        public int RecordId { get; set; }
        public string? Summary { get; set; }
    }
    
    public class ImportUpdatedDetail
    {
        public int LineNumber { get; set; }
        public int RecordId { get; set; }
        public string? Summary { get; set; }
        public string? ChangedFields { get; set; }
    }
}