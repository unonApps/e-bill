using System;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // e.g., "StagingBatch", "CallLogStaging"

        [MaxLength(100)]
        public string? EntityId { get; set; } // The ID of the entity being audited

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // e.g., "Created", "Updated", "Deleted", "Verified", "Rejected"

        [MaxLength(500)]
        public string? Description { get; set; } // Human-readable description

        [MaxLength(2000)]
        public string? OldValues { get; set; } // JSON of old values

        [MaxLength(2000)]
        public string? NewValues { get; set; } // JSON of new values

        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; } = string.Empty; // Username or system

        public DateTime PerformedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(50)]
        public string? Module { get; set; } // e.g., "CallLogStaging", "UserManagement"

        public bool IsSuccess { get; set; } = true;

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; } // If action failed

        // Additional context
        [MaxLength(4000)]
        public string? AdditionalData { get; set; } // JSON for any extra data
    }

    public enum AuditAction
    {
        Created,
        Updated,
        Deleted,
        Verified,
        Rejected,
        Published,
        RolledBack,
        BulkVerified,
        BulkRejected,
        Login,
        Logout,
        PasswordChanged,
        RoleAssigned,
        RoleRemoved,
        PhoneAssigned,
        PhoneUnassigned,
        PhoneReassigned,
        PhoneStatusChanged,
        PhonePrimarySet,
        CallPaymentAssigned,
        CallPaymentAccepted,
        CallPaymentRejected,
        CallAssignmentStatusChanged,

        // SIM Request Workflow
        SimRequestSubmitted,
        SimRequestApproved,
        SimRequestRejected,
        SimRequestIctsProcessing,
        SimRequestCollectionNotified,
        SimRequestCompleted,

        // Refund Request Workflow
        RefundRequestSubmitted,
        RefundRequestApproved,
        RefundRequestRejected,
        RefundRequestCompleted,

        // Call Log Verification Workflow
        CallLogVerificationSubmitted,
        CallLogVerificationApproved,
        CallLogVerificationRejected
    }
}