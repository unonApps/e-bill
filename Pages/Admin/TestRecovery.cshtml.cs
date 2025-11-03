using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TestRecoveryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICallLogRecoveryService _recoveryService;
        private readonly ILogger<TestRecoveryModel> _logger;

        public TestRecoveryModel(
            ApplicationDbContext context,
            ICallLogRecoveryService recoveryService,
            ILogger<TestRecoveryModel> logger)
        {
            _context = context;
            _recoveryService = recoveryService;
            _logger = logger;
        }

        public string? Message { get; set; }
        public bool Success { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var log = new StringBuilder();
            try
            {
                log.AppendLine("===== TEST RECOVERY JOB START =====");
                log.AppendLine($"Time: {DateTime.UtcNow}");
                log.AppendLine();

                // Find active batches (including Published batches)
                var batches = await _context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified ||
                               b.BatchStatus == BatchStatus.Verified ||
                               b.BatchStatus == BatchStatus.Published)
                    .ToListAsync();

                log.AppendLine($"Found {batches.Count} active batches");
                log.AppendLine();

                if (batches.Count == 0)
                {
                    Message = log.ToString() + "\nNo active batches found.";
                    Success = true;
                    return Page();
                }

                // Test the new method
                log.AppendLine("--- Testing ProcessVerifiedButNotSubmittedAsync ---");
                foreach (var batch in batches)
                {
                    log.AppendLine($"\nBatch: {batch.BatchName} ({batch.Id})");

                    try
                    {
                        var result = await _recoveryService.ProcessVerifiedButNotSubmittedAsync(batch.Id);

                        log.AppendLine($"  Success: {result.Success}");
                        log.AppendLine($"  Records Processed: {result.RecordsProcessed}");
                        log.AppendLine($"  Amount Recovered: ${result.AmountRecovered:N2}");
                        log.AppendLine($"  Message: {result.Message}");

                        if (result.Errors.Any())
                        {
                            log.AppendLine("  Errors:");
                            foreach (var error in result.Errors)
                            {
                                log.AppendLine($"    - {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($"  ERROR: {ex.Message}");
                        log.AppendLine($"  Stack: {ex.StackTrace}");
                    }
                }

                log.AppendLine();
                log.AppendLine("===== TEST RECOVERY JOB COMPLETE =====");

                Message = log.ToString();
                Success = true;
            }
            catch (Exception ex)
            {
                log.AppendLine();
                log.AppendLine("===== FATAL ERROR =====");
                log.AppendLine($"Error: {ex.Message}");
                log.AppendLine($"Stack Trace: {ex.StackTrace}");

                Message = log.ToString();
                Success = false;
            }

            return Page();
        }
    }
}
