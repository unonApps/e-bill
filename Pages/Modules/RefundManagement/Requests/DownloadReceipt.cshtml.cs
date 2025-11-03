using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;

namespace TAB.Web.Pages.Modules.RefundManagement.Requests
{
    [Authorize]
    public class DownloadReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DownloadReceiptModel> _logger;

        public DownloadReceiptModel(
            ApplicationDbContext context,
            ILogger<DownloadReceiptModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var request = await _context.RefundRequests
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.PurchaseReceiptData,
                        r.PurchaseReceiptFileName,
                        r.PurchaseReceiptContentType
                    })
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    _logger.LogWarning("Refund request {RequestId} not found", id);
                    return NotFound("Refund request not found");
                }

                if (request.PurchaseReceiptData == null || request.PurchaseReceiptData.Length == 0)
                {
                    _logger.LogWarning("No receipt data found for refund request {RequestId}", id);
                    return NotFound("No receipt file found for this request");
                }

                var fileName = request.PurchaseReceiptFileName ?? $"receipt_{id}.pdf";
                var contentType = request.PurchaseReceiptContentType ?? "application/pdf";

                _logger.LogInformation("Serving receipt file '{FileName}' ({Size} bytes) for refund request {RequestId}",
                    fileName, request.PurchaseReceiptData.Length, id);

                // Return the file
                return File(request.PurchaseReceiptData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt for refund request {RequestId}", id);
                return StatusCode(500, "An error occurred while retrieving the receipt");
            }
        }
    }
}
