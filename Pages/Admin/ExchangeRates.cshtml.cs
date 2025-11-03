using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ExchangeRatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExchangeRatesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<ExchangeRate> ExchangeRates { get; set; } = new();
        public List<ExchangeRate> FilteredExchangeRates { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        // Filter properties
        public int? FilterYear { get; set; }
        public int? FilterMonth { get; set; }
        public string? SearchTerm { get; set; }

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public async Task OnGetAsync(int? filterYear, int? filterMonth, string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            FilterYear = filterYear;
            FilterMonth = filterMonth;
            SearchTerm = searchTerm;
            CurrentPage = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.ExchangeRates.AsQueryable();

            // Apply filters
            if (filterYear.HasValue)
            {
                query = query.Where(r => r.Year == filterYear.Value);
            }

            if (filterMonth.HasValue)
            {
                query = query.Where(r => r.Month == filterMonth.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.CreatedBy.Contains(searchTerm) ||
                                        r.UpdatedBy != null && r.UpdatedBy.Contains(searchTerm));
            }

            // Get total count for pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Ensure current page is valid
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }

            // Apply pagination
            FilteredExchangeRates = await query
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Get all for year dropdown
            ExchangeRates = await _context.ExchangeRates
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(int month, int year, decimal rate)
        {
            if (month < 1 || month > 12 || year < 2000 || rate <= 0)
            {
                StatusMessage = "Invalid input values. Please check month, year, and rate.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Check if rate already exists for this period
            var exists = await _context.ExchangeRates
                .AnyAsync(r => r.Month == month && r.Year == year);

            if (exists)
            {
                StatusMessage = $"Exchange rate for {new DateTime(year, month, 1):MMMM yyyy} already exists. Use update instead.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            var exchangeRate = new ExchangeRate
            {
                Month = month,
                Year = year,
                Rate = rate,
                CreatedBy = user?.Email ?? User.Identity?.Name ?? "System",
                CreatedDate = DateTime.UtcNow
            };

            _context.ExchangeRates.Add(exchangeRate);
            await _context.SaveChangesAsync();

            StatusMessage = $"Exchange rate for {exchangeRate.PeriodDisplay} created successfully: 1 USD = {rate:N4} KES";
            StatusMessageClass = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id, decimal rate)
        {
            var exchangeRate = await _context.ExchangeRates.FindAsync(id);

            if (exchangeRate == null)
            {
                StatusMessage = "Exchange rate not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            if (rate <= 0)
            {
                StatusMessage = "Invalid rate value. Rate must be greater than zero.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            exchangeRate.Rate = rate;
            exchangeRate.UpdatedBy = user?.Email ?? User.Identity?.Name ?? "System";
            exchangeRate.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = $"Exchange rate for {exchangeRate.PeriodDisplay} updated successfully: 1 USD = {rate:N4} KES";
            StatusMessageClass = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var exchangeRate = await _context.ExchangeRates.FindAsync(id);

            if (exchangeRate == null)
            {
                StatusMessage = "Exchange rate not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Check if this rate is being used by any telecom records
            var hasAirtelRecords = await _context.Airtels
                .AnyAsync(a => a.CallMonth == exchangeRate.Month && a.CallYear == exchangeRate.Year && a.AmountUSD.HasValue);

            var hasSafaricomRecords = await _context.Safaricoms
                .AnyAsync(s => s.CallMonth == exchangeRate.Month && s.CallYear == exchangeRate.Year && s.AmountUSD.HasValue);

            var hasPSTNRecords = await _context.PSTNs
                .AnyAsync(p => p.CallMonth == exchangeRate.Month && p.CallYear == exchangeRate.Year && p.AmountUSD.HasValue);

            if (hasAirtelRecords || hasSafaricomRecords || hasPSTNRecords)
            {
                StatusMessage = $"Cannot delete exchange rate for {exchangeRate.PeriodDisplay}. It is being used by existing call records.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            _context.ExchangeRates.Remove(exchangeRate);
            await _context.SaveChangesAsync();

            StatusMessage = $"Exchange rate for {exchangeRate.PeriodDisplay} deleted successfully.";
            StatusMessageClass = "success";

            return RedirectToPage();
        }
    }
}
