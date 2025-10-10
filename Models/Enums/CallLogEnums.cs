namespace TAB.Web.Models.Enums
{
    public enum VerificationType
    {
        Personal,
        Official
    }

    public enum AssignmentStatus
    {
        None,           // No assignment, belongs to original phone owner
        Pending,        // Assignment created, awaiting acceptance
        Accepted,       // Assignment accepted by recipient
        Rejected,       // Assignment rejected, reverted to original owner
        Reassigned      // Assignment passed to another user
    }

    public enum DocumentType
    {
        OverageJustification,
        ApprovalLetter,
        Receipt,
        Other
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        PartiallyApproved,
        Rejected,
        Reverted
    }

    public enum SupervisorAction
    {
        Approve,
        PartiallyApprove,
        Reject,
        Revert
    }
}
