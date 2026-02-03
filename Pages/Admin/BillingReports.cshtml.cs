using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class BillingReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BillingReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Summary totals for the last 12 months
        public int TotalRecords { get; set; }
        public decimal TotalKES { get; set; }
        public decimal TotalUSD { get; set; }
        public int MonthsDisplayed { get; set; }

        // Provider-level totals for summary cards
        public decimal SafaricomTotalKES { get; set; }
        public decimal AirtelTotalKES { get; set; }
        public decimal PSTNTotalKES { get; set; }
        public decimal PrivateWireTotalUSD { get; set; }
        public int SafaricomRecords { get; set; }
        public int AirtelRecords { get; set; }
        public int PSTNRecords { get; set; }
        public int PrivateWireRecords { get; set; }

        // Monthly data
        public List<MonthlyBillingSummary> MonthlySummaries { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadBillingDataAsync();
        }

        private async Task LoadBillingDataAsync()
        {
            // Calculate date range for last 12 months
            var now = DateTime.UtcNow;
            var startDate = now.AddMonths(-11); // 12 months including current
            var startYear = startDate.Year;
            var startMonth = startDate.Month;

            // Query raw data grouped by month and provider
            var rawData = await _context.CallRecords
                .Where(c => (c.CallYear > startYear) ||
                            (c.CallYear == startYear && c.CallMonth >= startMonth))
                .GroupBy(c => new { c.CallYear, c.CallMonth, c.SourceSystem })
                .Select(g => new
                {
                    Year = g.Key.CallYear,
                    Month = g.Key.CallMonth,
                    Provider = g.Key.SourceSystem ?? "Unknown",
                    RecordCount = g.Count(),
                    TotalKES = g.Sum(c => c.CallCostKSHS),
                    TotalUSD = g.Sum(c => c.CallCostUSD)
                })
                .ToListAsync();

            // Group by month and create summaries
            var monthlyGroups = rawData
                .GroupBy(r => new { r.Year, r.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .ToList();

            MonthlySummaries = monthlyGroups.Select(g => new MonthlyBillingSummary
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                TotalRecords = g.Sum(p => p.RecordCount),
                // KES for Safaricom, Airtel, and PSTN
                TotalKES = g.Where(p => p.Provider == "Safaricom" || p.Provider == "Airtel" || p.Provider == "PSTN")
                            .Sum(p => p.TotalKES),
                // USD only for PrivateWire
                TotalUSD = g.Where(p => p.Provider == "PrivateWire")
                            .Sum(p => p.TotalUSD),
                Providers = g.Select(p => new ProviderSummary
                {
                    Provider = p.Provider,
                    RecordCount = p.RecordCount,
                    // KES for Safaricom/Airtel/PSTN
                    AmountKES = (p.Provider == "Safaricom" || p.Provider == "Airtel" || p.Provider == "PSTN") ? p.TotalKES : 0,
                    // USD only for PrivateWire
                    AmountUSD = (p.Provider == "PrivateWire") ? p.TotalUSD : 0
                }).OrderByDescending(p => p.RecordCount).ToList()
            }).ToList();

            // Calculate totals - KES for Safaricom/Airtel/PSTN, USD for PrivateWire
            TotalRecords = MonthlySummaries.Sum(m => m.TotalRecords);
            TotalKES = MonthlySummaries.Sum(m => m.TotalKES);
            TotalUSD = MonthlySummaries.Sum(m => m.TotalUSD);
            MonthsDisplayed = MonthlySummaries.Count;

            // Calculate provider-level totals for summary cards
            var allProviders = MonthlySummaries.SelectMany(m => m.Providers).ToList();
            SafaricomTotalKES = allProviders.Where(p => p.Provider == "Safaricom").Sum(p => p.AmountKES);
            AirtelTotalKES = allProviders.Where(p => p.Provider == "Airtel").Sum(p => p.AmountKES);
            PSTNTotalKES = allProviders.Where(p => p.Provider == "PSTN").Sum(p => p.AmountKES);
            PrivateWireTotalUSD = allProviders.Where(p => p.Provider == "PrivateWire").Sum(p => p.AmountUSD);
            SafaricomRecords = allProviders.Where(p => p.Provider == "Safaricom").Sum(p => p.RecordCount);
            AirtelRecords = allProviders.Where(p => p.Provider == "Airtel").Sum(p => p.RecordCount);
            PSTNRecords = allProviders.Where(p => p.Provider == "PSTN").Sum(p => p.RecordCount);
            PrivateWireRecords = allProviders.Where(p => p.Provider == "PrivateWire").Sum(p => p.RecordCount);
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            await LoadBillingDataAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Month,Year,Provider,Records,Amount (KES),Amount (USD)");

            foreach (var month in MonthlySummaries)
            {
                foreach (var provider in month.Providers)
                {
                    // Show appropriate currency based on provider type
                    // KES for Safaricom/Airtel/PSTN, USD only for PrivateWire
                    var kesValue = (provider.Provider == "Safaricom" || provider.Provider == "Airtel" || provider.Provider == "PSTN")
                        ? provider.AmountKES.ToString("F2") : "0.00";
                    var usdValue = (provider.Provider == "PrivateWire")
                        ? provider.AmountUSD.ToString("F2") : "0.00";
                    csv.AppendLine($"{month.MonthName},{month.Year},{provider.Provider},{provider.RecordCount},{kesValue},{usdValue}");
                }
            }

            // Add totals row
            csv.AppendLine($"TOTAL,,,{TotalRecords},{TotalKES:F2},{TotalUSD:F2}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"BillingReport_{DateTime.UtcNow:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            await LoadBillingDataAsync();

            using var workbook = new XLWorkbook();

            // Sheet 1: Monthly Summary
            var summarySheet = workbook.Worksheets.Add("Monthly Summary");
            summarySheet.Cell(1, 1).Value = "Month";
            summarySheet.Cell(1, 2).Value = "Records";
            summarySheet.Cell(1, 3).Value = "Amount (KES)";
            summarySheet.Cell(1, 4).Value = "Amount (USD)";

            // Style header row
            var headerRange = summarySheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var month in MonthlySummaries)
            {
                summarySheet.Cell(row, 1).Value = month.MonthName;
                summarySheet.Cell(row, 2).Value = month.TotalRecords;
                summarySheet.Cell(row, 3).Value = month.TotalKES;
                summarySheet.Cell(row, 4).Value = month.TotalUSD;
                row++;
            }

            // Add totals row
            summarySheet.Cell(row, 1).Value = "TOTAL";
            summarySheet.Cell(row, 2).Value = TotalRecords;
            summarySheet.Cell(row, 3).Value = TotalKES;
            summarySheet.Cell(row, 4).Value = TotalUSD;
            summarySheet.Range(row, 1, row, 4).Style.Font.Bold = true;
            summarySheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Format currency columns
            summarySheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
            summarySheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
            summarySheet.Columns().AdjustToContents();

            // Sheet 2: Provider Details
            var detailSheet = workbook.Worksheets.Add("Provider Details");
            detailSheet.Cell(1, 1).Value = "Month";
            detailSheet.Cell(1, 2).Value = "Provider";
            detailSheet.Cell(1, 3).Value = "Records";
            detailSheet.Cell(1, 4).Value = "Amount (KES)";
            detailSheet.Cell(1, 5).Value = "Amount (USD)";

            var detailHeader = detailSheet.Range(1, 1, 1, 5);
            detailHeader.Style.Font.Bold = true;
            detailHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

            row = 2;
            foreach (var month in MonthlySummaries)
            {
                foreach (var provider in month.Providers)
                {
                    detailSheet.Cell(row, 1).Value = month.MonthName;
                    detailSheet.Cell(row, 2).Value = provider.Provider;
                    detailSheet.Cell(row, 3).Value = provider.RecordCount;
                    // Show appropriate currency based on provider type
                    // KES for Safaricom/Airtel/PSTN, USD only for PrivateWire
                    detailSheet.Cell(row, 4).Value = (provider.Provider == "Safaricom" || provider.Provider == "Airtel" || provider.Provider == "PSTN")
                        ? provider.AmountKES : 0;
                    detailSheet.Cell(row, 5).Value = (provider.Provider == "PrivateWire")
                        ? provider.AmountUSD : 0;
                    row++;
                }
            }

            detailSheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
            detailSheet.Column(5).Style.NumberFormat.Format = "#,##0.00";
            detailSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var fileName = $"BillingReport_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    public class MonthlyBillingSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public decimal TotalKES { get; set; }
        public decimal TotalUSD { get; set; }
        public List<ProviderSummary> Providers { get; set; } = new();
    }

    public class ProviderSummary
    {
        public string Provider { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public decimal AmountKES { get; set; }
        public decimal AmountUSD { get; set; }
    }
}
