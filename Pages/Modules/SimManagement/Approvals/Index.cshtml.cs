using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.SimManagement.Approvals
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // All SIM requests for the unified table view
        public List<SimRequest> SimRequests { get; set; } = new();

        // Statistics
        public int TotalRequests { get; set; }
        public int PendingSupervisorCount { get; set; }
        public int PendingIctsCount { get; set; }
        public int PendingAdminCount { get; set; }
        public int PendingServiceProviderCount { get; set; }
        public int PendingCollectionCount { get; set; }
        public int ApprovedCount { get; set; }
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // User role flags
        public bool IsSupervisor { get; set; }
        public bool IsIcts { get; set; }
        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            var userName = User.Identity?.Name;

            // Set role flags
            IsAdmin = User.IsInRole("Admin");
            IsSupervisor = User.IsInRole("Supervisor");
            IsIcts = User.IsInRole("ICTS");

            // Set current page
            CurrentPage = page < 1 ? 1 : page;

            // Build query based on user role
            IQueryable<SimRequest> query = _context.SimRequests.Include(s => s.ServiceProvider);

            if (IsAdmin)
            {
                // Admin sees all requests
                query = query.OrderByDescending(r => r.RequestDate);
            }
            else if (IsSupervisor && IsIcts)
            {
                // User has both roles - show requests assigned to them as supervisor + ICTS pending
                query = query.Where(r => r.Supervisor == userName || r.SupervisorEmail == userName || r.Status == RequestStatus.PendingIcts)
                            .OrderByDescending(r => r.RequestDate);
            }
            else if (IsSupervisor)
            {
                // Supervisor sees only their assigned requests
                query = query.Where(r => r.Supervisor == userName || r.SupervisorEmail == userName)
                            .OrderByDescending(r => r.RequestDate);
            }
            else if (IsIcts)
            {
                // ICTS sees only pending ICTS requests
                query = query.Where(r => r.Status == RequestStatus.PendingIcts)
                            .OrderByDescending(r => r.RequestDate);
            }
            else
            {
                // Other users see nothing (or could show their own requests if needed)
                query = query.Where(r => false).OrderByDescending(r => r.RequestDate);
            }

            // Get total count before pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination
            SimRequests = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Calculate statistics from all accessible requests (not just current page)
            var allAccessibleRequests = await _context.SimRequests
                .Where(r => IsAdmin ||
                           (IsSupervisor && (r.Supervisor == userName || r.SupervisorEmail == userName)) ||
                           (IsIcts && r.Status == RequestStatus.PendingIcts))
                .ToListAsync();

            TotalRequests = allAccessibleRequests.Count;
            PendingSupervisorCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.PendingSupervisor);
            PendingIctsCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.PendingIcts);
            PendingAdminCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.PendingAdmin);
            PendingServiceProviderCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.PendingServiceProvider);
            PendingCollectionCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.PendingSIMCollection);
            ApprovedCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.Approved);
            CompletedCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.Completed);
            RejectedCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.Rejected);
            CancelledCount = allAccessibleRequests.Count(r => r.Status == RequestStatus.Cancelled);

            return Page();
        }

        public async Task<IActionResult> OnPostApproveSupervisorAsync(int requestId, string mobileService, string mobileServiceAllowance, 
            string handsetAllowance, string supervisorNotes, string action)
        {
            var request = await _context.SimRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            // Update request with supervisor approval details
            request.MobileService = mobileService;
            request.MobileServiceAllowance = mobileServiceAllowance;
            request.HandsetAllowance = handsetAllowance;
            request.SupervisorNotes = supervisorNotes;
            request.SupervisorName = User.Identity?.Name;
            request.SupervisorEmail = User.Identity?.Name;
            request.SupervisorApprovalDate = DateTime.UtcNow;
            
            if (action == "approve")
            {
                request.Status = RequestStatus.PendingIcts;
                request.SubmittedToSupervisor = true;
                
                // Add to history
                _context.SimRequestHistories.Add(new SimRequestHistory
                {
                    SimRequestId = request.Id,
                    Action = "Approved by Supervisor",
                    PerformedBy = User.Identity?.Name ?? "Unknown",
                    UserName = User.Identity?.Name ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    Comments = supervisorNotes,
                    NewStatus = "PendingIcts",
                    PreviousStatus = "PendingSupervisor"
                });
            }

            await _context.SaveChangesAsync();
            
            // Redirect back to the approvals page with supervisor tab active
            return RedirectToPage("/Modules/SimManagement/Approvals/Index", new { tab = "supervisor" });
        }

        public async Task<IActionResult> OnPostRejectSupervisorAsync(int requestId, string rejectionReason)
        {
            var request = await _context.SimRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            // Update request with rejection details
            request.Status = RequestStatus.Rejected;
            request.SupervisorNotes = rejectionReason;
            request.SupervisorName = User.Identity?.Name;
            request.SupervisorEmail = User.Identity?.Name;
            request.SupervisorApprovalDate = DateTime.UtcNow;
            
            // Add to history
            _context.SimRequestHistories.Add(new SimRequestHistory
            {
                SimRequestId = request.Id,
                Action = "Rejected by Supervisor",
                PerformedBy = User.Identity?.Name ?? "Unknown",
                UserName = User.Identity?.Name ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Comments = rejectionReason,
                NewStatus = "Rejected",
                PreviousStatus = "PendingSupervisor"
            });

            await _context.SaveChangesAsync();
            
            // Redirect back to the approvals page with supervisor tab active
            return RedirectToPage("/Modules/SimManagement/Approvals/Index", new { tab = "supervisor" });
        }

        public async Task<IActionResult> OnPostProcessIctsAsync(int requestId, string simSerialNo, string serviceRequestNo, 
            string phoneNumber, string lineType, string simPuk, string pin, string processingNotes)
        {
            var request = await _context.SimRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            // Update request with ICTS processing details
            request.SimSerialNo = simSerialNo;
            request.ServiceRequestNo = serviceRequestNo;
            request.LineType = lineType;
            request.SimPuk = simPuk;
            request.ProcessingNotes = processingNotes;
            request.ProcessedBy = User.Identity?.Name;
            request.ProcessedDate = DateTime.UtcNow;
            request.Status = RequestStatus.Completed;
            
            // Add to history
            _context.SimRequestHistories.Add(new SimRequestHistory
            {
                SimRequestId = request.Id,
                Action = "Processed by ICTS",
                PerformedBy = User.Identity?.Name ?? "Unknown",
                UserName = User.Identity?.Name ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Comments = processingNotes,
                NewStatus = "Completed",
                PreviousStatus = "PendingIcts"
            });

            await _context.SaveChangesAsync();
            
            // Redirect back to the approvals page with ICTS tab active
            return RedirectToPage("/Modules/SimManagement/Approvals/Index", new { tab = "icts" });
        }

        public async Task<IActionResult> OnPostRevertIctsAsync(int requestId, string revertReason)
        {
            var request = await _context.SimRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }

            // Revert request back to draft/requestor
            request.Status = RequestStatus.Draft;
            request.ProcessingNotes = $"Reverted by ICTS: {revertReason}";
            
            // Add to history
            _context.SimRequestHistories.Add(new SimRequestHistory
            {
                SimRequestId = request.Id,
                Action = "Reverted to Requestor by ICTS",
                PerformedBy = User.Identity?.Name ?? "Unknown",
                UserName = User.Identity?.Name ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Comments = revertReason,
                NewStatus = "Draft",
                PreviousStatus = "PendingIcts"
            });

            await _context.SaveChangesAsync();
            
            // Redirect back to the approvals page with ICTS tab active
            return RedirectToPage("/Modules/SimManagement/Approvals/Index", new { tab = "icts" });
        }
    }
}