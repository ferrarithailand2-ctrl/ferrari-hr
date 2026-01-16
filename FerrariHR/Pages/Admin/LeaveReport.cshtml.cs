using System.Globalization;
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
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class LeaveReportModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveReportModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int Year { get; set; } = DateTime.Today.Year;

        // 1–12; if 0 or <1, export all months
        [BindProperty]
        public int Month { get; set; } = DateTime.Today.Month;

        public void OnGet()
        {
            // Show form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Year < 2000 || Year > 2100)
            {
                ModelState.AddModelError(nameof(Year), "Year is not valid.");
                return Page();
            }

            // Base query
            var query = _context.LeaveRequests.AsQueryable();

            // Filter by year using StartDate
            query = query.Where(l => l.StartDate.Year == Year);

            // Optional month filter
            if (Month >= 1 && Month <= 12)
            {
                query = query.Where(l => l.StartDate.Month == Month);
            }

            var data = await query
                .OrderBy(l => l.StartDate)
                .ThenBy(l => l.UserId)
                .ToListAsync();

            // Collect user IDs and load user info
            var userIds = data
                .Select(l => l.UserId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            // Build Excel
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("LeaveReport");

            int row = 1;
            ws.Cell(row, 1).Value = "Employee";
            ws.Cell(row, 2).Value = "Email";
            ws.Cell(row, 3).Value = "Start date";
            ws.Cell(row, 4).Value = "End date";
            ws.Cell(row, 5).Value = "Type";
            ws.Cell(row, 6).Value = "Reason";
            ws.Cell(row, 7).Value = "Status";
            ws.Cell(row, 8).Value = "Requested at";
            ws.Cell(row, 9).Value = "Decision by (UserId)";
            ws.Cell(row, 10).Value = "Decision at";

            ws.Row(row).Style.Font.Bold = true;

            foreach (var l in data)
            {
                row++;

                IdentityUser? user = null;
                if (l.UserId != null && users.TryGetValue(l.UserId, out var found))
                {
                    user = found;
                }

                ws.Cell(row, 1).Value = user?.UserName ?? "";
                ws.Cell(row, 2).Value = user?.Email ?? "";
                ws.Cell(row, 3).Value = l.StartDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 4).Value = l.EndDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 5).Value = l.Type ?? "";
                ws.Cell(row, 6).Value = l.Reason ?? "";
                ws.Cell(row, 7).Value = l.Status ?? "";
                ws.Cell(row, 8).Value = l.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                ws.Cell(row, 9).Value = l.DecisionByUserId ?? "";
                ws.Cell(row, 10).Value = l.DecisionAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName;
            if (Month >= 1 && Month <= 12)
            {
                fileName = $"LeaveReport_{Year}_{Month:00}.xlsx";
            }
            else
            {
                fileName = $"LeaveReport_{Year}.xlsx";
            }

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream.ToArray(), contentType, fileName);
        }
    }
}
