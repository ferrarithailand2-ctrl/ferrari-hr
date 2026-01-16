using System.Globalization;
using ClosedXML.Excel;
using FerrariHR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OvertimeReportModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OvertimeReportModel(AppDbContext context, UserManager<IdentityUser> userManager)
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
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Year < 2000 || Year > 2100)
            {
                ModelState.AddModelError(nameof(Year), "Year is not valid.");
                return Page();
            }

            var query = _context.OvertimeRequests.AsQueryable();

            // Filter by CreatedAt year / month
            query = query.Where(o => o.CreatedAt.Year == Year);

            if (Month >= 1 && Month <= 12)
            {
                query = query.Where(o => o.CreatedAt.Month == Month);
            }

            var data = await query
                .OrderBy(o => o.CreatedAt)
                .ThenBy(o => o.UserId)
                .ToListAsync();

            // Load users
            var userIds = data
                .Select(o => o.UserId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("OvertimeReport");

            int row = 1;
            ws.Cell(row, 1).Value = "Employee";
            ws.Cell(row, 2).Value = "Email";
            ws.Cell(row, 3).Value = "Request date";
            ws.Cell(row, 4).Value = "Hours";
            ws.Cell(row, 5).Value = "Reason";
            ws.Cell(row, 6).Value = "Status";
            ws.Cell(row, 7).Value = "Created at";
            ws.Cell(row, 8).Value = "Decision by (UserId)";
            ws.Cell(row, 9).Value = "Decision at";

            ws.Row(row).Style.Font.Bold = true;

            foreach (var o in data)
            {
                row++;

                IdentityUser? user = null;
                if (o.UserId != null && users.TryGetValue(o.UserId, out var found))
                {
                    user = found;
                }

                ws.Cell(row, 1).Value = user?.UserName ?? "";
                ws.Cell(row, 2).Value = user?.Email ?? "";
                ws.Cell(row, 3).Value = o.CreatedAt.ToString("yyyy-MM-dd");
                ws.Cell(row, 4).Value = o.Hours;
                ws.Cell(row, 5).Value = o.Reason ?? "";
                ws.Cell(row, 6).Value = o.Status ?? "";
                ws.Cell(row, 7).Value = o.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                ws.Cell(row, 8).Value = o.DecisionByUserId ?? "";
                ws.Cell(row, 9).Value = o.DecisionAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName;
            if (Month >= 1 && Month <= 12)
            {
                fileName = $"OvertimeReport_{Year}_{Month:00}.xlsx";
            }
            else
            {
                fileName = $"OvertimeReport_{Year}.xlsx";
            }

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(stream.ToArray(), contentType, fileName);
        }
    }
}
