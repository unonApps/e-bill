using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using System.Text.Json;

namespace TAB.Web.Services
{
    public interface IAuditLogService
    {
        Task LogPhoneAssignedAsync(string phoneNumber, string indexNumber, string userName, string phoneType, PhoneStatus status, string performedBy, string? ipAddress = null);
        Task LogPhoneUnassignedAsync(string phoneNumber, string indexNumber, string userName, string performedBy, string? ipAddress = null);
        Task LogPhoneReassignedAsync(string phoneNumber, string fromIndexNumber, string fromUserName, string toIndexNumber, string toUserName, string performedBy, string? ipAddress = null);
        Task LogPhoneStatusChangedAsync(string phoneNumber, string indexNumber, string userName, PhoneStatus oldStatus, PhoneStatus newStatus, string performedBy, string? ipAddress = null);
        Task LogPhonePrimarySetAsync(string phoneNumber, string indexNumber, string userName, string performedBy, string? ipAddress = null);
        Task LogPhoneEditedAsync(string phoneNumber, string indexNumber, string userName, Dictionary<string, object> oldValues, Dictionary<string, object> newValues, string performedBy, string? ipAddress = null);
        Task LogUserCreatedAsync(string indexNumber, string userName, string email, string? phoneNumber, string performedBy, string? ipAddress = null);
        Task LogUserEditedAsync(string indexNumber, string userName, Dictionary<string, object> oldValues, Dictionary<string, object> newValues, string performedBy, string? ipAddress = null);
        Task LogCallPaymentAssignedAsync(int callRecordId, string assignedFrom, string assignedFromName, string assignedTo, string assignedToName, string reason, decimal amount, string performedBy, string? ipAddress = null);
        Task LogCallPaymentAcceptedAsync(int callRecordId, int assignmentId, string acceptedBy, string acceptedByName, string performedBy, string? ipAddress = null);
        Task LogCallPaymentRejectedAsync(int callRecordId, int assignmentId, string rejectedBy, string rejectedByName, string reason, string performedBy, string? ipAddress = null);
        Task LogCallAssignmentStatusChangedAsync(int callRecordId, string oldStatus, string newStatus, string changedBy, string? reason = null, string? ipAddress = null);

        // SIM Request Workflow Audit Logs
        Task LogSimRequestSubmittedAsync(int requestId, string requesterName, string requesterIndex, string serviceProvider, string performedBy, string? ipAddress = null);
        Task LogSimRequestApprovedAsync(int requestId, string approverRole, string approverName, string requesterName, string? comments, string performedBy, string? ipAddress = null);
        Task LogSimRequestRejectedAsync(int requestId, string approverRole, string approverName, string requesterName, string? reason, string performedBy, string? ipAddress = null);
        Task LogSimRequestIctsProcessingAsync(int requestId, string ictsStaffName, string requesterName, string? assignedNumber, string? comments, string performedBy, string? ipAddress = null);
        Task LogSimRequestCollectionNotifiedAsync(int requestId, string ictsStaffName, string requesterName, string? assignedNumber, string performedBy, string? ipAddress = null);
        Task LogSimRequestCompletedAsync(int requestId, string ictsStaffName, string requesterName, string assignedNumber, string performedBy, string? ipAddress = null);

        // Refund Request Workflow Audit Logs
        Task LogRefundRequestSubmittedAsync(int requestId, string requesterName, string requesterIndex, decimal amount, string phoneNumber, string performedBy, string? ipAddress = null);
        Task LogRefundRequestApprovedAsync(int requestId, string approverRole, string approverName, string requesterName, decimal amount, string? comments, string performedBy, string? ipAddress = null);
        Task LogRefundRequestRejectedAsync(int requestId, string approverRole, string approverName, string requesterName, decimal amount, string? reason, string performedBy, string? ipAddress = null);
        Task LogRefundRequestCompletedAsync(int requestId, string approverName, string requesterName, decimal amount, string? paymentReference, string performedBy, string? ipAddress = null);

        // Call Log Verification Workflow Audit Logs
        Task LogCallLogVerificationSubmittedAsync(int verificationId, string requesterName, string requesterIndex, int callCount, decimal totalAmount, string performedBy, string? ipAddress = null);
        Task LogCallLogVerificationApprovedAsync(int verificationId, string supervisorName, string requesterName, int callCount, decimal totalAmount, string? comments, string performedBy, string? ipAddress = null);
        Task LogCallLogVerificationRejectedAsync(int verificationId, string supervisorName, string requesterName, int callCount, decimal totalAmount, string? reason, string performedBy, string? ipAddress = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogPhoneAssignedAsync(string phoneNumber, string indexNumber, string userName, string phoneType, PhoneStatus status, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.PhoneAssigned.ToString(),
                    Description = $"Phone number {phoneNumber} ({phoneType}) assigned to {userName} (Index: {indexNumber}) with status {status}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        PhoneNumber = phoneNumber,
                        IndexNumber = indexNumber,
                        UserName = userName,
                        PhoneType = phoneType,
                        Status = status.ToString()
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging phone assignment audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogPhoneUnassignedAsync(string phoneNumber, string indexNumber, string userName, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.PhoneUnassigned.ToString(),
                    Description = $"Phone number {phoneNumber} unassigned from {userName} (Index: {indexNumber})",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        PhoneNumber = phoneNumber,
                        IndexNumber = indexNumber,
                        UserName = userName
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging phone unassignment audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogPhoneReassignedAsync(string phoneNumber, string fromIndexNumber, string fromUserName, string toIndexNumber, string toUserName, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.PhoneReassigned.ToString(),
                    Description = $"Phone number {phoneNumber} reassigned from {fromUserName} (Index: {fromIndexNumber}) to {toUserName} (Index: {toIndexNumber})",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        IndexNumber = fromIndexNumber,
                        UserName = fromUserName
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        IndexNumber = toIndexNumber,
                        UserName = toUserName
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging phone reassignment audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogPhoneStatusChangedAsync(string phoneNumber, string indexNumber, string userName, PhoneStatus oldStatus, PhoneStatus newStatus, string performedBy, string? ipAddress = null)
        {
            try
            {
                string action = newStatus switch
                {
                    PhoneStatus.Active => "activated",
                    PhoneStatus.Suspended => "suspended",
                    PhoneStatus.Deactivated => "deactivated",
                    _ => "status changed"
                };

                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.PhoneStatusChanged.ToString(),
                    Description = $"Phone number {phoneNumber} for {userName} (Index: {indexNumber}) {action} (from {oldStatus} to {newStatus})",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        Status = oldStatus.ToString()
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        Status = newStatus.ToString()
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging phone status change audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogPhonePrimarySetAsync(string phoneNumber, string indexNumber, string userName, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.PhonePrimarySet.ToString(),
                    Description = $"Phone number {phoneNumber} set as primary for {userName} (Index: {indexNumber})",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        PhoneNumber = phoneNumber,
                        IndexNumber = indexNumber,
                        UserName = userName,
                        IsPrimary = true
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging set primary phone audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogPhoneEditedAsync(string phoneNumber, string indexNumber, string userName, Dictionary<string, object> oldValues, Dictionary<string, object> newValues, string performedBy, string? ipAddress = null)
        {
            try
            {
                var changes = new List<string>();
                foreach (var key in newValues.Keys)
                {
                    if (oldValues.ContainsKey(key) && !Equals(oldValues[key], newValues[key]))
                    {
                        changes.Add($"{key}: {oldValues[key]} → {newValues[key]}");
                    }
                }

                var auditLog = new AuditLog
                {
                    EntityType = "UserPhone",
                    EntityId = phoneNumber,
                    Action = AuditAction.Updated.ToString(),
                    Description = $"Phone number {phoneNumber} for {userName} (Index: {indexNumber}) updated: {string.Join(", ", changes)}",
                    OldValues = JsonSerializer.Serialize(oldValues),
                    NewValues = JsonSerializer.Serialize(newValues),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserPhoneManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging phone edit audit for {PhoneNumber}", phoneNumber);
            }
        }

        public async Task LogUserCreatedAsync(string indexNumber, string userName, string email, string? phoneNumber, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "EbillUser",
                    EntityId = indexNumber,
                    Action = AuditAction.Created.ToString(),
                    Description = $"Ebill user created: {userName} (Index: {indexNumber}, Email: {email})" +
                                  (phoneNumber != null ? $" with phone {phoneNumber}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        IndexNumber = indexNumber,
                        UserName = userName,
                        Email = email,
                        PhoneNumber = phoneNumber
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user creation audit for {IndexNumber}", indexNumber);
            }
        }

        public async Task LogUserEditedAsync(string indexNumber, string userName, Dictionary<string, object> oldValues, Dictionary<string, object> newValues, string performedBy, string? ipAddress = null)
        {
            try
            {
                var changes = new List<string>();
                foreach (var key in newValues.Keys)
                {
                    if (oldValues.ContainsKey(key) && !Equals(oldValues[key], newValues[key]))
                    {
                        changes.Add($"{key}: {oldValues[key]} → {newValues[key]}");
                    }
                }

                var auditLog = new AuditLog
                {
                    EntityType = "EbillUser",
                    EntityId = indexNumber,
                    Action = AuditAction.Updated.ToString(),
                    Description = $"Ebill user {userName} (Index: {indexNumber}) updated: {string.Join(", ", changes)}",
                    OldValues = JsonSerializer.Serialize(oldValues),
                    NewValues = JsonSerializer.Serialize(newValues),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "UserManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user edit audit for {IndexNumber}", indexNumber);
            }
        }

        public async Task LogCallPaymentAssignedAsync(int callRecordId, string assignedFrom, string assignedFromName, string assignedTo, string assignedToName, string reason, decimal amount, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallRecord",
                    EntityId = callRecordId.ToString(),
                    Action = AuditAction.CallPaymentAssigned.ToString(),
                    Description = $"Call payment (${amount:F2}) assigned from {assignedFromName} (Index: {assignedFrom}) to {assignedToName} (Index: {assignedTo}). Reason: {reason}",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        ResponsibleIndexNumber = assignedFrom,
                        ResponsibleUserName = assignedFromName,
                        PayingIndexNumber = assignedFrom,
                        AssignmentStatus = "None"
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ResponsibleIndexNumber = assignedFrom,
                        ResponsibleUserName = assignedFromName,
                        PayingIndexNumber = assignedTo,
                        PayingUserName = assignedToName,
                        AssignmentStatus = "Pending",
                        AssignmentReason = reason,
                        Amount = amount
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call payment assignment audit for CallRecord {CallRecordId}", callRecordId);
            }
        }

        public async Task LogCallPaymentAcceptedAsync(int callRecordId, int assignmentId, string acceptedBy, string acceptedByName, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallRecord",
                    EntityId = callRecordId.ToString(),
                    Action = AuditAction.CallPaymentAccepted.ToString(),
                    Description = $"Call payment assignment #{assignmentId} accepted by {acceptedByName} (Index: {acceptedBy})",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = "Pending",
                        AssignmentId = assignmentId
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = "Accepted",
                        AcceptedBy = acceptedBy,
                        AcceptedByName = acceptedByName,
                        AcceptedDate = DateTime.UtcNow
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call payment acceptance audit for CallRecord {CallRecordId}", callRecordId);
            }
        }

        public async Task LogCallPaymentRejectedAsync(int callRecordId, int assignmentId, string rejectedBy, string rejectedByName, string reason, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallRecord",
                    EntityId = callRecordId.ToString(),
                    Action = AuditAction.CallPaymentRejected.ToString(),
                    Description = $"Call payment assignment #{assignmentId} rejected by {rejectedByName} (Index: {rejectedBy}). Reason: {reason}",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = "Pending",
                        AssignmentId = assignmentId
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = "Rejected",
                        RejectedBy = rejectedBy,
                        RejectedByName = rejectedByName,
                        RejectionReason = reason,
                        RejectedDate = DateTime.UtcNow
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call payment rejection audit for CallRecord {CallRecordId}", callRecordId);
            }
        }

        public async Task LogCallAssignmentStatusChangedAsync(int callRecordId, string oldStatus, string newStatus, string changedBy, string? reason = null, string? ipAddress = null)
        {
            try
            {
                var description = $"Call assignment status changed from '{oldStatus}' to '{newStatus}' by {changedBy}";
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    description += $". Reason: {reason}";
                }

                var auditLog = new AuditLog
                {
                    EntityType = "CallRecord",
                    EntityId = callRecordId.ToString(),
                    Action = AuditAction.CallAssignmentStatusChanged.ToString(),
                    Description = description,
                    OldValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = oldStatus
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        AssignmentStatus = newStatus,
                        ChangedBy = changedBy,
                        Reason = reason
                    }),
                    PerformedBy = changedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call assignment status change audit for CallRecord {CallRecordId}", callRecordId);
            }
        }

        // SIM Request Workflow Audit Logs
        public async Task LogSimRequestSubmittedAsync(int requestId, string requesterName, string requesterIndex, string serviceProvider, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestSubmitted.ToString(),
                    Description = $"SIM request #{requestId} submitted by {requesterName} (Index: {requesterIndex}) for {serviceProvider}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        RequestId = requestId,
                        RequesterName = requesterName,
                        RequesterIndex = requesterIndex,
                        ServiceProvider = serviceProvider,
                        Status = "PendingSupervisor"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM request submission audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogSimRequestApprovedAsync(int requestId, string approverRole, string approverName, string requesterName, string? comments, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestApproved.ToString(),
                    Description = $"SIM request #{requestId} for {requesterName} approved by {approverRole} {approverName}" +
                                  (!string.IsNullOrWhiteSpace(comments) ? $". Comments: {comments}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApproverRole = approverRole,
                        ApproverName = approverName,
                        Comments = comments,
                        ApprovedDate = DateTime.UtcNow
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM request approval audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogSimRequestRejectedAsync(int requestId, string approverRole, string approverName, string requesterName, string? reason, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestRejected.ToString(),
                    Description = $"SIM request #{requestId} for {requesterName} rejected by {approverRole} {approverName}. Reason: {reason}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApproverRole = approverRole,
                        ApproverName = approverName,
                        RejectionReason = reason,
                        RejectedDate = DateTime.UtcNow,
                        Status = "Rejected"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM request rejection audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogSimRequestIctsProcessingAsync(int requestId, string ictsStaffName, string requesterName, string? assignedNumber, string? comments, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestIctsProcessing.ToString(),
                    Description = $"SIM request #{requestId} for {requesterName} processed by ICTS {ictsStaffName}" +
                                  (!string.IsNullOrWhiteSpace(assignedNumber) ? $". Assigned number: {assignedNumber}" : "") +
                                  (!string.IsNullOrWhiteSpace(comments) ? $". Comments: {comments}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        IctsStaffName = ictsStaffName,
                        AssignedNumber = assignedNumber,
                        Comments = comments,
                        ProcessedDate = DateTime.UtcNow,
                        Status = "PendingServiceProvider"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM request ICTS processing audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogSimRequestCollectionNotifiedAsync(int requestId, string ictsStaffName, string requesterName, string? assignedNumber, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestCollectionNotified.ToString(),
                    Description = $"SIM ready for collection notification sent for request #{requestId} ({requesterName}) by ICTS {ictsStaffName}" +
                                  (!string.IsNullOrWhiteSpace(assignedNumber) ? $". Number: {assignedNumber}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        IctsStaffName = ictsStaffName,
                        AssignedNumber = assignedNumber,
                        CollectionNotifiedDate = DateTime.UtcNow,
                        Status = "PendingSIMCollection"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM collection notification audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogSimRequestCompletedAsync(int requestId, string ictsStaffName, string requesterName, string assignedNumber, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "SimRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.SimRequestCompleted.ToString(),
                    Description = $"SIM request #{requestId} for {requesterName} completed by ICTS {ictsStaffName}. Number: {assignedNumber}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        IctsStaffName = ictsStaffName,
                        AssignedNumber = assignedNumber,
                        CompletedDate = DateTime.UtcNow,
                        Status = "Completed"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "SimManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging SIM request completion audit for Request {RequestId}", requestId);
            }
        }

        // Refund Request Workflow Audit Logs
        public async Task LogRefundRequestSubmittedAsync(int requestId, string requesterName, string requesterIndex, decimal amount, string phoneNumber, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "RefundRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.RefundRequestSubmitted.ToString(),
                    Description = $"Refund request #{requestId} submitted by {requesterName} (Index: {requesterIndex}) for ${amount:F2} on phone {phoneNumber}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        RequestId = requestId,
                        RequesterName = requesterName,
                        RequesterIndex = requesterIndex,
                        Amount = amount,
                        PhoneNumber = phoneNumber,
                        Status = "PendingSupervisor"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "RefundManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund request submission audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogRefundRequestApprovedAsync(int requestId, string approverRole, string approverName, string requesterName, decimal amount, string? comments, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "RefundRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.RefundRequestApproved.ToString(),
                    Description = $"Refund request #{requestId} for {requesterName} (${amount:F2}) approved by {approverRole} {approverName}" +
                                  (!string.IsNullOrWhiteSpace(comments) ? $". Comments: {comments}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApproverRole = approverRole,
                        ApproverName = approverName,
                        Amount = amount,
                        Comments = comments,
                        ApprovedDate = DateTime.UtcNow
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "RefundManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund request approval audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogRefundRequestRejectedAsync(int requestId, string approverRole, string approverName, string requesterName, decimal amount, string? reason, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "RefundRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.RefundRequestRejected.ToString(),
                    Description = $"Refund request #{requestId} for {requesterName} (${amount:F2}) rejected by {approverRole} {approverName}. Reason: {reason}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApproverRole = approverRole,
                        ApproverName = approverName,
                        Amount = amount,
                        RejectionReason = reason,
                        RejectedDate = DateTime.UtcNow,
                        Status = "Rejected"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "RefundManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund request rejection audit for Request {RequestId}", requestId);
            }
        }

        public async Task LogRefundRequestCompletedAsync(int requestId, string approverName, string requesterName, decimal amount, string? paymentReference, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "RefundRequest",
                    EntityId = requestId.ToString(),
                    Action = AuditAction.RefundRequestCompleted.ToString(),
                    Description = $"Refund request #{requestId} for {requesterName} (${amount:F2}) completed by {approverName}" +
                                  (!string.IsNullOrWhiteSpace(paymentReference) ? $". Payment reference: {paymentReference}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        ApproverName = approverName,
                        Amount = amount,
                        PaymentReference = paymentReference,
                        CompletedDate = DateTime.UtcNow,
                        Status = "Completed"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "RefundManagement",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging refund request completion audit for Request {RequestId}", requestId);
            }
        }

        // Call Log Verification Workflow Audit Logs
        public async Task LogCallLogVerificationSubmittedAsync(int verificationId, string requesterName, string requesterIndex, int callCount, decimal totalAmount, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallLogVerification",
                    EntityId = verificationId.ToString(),
                    Action = AuditAction.CallLogVerificationSubmitted.ToString(),
                    Description = $"Call log verification #{verificationId} submitted by {requesterName} (Index: {requesterIndex}) with {callCount} calls totaling ${totalAmount:F2}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        VerificationId = verificationId,
                        RequesterName = requesterName,
                        RequesterIndex = requesterIndex,
                        CallCount = callCount,
                        TotalAmount = totalAmount,
                        Status = "PendingSupervisor"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call log verification submission audit for Verification {VerificationId}", verificationId);
            }
        }

        public async Task LogCallLogVerificationApprovedAsync(int verificationId, string supervisorName, string requesterName, int callCount, decimal totalAmount, string? comments, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallLogVerification",
                    EntityId = verificationId.ToString(),
                    Action = AuditAction.CallLogVerificationApproved.ToString(),
                    Description = $"Call log verification #{verificationId} for {requesterName} ({callCount} calls, ${totalAmount:F2}) approved by Supervisor {supervisorName}" +
                                  (!string.IsNullOrWhiteSpace(comments) ? $". Comments: {comments}" : ""),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        SupervisorName = supervisorName,
                        CallCount = callCount,
                        TotalAmount = totalAmount,
                        Comments = comments,
                        ApprovedDate = DateTime.UtcNow,
                        Status = "Approved"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call log verification approval audit for Verification {VerificationId}", verificationId);
            }
        }

        public async Task LogCallLogVerificationRejectedAsync(int verificationId, string supervisorName, string requesterName, int callCount, decimal totalAmount, string? reason, string performedBy, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = "CallLogVerification",
                    EntityId = verificationId.ToString(),
                    Action = AuditAction.CallLogVerificationRejected.ToString(),
                    Description = $"Call log verification #{verificationId} for {requesterName} ({callCount} calls, ${totalAmount:F2}) rejected by Supervisor {supervisorName}. Reason: {reason}",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        SupervisorName = supervisorName,
                        CallCount = callCount,
                        TotalAmount = totalAmount,
                        RejectionReason = reason,
                        RejectedDate = DateTime.UtcNow,
                        Status = "Rejected"
                    }),
                    PerformedBy = performedBy,
                    PerformedDate = DateTime.UtcNow,
                    IPAddress = ipAddress,
                    Module = "CallLogVerification",
                    IsSuccess = true
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging call log verification rejection audit for Verification {VerificationId}", verificationId);
            }
        }
    }
}
