using ClosedXML.Excel;
using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class SalaryReportModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SalaryReportModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int Year { get; set; } = DateTime.Today.Year;

        [BindProperty]
        public int Month { get; set; } = DateTime.Today.Month;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Year < 2000 || Year > 2100)
            {
                ModelState.AddModelError(nameof(Year), "Year is not valid.");
                return Page();
            }

            if (Month < 1 || Month > 12)
            {
                ModelState.AddModelError(nameof(Month), "Month must be 1–12.");
                return Page();
            }

            // 1) Salary Configs (base + rates)
            var configs = await _context.SalaryConfigs.ToListAsync();
            var cfgByUserId = configs.ToDictionary(c => c.UserId, c => c);

            var userIds = configs.Select(c => c.UserId).Distinct().ToList();

            // 2) Late monthly summary (imported)
            var lateRecords = await _context.LateRecords
                .Where(l => l.Year == Year && l.Month == Month)
                .ToListAsync();

            var lateByUserId = lateRecords.ToDictionary(l => l.UserId, l => l);

            // 3) Approved OT in month
            var approvedOt = await _context.OvertimeRequests
                .Where(o => o.Status == "Approved"
                         && o.CreatedAt.Year == Year
                         && o.CreatedAt.Month == Month
                         && o.UserId != null)
                .ToListAsync();

            // OT hours might be double in the model -> force everything to decimal
            var otHoursByUserId = approvedOt
                .GroupBy(o => o.UserId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => (decimal)x.Hours)
                );

            // 4) Load users (only those with SalaryConfig)
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            // 5) Build rows
            var rows = new List<Row>();

            foreach (var u in users)
            {
                if (!cfgByUserId.TryGetValue(u.Id, out var cfg))
                    continue;

                decimal baseSalary = cfg.BaseMonthlySalary;
                decimal otRate = cfg.OvertimeHourlyRate;
                decimal lateRate = cfg.LateDeductionPerMinute;

                decimal otHours = 0m;
                if (otHoursByUserId.TryGetValue(u.Id, out var otHoursValue))
                {
                    otHours = otHoursValue;
                }

                LateRecord? late = null;
                lateByUserId.TryGetValue(u.Id, out late);

                int lateMinutes = late?.TotalLateMinutes ?? 0;
                int lateDays = late?.LateDays ?? 0;

                decimal otPay = otHours * otRate;
                decimal lateDeduction = (decimal)lateMinutes * lateRate;
                decimal totalSalary = baseSalary + otPay - lateDeduction;

                rows.Add(new Row
                {
                    Employee = u.UserName ?? "",
                    Email = u.Email ?? "",
                    BaseMonthlySalary = baseSalary,
                    ApprovedOtHours = otHours,
                    OTRatePerHour = otRate,
                    OTPay = otPay,
                    LateDays = lateDays,
                    LateMinutes = lateMinutes,
                    LateDeductionPerMinute = lateRate,
                    LateDeduction = lateDeduction,
                    TotalSalary = totalSalary
                });
            }

            rows = rows.OrderBy(r => r.Employee).ToList();

            // 6) Export Excel
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("SalaryReport");

            int r = 1;
            ws.Cell(r, 1).Value = "Employee";
            ws.Cell(r, 2).Value = "Email";
            ws.Cell(r, 3).Value = "BaseMonthlySalary";
            ws.Cell(r, 4).Value = "ApprovedOTHours";
            ws.Cell(r, 5).Value = "OTRatePerHour";
            ws.Cell(r, 6).Value = "OTPay";
            ws.Cell(r, 7).Value = "LateDays";
            ws.Cell(r, 8).Value = "LateMinutes";
            ws.Cell(r, 9).Value = "LateDeductionPerMinute";
            ws.Cell(r, 10).Value = "LateDeduction";
            ws.Cell(r, 11).Value = "TotalSalary";

            ws.Row(r).Style.Font.Bold = true;

            foreach (var row in rows)
            {
                r++;
                ws.Cell(r, 1).Value = row.Employee;
                ws.Cell(r, 2).Value = row.Email;
                ws.Cell(r, 3).Value = row.BaseMonthlySalary;
                ws.Cell(r, 4).Value = row.ApprovedOtHours;
                ws.Cell(r, 5).Value = row.OTRatePerHour;
                ws.Cell(r, 6).Value = row.OTPay;
                ws.Cell(r, 7).Value = row.LateDays;
                ws.Cell(r, 8).Value = row.LateMinutes;
                ws.Cell(r, 9).Value = row.LateDeductionPerMinute;
                ws.Cell(r, 10).Value = row.LateDeduction;
                ws.Cell(r, 11).Value = row.TotalSalary;
            }

            // Format money columns
            ws.Columns(3, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns(9, 11).Style.NumberFormat.Format = "#,##0.00";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"SalaryReport_{Year}_{Month:00}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream.ToArray(), contentType, fileName);
        }

        private class Row
        {
            public string Employee { get; set; } = "";
            public string Email { get; set; } = "";
            public decimal BaseMonthlySalary { get; set; }
            public decimal ApprovedOtHours { get; set; }
            public decimal OTRatePerHour { get; set; }
            public decimal OTPay { get; set; }
            public int LateDays { get; set; }
            public int LateMinutes { get; set; }
            public decimal LateDeductionPerMinute { get; set; }
            public decimal LateDeduction { get; set; }
            public decimal TotalSalary { get; set; }
        }
    }
}
