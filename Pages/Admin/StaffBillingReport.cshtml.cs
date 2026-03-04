using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin,Agency Focal Point")]
    public class StaffBillingReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffBillingReportModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterOrganization { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterOffice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "FullName";

        [BindProperty(SupportsGet = true)]
        public string SortDir { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        // KPI totals (computed from ALL filtered rows, before pagination)
        public int TotalStaffCount { get; set; }
        public int TotalCallCount { get; set; }
        public decimal TotalPersonalCostKES { get; set; }
        public decimal TotalOfficialCostKES { get; set; }
        public decimal TotalRecoveredCostKES { get; set; }

        // Pagination
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Dropdown data
        public List<Organization> Organizations { get; set; } = new();
        public List<Office> Offices { get; set; } = new();
        public bool IsOrgLocked { get; set; }

        public List<StaffBillingRow> StaffRows { get; set; } = new();

        public async Task OnGetAsync()
        {
            FilterMonth ??= DateTime.UtcNow.Month;
            FilterYear ??= DateTime.UtcNow.Year;
            await LoadDropdownsAsync();
            await LoadStaffBillingDataAsync();
        }

        private async Task LoadDropdownsAsync()
        {
            var scopedOrgId = await FocalPointHelper.GetScopedOrgIdAsync(User, _userManager);

            if (scopedOrgId.HasValue)
            {
                IsOrgLocked = true;
                FilterOrganization = scopedOrgId.Value;
                Organizations = await _context.Organizations
                    .Where(o => o.Id == scopedOrgId.Value).ToListAsync();
            }
            else
            {
                Organizations = await _context.Organizations
                    .OrderBy(o => o.Code).ThenBy(o => o.Name).ToListAsync();
            }

            if (FilterOrganization.HasValue && FilterOrganization > 0)
            {
                Offices = await _context.Offices
                    .Where(o => o.OrganizationId == FilterOrganization.Value)
                    .OrderBy(o => o.Name).ToListAsync();
            }
        }

        private async Task LoadStaffBillingDataAsync(bool applyPagination = true)
        {
            FilterMonth ??= DateTime.UtcNow.Month;
            FilterYear ??= DateTime.UtcNow.Year;

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ebill.sp_StaffBillingReport";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                var p = cmd.Parameters;
                p.Add(new SqlParameter("@Year", FilterYear ?? DateTime.UtcNow.Year));
                p.Add(new SqlParameter("@Month", FilterMonth ?? 0));
                p.Add(new SqlParameter("@OrgId", (object?)FilterOrganization ?? DBNull.Value));
                p.Add(new SqlParameter("@OfficeId", (object?)FilterOffice ?? DBNull.Value));
                p.Add(new SqlParameter("@SortBy", SortBy ?? "FullName"));
                p.Add(new SqlParameter("@SortDir", SortDir ?? "asc"));
                p.Add(new SqlParameter("@PageNumber", applyPagination ? PageNumber : 1));
                p.Add(new SqlParameter("@PageSize", applyPagination ? PageSize : 0));

                using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: KPI totals
                if (await reader.ReadAsync())
                {
                    TotalStaffCount = reader.GetInt32(reader.GetOrdinal("TotalStaffCount"));
                    TotalCallCount = reader.GetInt32(reader.GetOrdinal("TotalCallCount"));
                    TotalPersonalCostKES = reader.GetDecimal(reader.GetOrdinal("TotalPersonalCostKES"));
                    TotalOfficialCostKES = reader.GetDecimal(reader.GetOrdinal("TotalOfficialCostKES"));
                    TotalRecoveredCostKES = reader.GetDecimal(reader.GetOrdinal("TotalRecoveredCostKES"));
                }

                // Result set 2: Staff rows
                await reader.NextResultAsync();
                StaffRows = new List<StaffBillingRow>();
                while (await reader.ReadAsync())
                {
                    if (TotalCount == 0 && applyPagination)
                        TotalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                    StaffRows.Add(new StaffBillingRow
                    {
                        IndexNumber = reader.GetString(reader.GetOrdinal("IndexNumber")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        OrganizationName = reader.GetString(reader.GetOrdinal("OrganizationName")),
                        OfficeName = reader.GetString(reader.GetOrdinal("OfficeName")),
                        PersonalCallCount = reader.GetInt32(reader.GetOrdinal("PersonalCallCount")),
                        PersonalCallCostKES = reader.GetDecimal(reader.GetOrdinal("PersonalCallCostKES")),
                        PersonalCallCostUSD = reader.GetDecimal(reader.GetOrdinal("PersonalCallCostUSD")),
                        OfficialCallCount = reader.GetInt32(reader.GetOrdinal("OfficialCallCount")),
                        OfficialCallCostKES = reader.GetDecimal(reader.GetOrdinal("OfficialCallCostKES")),
                        OfficialCallCostUSD = reader.GetDecimal(reader.GetOrdinal("OfficialCallCostUSD")),
                        RecoveredCallCount = reader.GetInt32(reader.GetOrdinal("RecoveredCallCount")),
                        RecoveredCallCostKES = reader.GetDecimal(reader.GetOrdinal("RecoveredCallCostKES")),
                        TotalCallCount = reader.GetInt32(reader.GetOrdinal("TotalCallCount")),
                        TotalCostKES = reader.GetDecimal(reader.GetOrdinal("TotalCostKES")),
                        TotalCostUSD = reader.GetDecimal(reader.GetOrdinal("TotalCostUSD"))
                    });
                }

                if (applyPagination)
                {
                    TotalPages = PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;
                    if (PageNumber < 1) PageNumber = 1;
                    if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }
        }

        public async Task<IActionResult> OnGetOfficesAsync(int orgId)
        {
            var offices = await _context.Offices
                .Where(o => o.OrganizationId == orgId)
                .OrderBy(o => o.Name)
                .Select(o => new { value = o.Id.ToString(), text = o.Name })
                .ToListAsync();
            return new JsonResult(offices);
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            FilterMonth ??= DateTime.UtcNow.Month;
            FilterYear ??= DateTime.UtcNow.Year;
            await LoadDropdownsAsync();
            await LoadStaffBillingDataAsync(applyPagination: false);

            var csv = new StringBuilder();
            csv.AppendLine("Index No,Name,Organization,Office,Personal Calls,Personal (KES),Personal (USD),Official Calls,Official (KES),Official (USD),Recovered Calls,Recovered (KES),Total Calls,Total (KES),Total (USD)");

            foreach (var row in StaffRows)
            {
                csv.AppendLine($"\"{row.IndexNumber}\",\"{row.FullName}\",\"{row.OrganizationName}\",\"{row.OfficeName}\"," +
                    $"{row.PersonalCallCount},{row.PersonalCallCostKES:F2},{row.PersonalCallCostUSD:F2}," +
                    $"{row.OfficialCallCount},{row.OfficialCallCostKES:F2},{row.OfficialCallCostUSD:F2}," +
                    $"{row.RecoveredCallCount},{row.RecoveredCallCostKES:F2}," +
                    $"{row.TotalCallCount},{row.TotalCostKES:F2},{row.TotalCostUSD:F2}");
            }

            csv.AppendLine($"TOTAL,,,," +
                $"{StaffRows.Sum(r => r.PersonalCallCount)}," +
                $"{StaffRows.Sum(r => r.PersonalCallCostKES):F2}," +
                $"{StaffRows.Sum(r => r.PersonalCallCostUSD):F2}," +
                $"{StaffRows.Sum(r => r.OfficialCallCount)}," +
                $"{StaffRows.Sum(r => r.OfficialCallCostKES):F2}," +
                $"{StaffRows.Sum(r => r.OfficialCallCostUSD):F2}," +
                $"{StaffRows.Sum(r => r.RecoveredCallCount)}," +
                $"{StaffRows.Sum(r => r.RecoveredCallCostKES):F2}," +
                $"{TotalCallCount}," +
                $"{StaffRows.Sum(r => r.TotalCostKES):F2}," +
                $"{StaffRows.Sum(r => r.TotalCostUSD):F2}");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var monthLabel = FilterMonth == 0
                ? $"AllMonths_{FilterYear}"
                : new DateTime(FilterYear!.Value, FilterMonth!.Value, 1).ToString("MMMM_yyyy");
            var fileName = $"StaffBillingReport_{monthLabel}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            FilterMonth ??= DateTime.UtcNow.Month;
            FilterYear ??= DateTime.UtcNow.Year;
            await LoadDropdownsAsync();
            await LoadStaffBillingDataAsync(applyPagination: false);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Staff Billing");

            // Headers
            var headers = new[] { "Index No", "Name", "Organization", "Office",
                "Personal Calls", "Personal (KES)", "Personal (USD)",
                "Official Calls", "Official (KES)", "Official (USD)",
                "Recovered Calls", "Recovered (KES)",
                "Total Calls", "Total (KES)", "Total (USD)" };
            for (int i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = sheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var r in StaffRows)
            {
                sheet.Cell(row, 1).Value = r.IndexNumber;
                sheet.Cell(row, 2).Value = r.FullName;
                sheet.Cell(row, 3).Value = r.OrganizationName;
                sheet.Cell(row, 4).Value = r.OfficeName;
                sheet.Cell(row, 5).Value = r.PersonalCallCount;
                sheet.Cell(row, 6).Value = r.PersonalCallCostKES;
                sheet.Cell(row, 7).Value = r.PersonalCallCostUSD;
                sheet.Cell(row, 8).Value = r.OfficialCallCount;
                sheet.Cell(row, 9).Value = r.OfficialCallCostKES;
                sheet.Cell(row, 10).Value = r.OfficialCallCostUSD;
                sheet.Cell(row, 11).Value = r.RecoveredCallCount;
                sheet.Cell(row, 12).Value = r.RecoveredCallCostKES;
                sheet.Cell(row, 13).Value = r.TotalCallCount;
                sheet.Cell(row, 14).Value = r.TotalCostKES;
                sheet.Cell(row, 15).Value = r.TotalCostUSD;
                row++;
            }

            // Totals row
            sheet.Cell(row, 1).Value = "TOTAL";
            sheet.Cell(row, 5).Value = StaffRows.Sum(r => r.PersonalCallCount);
            sheet.Cell(row, 6).Value = StaffRows.Sum(r => r.PersonalCallCostKES);
            sheet.Cell(row, 7).Value = StaffRows.Sum(r => r.PersonalCallCostUSD);
            sheet.Cell(row, 8).Value = StaffRows.Sum(r => r.OfficialCallCount);
            sheet.Cell(row, 9).Value = StaffRows.Sum(r => r.OfficialCallCostKES);
            sheet.Cell(row, 10).Value = StaffRows.Sum(r => r.OfficialCallCostUSD);
            sheet.Cell(row, 11).Value = StaffRows.Sum(r => r.RecoveredCallCount);
            sheet.Cell(row, 12).Value = StaffRows.Sum(r => r.RecoveredCallCostKES);
            sheet.Cell(row, 13).Value = TotalCallCount;
            sheet.Cell(row, 14).Value = StaffRows.Sum(r => r.TotalCostKES);
            sheet.Cell(row, 15).Value = StaffRows.Sum(r => r.TotalCostUSD);
            sheet.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
            sheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Format currency columns
            foreach (var col in new[] { 6, 7, 9, 10, 12, 14, 15 })
                sheet.Column(col).Style.NumberFormat.Format = "#,##0.00";

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var monthLabel = FilterMonth == 0
                ? $"AllMonths_{FilterYear}"
                : new DateTime(FilterYear!.Value, FilterMonth!.Value, 1).ToString("MMMM_yyyy");
            var fileName = $"StaffBillingReport_{monthLabel}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    public class StaffBillingRow
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public int PersonalCallCount { get; set; }
        public decimal PersonalCallCostKES { get; set; }
        public decimal PersonalCallCostUSD { get; set; }
        public int OfficialCallCount { get; set; }
        public decimal OfficialCallCostKES { get; set; }
        public decimal OfficialCallCostUSD { get; set; }
        public int RecoveredCallCount { get; set; }
        public decimal RecoveredCallCostKES { get; set; }
        public int TotalCallCount { get; set; }
        public decimal TotalCostKES { get; set; }
        public decimal TotalCostUSD { get; set; }
    }
}
