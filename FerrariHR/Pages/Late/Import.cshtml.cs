using ClosedXML.Excel;
using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.Late
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ImportModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ImportModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int Year { get; set; } = DateTime.Today.Year;

        [BindProperty]
        public int Month { get; set; } = DateTime.Today.Month;

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public string? Message { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                Message = "Please select an Excel file.";
                return Page();
            }

            if (!UploadFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Only .xlsx files are supported.";
                return Page();
            }

            int imported = 0;
            int skipped = 0;

            using (var stream = UploadFile.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var ws = workbook.Worksheets.First();

                // Expected columns (summary from time-tracking tool):
                // A: Employee Name
                // B: WorkDays
                // C: LateDays
                // D: TotalLateMinutes

                int row = 2;
                while (true)
                {
                    var rawName = ws.Cell(row, 1).GetString();
                    var name = rawName?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        // end of data
                        break;
                    }

                    var normalized = name.ToUpperInvariant();

                    var user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized);

                    if (user == null)
                    {
                        skipped++;
                        row++;
                        continue;
                    }

                    int workDays;
                    int lateDays;
                    int totalMinutes;

                    var workCell = ws.Cell(row, 2);
                    var lateDaysCell = ws.Cell(row, 3);
                    var minutesCell = ws.Cell(row, 4);

                    if (!workCell.TryGetValue(out workDays) ||
                        !lateDaysCell.TryGetValue(out lateDays) ||
                        !minutesCell.TryGetValue(out totalMinutes))
                    {
                        skipped++;
                        row++;
                        continue;
                    }

                    // Upsert per user/year/month
                    var existing = await _context.LateRecords
                        .SingleOrDefaultAsync(l =>
                            l.UserId == user.Id &&
                            l.Year == Year &&
                            l.Month == Month);

                    if (existing != null)
                    {
                        existing.WorkDays = workDays;
                        existing.LateDays = lateDays;
                        existing.TotalLateMinutes = totalMinutes;
                    }
                    else
                    {
                        var record = new LateRecord
                        {
                            UserId = user.Id,
                            Year = Year,
                            Month = Month,
                            WorkDays = workDays,
                            LateDays = lateDays,
                            TotalLateMinutes = totalMinutes,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.LateRecords.Add(record);
                    }

                    imported++;
                    row++;
                }

                await _context.SaveChangesAsync();
            }

            ImportedCount = imported;
            SkippedCount = skipped;
            Message = $"Imported {imported} row(s), skipped {skipped}.";

            return Page();
        }
    }
}
