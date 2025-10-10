using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface ISimRequestHistoryService
    {
        Task AddHistoryAsync(int simRequestId, string action, string? previousStatus = null, string? newStatus = null, string? comments = null, string? userId = null, string? userName = null, string? ipAddress = null);
        Task<List<SimRequestHistory>> GetHistoryAsync(int simRequestId);
        Task AddStatusChangedHistoryAsync(int simRequestId, string previousStatus, string newStatus, string userId, string userName, string? comments = null, string? ipAddress = null);
        Task AddCommentHistoryAsync(int simRequestId, string comments, string userId, string userName, string? ipAddress = null);
        Task AddSubmissionHistoryAsync(int simRequestId, string userId, string userName, string? ipAddress = null);
        Task AddApprovalHistoryAsync(int simRequestId, string approverType, bool approved, string userId, string userName, string? comments = null, string? ipAddress = null);
        Task AddReversionHistoryAsync(int simRequestId, string reverterType, string userId, string userName, string? comments = null, string? ipAddress = null);
        Task AddIctsActionHistoryAsync(int simRequestId, string ictsAction, string userId, string userName, string? comments = null, string? ipAddress = null);
        Task AddCompletionHistoryAsync(int simRequestId, string userId, string userName, string? comments = null, string? ipAddress = null);
    }

    public class SimRequestHistoryService : ISimRequestHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SimRequestHistoryService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddHistoryAsync(int simRequestId, string action, string? previousStatus = null, string? newStatus = null, string? comments = null, string? userId = null, string? userName = null, string? ipAddress = null)
        {
            var history = new SimRequestHistory
            {
                SimRequestId = simRequestId,
                Action = action,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Comments = comments,
                PerformedBy = userId ?? "System",
                UserName = userName ?? "System",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.SimRequestHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SimRequestHistory>> GetHistoryAsync(int simRequestId)
        {
            return await _context.SimRequestHistories
                .Where(h => h.SimRequestId == simRequestId)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
        }

        public async Task AddStatusChangedHistoryAsync(int simRequestId, string previousStatus, string newStatus, string userId, string userName, string? comments = null, string? ipAddress = null)
        {
            var action = newStatus switch
            {
                nameof(RequestStatus.PendingSupervisor) => HistoryActions.SubmittedToSupervisor,
                nameof(RequestStatus.PendingIcts) when previousStatus == nameof(RequestStatus.PendingSupervisor) => HistoryActions.SupervisorApproved,
                nameof(RequestStatus.Approved) when previousStatus == nameof(RequestStatus.PendingSupervisor) => HistoryActions.SupervisorApproved,
                nameof(RequestStatus.Approved) when previousStatus == nameof(RequestStatus.PendingIcts) => HistoryActions.IctsProcessed,
                nameof(RequestStatus.Approved) when previousStatus == nameof(RequestStatus.PendingAdmin) => HistoryActions.AdminApproved,
                nameof(RequestStatus.Rejected) when previousStatus == nameof(RequestStatus.PendingSupervisor) => HistoryActions.SupervisorRejected,
                nameof(RequestStatus.Rejected) when previousStatus == nameof(RequestStatus.PendingIcts) => HistoryActions.IctsReverted,
                nameof(RequestStatus.Rejected) when previousStatus == nameof(RequestStatus.PendingAdmin) => HistoryActions.AdminRejected,
                nameof(RequestStatus.Completed) => HistoryActions.Completed,
                _ => HistoryActions.StatusChanged
            };

            await AddHistoryAsync(simRequestId, action, previousStatus, newStatus, comments, userId, userName, ipAddress);
        }

        public async Task AddCommentHistoryAsync(int simRequestId, string comments, string userId, string userName, string? ipAddress = null)
        {
            await AddHistoryAsync(simRequestId, HistoryActions.CommentAdded, null, null, comments, userId, userName, ipAddress);
        }

        public async Task AddSubmissionHistoryAsync(int simRequestId, string userId, string userName, string? ipAddress = null)
        {
            await AddHistoryAsync(simRequestId, HistoryActions.SubmittedToSupervisor, nameof(RequestStatus.Draft), nameof(RequestStatus.PendingSupervisor), "Request submitted for approval", userId, userName, ipAddress);
        }

        public async Task AddApprovalHistoryAsync(int simRequestId, string approverType, bool approved, string userId, string userName, string? comments = null, string? ipAddress = null)
        {
            var action = approverType.ToLower() switch
            {
                "supervisor" => approved ? HistoryActions.SupervisorApproved : HistoryActions.SupervisorRejected,
                "admin" => approved ? HistoryActions.AdminApproved : HistoryActions.AdminRejected,
                _ => approved ? HistoryActions.StatusChanged : HistoryActions.StatusChanged
            };

            var previousStatus = approverType.ToLower() == "supervisor" ? nameof(RequestStatus.PendingSupervisor) : nameof(RequestStatus.PendingAdmin);
            var newStatus = approved ? nameof(RequestStatus.Approved) : nameof(RequestStatus.Rejected);
            var defaultComment = approved ? $"Request approved by {approverType}" : $"Request rejected by {approverType}";

            await AddHistoryAsync(simRequestId, action, previousStatus, newStatus, comments ?? defaultComment, userId, userName, ipAddress);
        }

        public async Task AddReversionHistoryAsync(int simRequestId, string reverterType, string userId, string userName, string? comments = null, string? ipAddress = null)
        {
            var action = reverterType.ToLower() switch
            {
                "supervisor" => HistoryActions.SupervisorReverted,
                "icts" => HistoryActions.IctsReverted,
                _ => HistoryActions.StatusChanged
            };
            
            var defaultComment = $"Request reverted by {reverterType}";
            await AddHistoryAsync(simRequestId, action, null, "Draft", comments ?? defaultComment, userId, userName, ipAddress);
        }

        public async Task AddIctsActionHistoryAsync(int simRequestId, string ictsAction, string userId, string userName, string? comments = null, string? ipAddress = null)
        {
            await AddHistoryAsync(simRequestId, ictsAction, null, null, comments, userId, userName, ipAddress);
        }

        public async Task AddCompletionHistoryAsync(int simRequestId, string userId, string userName, string? comments = null, string? ipAddress = null)
        {
            await AddHistoryAsync(simRequestId, HistoryActions.Completed, nameof(RequestStatus.Approved), nameof(RequestStatus.Completed), comments ?? "Request completed successfully", userId, userName, ipAddress);
        }
    }
} 