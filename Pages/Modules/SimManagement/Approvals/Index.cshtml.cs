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

        public int SupervisorPendingCount { get; set; }
        public int IctsPendingCount { get; set; }
        public List<SimRequest> SupervisorRequests { get; set; } = new();
        public List<SimRequest> IctsRequests { get; set; } = new();
        
        // Categorized requests for supervisor view
        public List<SimRequest> SupervisorPendingRequests { get; set; } = new();
        public List<SimRequest> SupervisorProcessedRequests { get; set; } = new();
        public List<SimRequest> SupervisorRejectedRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get counts for badges
            var userEmail = User.Identity?.Name; // This is typically the email for logged-in users
            
            // Get supervisor requests - filter by supervisor email (stored in Supervisor field)
            // Also check SupervisorEmail field for backward compatibility
            SupervisorRequests = await _context.SimRequests
                .Include(s => s.ServiceProvider)
                .Where(s => s.Supervisor == userEmail || s.SupervisorEmail == userEmail)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();
            
            // Categorize supervisor requests
            SupervisorPendingRequests = SupervisorRequests.Where(s => s.Status == RequestStatus.PendingSupervisor).ToList();
            SupervisorProcessedRequests = SupervisorRequests.Where(s => 
                s.Status == RequestStatus.PendingIcts || 
                s.Status == RequestStatus.PendingAdmin || 
                s.Status == RequestStatus.Approved || 
                s.Status == RequestStatus.Completed).ToList();
            SupervisorRejectedRequests = SupervisorRequests.Where(s => s.Status == RequestStatus.Rejected).ToList();
            
            SupervisorPendingCount = SupervisorPendingRequests.Count;

            // Get ICTS requests (for ICTS users)
            if (User.IsInRole("ICTS") || User.IsInRole("Admin"))
            {
                IctsRequests = await _context.SimRequests
                    .Include(s => s.ServiceProvider)
                    .Where(s => s.Status == RequestStatus.PendingIcts)
                    .OrderByDescending(s => s.RequestDate)
                    .ToListAsync();
                    
                IctsPendingCount = IctsRequests.Count;
            }
            
            // Pass data to ViewData for partial views
            ViewData["SupervisorRequests"] = SupervisorRequests;
            ViewData["IctsRequests"] = IctsRequests;

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