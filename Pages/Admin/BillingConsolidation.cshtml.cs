using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class BillingConsolidationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserPhoneService _phoneService;

        public BillingConsolidationModel(ApplicationDbContext context, IUserPhoneService phoneService)
        {
            _context = context;
            _phoneService = phoneService;
        }

        // Filter parameters
        [BindProperty(SupportsGet = true)]
        public string? IndexNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BillType { get; set; } // PSTN, PrivateWire, Safaricom, Airtel

        // Results
        public List<ConsolidatedBill> Bills { get; set; } = new();
        public BillingSummary Summary { get; set; } = new();
        public EbillUser? SelectedUser { get; set; }
        public Dictionary<string, List<UserPhone>> UserPhonesMap { get; set; } = new();

        public class ConsolidatedBill
        {
            public string BillType { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string PhoneType { get; set; } = string.Empty;
            public DateTime BillDate { get; set; }
            public decimal Amount { get; set; }
            public string IndexNumber { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? Organization { get; set; }
            public string? Office { get; set; }
            public int BillId { get; set; }
        }

        public class BillingSummary
        {
            public decimal TotalAmount { get; set; }
            public int TotalBills { get; set; }
            public Dictionary<string, decimal> AmountByType { get; set; } = new();
            public Dictionary<string, int> CountByType { get; set; } = new();
            public Dictionary<string, decimal> AmountByPhone { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            // Set default date range if not provided
            if (!EndDate.HasValue)
                EndDate = DateTime.Today;
            if (!StartDate.HasValue)
                StartDate = EndDate.Value.AddMonths(-1);

            await LoadBillsAsync();
        }

        private async Task LoadBillsAsync()
        {
            var bills = new List<ConsolidatedBill>();

            // Load user if specified
            if (!string.IsNullOrEmpty(IndexNumber))
            {
                SelectedUser = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .Include(u => u.OfficeEntity)
                    .FirstOrDefaultAsync(u => u.IndexNumber == IndexNumber);
            }

            // Load user phones for mapping
            UserPhonesMap = await _phoneService.GetAllUserPhonesAsync();

            // Get PSTN bills
            if (string.IsNullOrEmpty(BillType) || BillType == "PSTN")
            {
                var pstnQuery = _context.PSTNs.AsQueryable();

                if (StartDate.HasValue)
                    pstnQuery = pstnQuery.Where(p => p.CallDate >= StartDate.Value);
                if (EndDate.HasValue)
                    pstnQuery = pstnQuery.Where(p => p.CallDate <= EndDate.Value);

                var pstnBills = await pstnQuery.ToListAsync();

                foreach (var bill in pstnBills)
                {
                    // PSTN uses Extension as the calling number
                    var userIndex = await _phoneService.GetUserByPhoneAsync(bill.Extension ?? "", bill.CallDate);
                    if (!string.IsNullOrEmpty(IndexNumber) && userIndex != IndexNumber)
                        continue;

                    if (userIndex != null)
                    {
                        var user = await _context.EbillUsers
                            .Include(u => u.OrganizationEntity)
                            .Include(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(u => u.IndexNumber == userIndex);

                        if (user != null)
                        {
                            var phoneInfo = UserPhonesMap.ContainsKey(userIndex)
                                ? UserPhonesMap[userIndex].FirstOrDefault(p => p.PhoneNumber == bill.Extension)
                                : null;

                            bills.Add(new ConsolidatedBill
                            {
                                BillType = "PSTN",
                                PhoneNumber = bill.Extension ?? "",
                                PhoneType = phoneInfo?.PhoneType ?? "Unknown",
                                BillDate = bill.CallDate ?? DateTime.MinValue,
                                Amount = bill.TotalCost,
                                IndexNumber = userIndex,
                                UserName = user.FullName,
                                Organization = user.OrganizationEntity?.Name,
                                Office = user.OfficeEntity?.Name,
                                BillId = bill.Id
                            });
                        }
                    }
                }
            }

            // Get PrivateWire bills
            if (string.IsNullOrEmpty(BillType) || BillType == "PrivateWire")
            {
                var pwQuery = _context.PrivateWires.AsQueryable();

                if (StartDate.HasValue)
                    pwQuery = pwQuery.Where(p => p.CallDate >= StartDate.Value);
                if (EndDate.HasValue)
                    pwQuery = pwQuery.Where(p => p.CallDate <= EndDate.Value);

                var pwBills = await pwQuery.ToListAsync();

                foreach (var bill in pwBills)
                {
                    // PrivateWire uses Extension as the service number
                    var userIndex = await _phoneService.GetUserByPhoneAsync(bill.Extension ?? "", bill.CallDate);
                    if (!string.IsNullOrEmpty(IndexNumber) && userIndex != IndexNumber)
                        continue;

                    if (userIndex != null)
                    {
                        var user = await _context.EbillUsers
                            .Include(u => u.OrganizationEntity)
                            .Include(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(u => u.IndexNumber == userIndex);

                        if (user != null)
                        {
                            var phoneInfo = UserPhonesMap.ContainsKey(userIndex)
                                ? UserPhonesMap[userIndex].FirstOrDefault(p => p.PhoneNumber == bill.Extension)
                                : null;

                            bills.Add(new ConsolidatedBill
                            {
                                BillType = "PrivateWire",
                                PhoneNumber = bill.Extension ?? "",
                                PhoneType = phoneInfo?.PhoneType ?? "Unknown",
                                BillDate = bill.CallDate ?? DateTime.MinValue,
                                Amount = bill.TotalCostKSH,
                                IndexNumber = userIndex,
                                UserName = user.FullName,
                                Organization = user.OrganizationEntity?.Name,
                                Office = user.OfficeEntity?.Name,
                                BillId = bill.Id
                            });
                        }
                    }
                }
            }

            // Get Safaricom bills
            if (string.IsNullOrEmpty(BillType) || BillType == "Safaricom")
            {
                var safQuery = _context.Safaricoms.AsQueryable();

                if (StartDate.HasValue)
                    safQuery = safQuery.Where(s => s.CallDate >= StartDate.Value);
                if (EndDate.HasValue)
                    safQuery = safQuery.Where(s => s.CallDate <= EndDate.Value);

                var safBills = await safQuery.ToListAsync();

                foreach (var bill in safBills)
                {
                    // Safaricom uses Ext as the calling number
                    var userIndex = await _phoneService.GetUserByPhoneAsync(bill.Ext ?? "", bill.CallDate);
                    if (!string.IsNullOrEmpty(IndexNumber) && userIndex != IndexNumber)
                        continue;

                    if (userIndex != null)
                    {
                        var user = await _context.EbillUsers
                            .Include(u => u.OrganizationEntity)
                            .Include(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(u => u.IndexNumber == userIndex);

                        if (user != null)
                        {
                            var phoneInfo = UserPhonesMap.ContainsKey(userIndex)
                                ? UserPhonesMap[userIndex].FirstOrDefault(p => p.PhoneNumber == bill.Ext)
                                : null;

                            bills.Add(new ConsolidatedBill
                            {
                                BillType = "Safaricom",
                                PhoneNumber = bill.Ext ?? "",
                                PhoneType = phoneInfo?.PhoneType ?? "Mobile",
                                BillDate = bill.CallDate ?? DateTime.MinValue,
                                Amount = bill.Cost ?? 0,
                                IndexNumber = userIndex,
                                UserName = user.FullName,
                                Organization = user.OrganizationEntity?.Name,
                                Office = user.OfficeEntity?.Name,
                                BillId = bill.Id
                            });
                        }
                    }
                }
            }

            // Sort bills by date descending
            Bills = bills.OrderByDescending(b => b.BillDate).ToList();

            // Calculate summary
            Summary.TotalAmount = Bills.Sum(b => b.Amount);
            Summary.TotalBills = Bills.Count;

            Summary.AmountByType = Bills
                .GroupBy(b => b.BillType)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Amount));

            Summary.CountByType = Bills
                .GroupBy(b => b.BillType)
                .ToDictionary(g => g.Key, g => g.Count());

            Summary.AmountByPhone = Bills
                .GroupBy(b => b.PhoneNumber)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.Amount));
        }
    }
}