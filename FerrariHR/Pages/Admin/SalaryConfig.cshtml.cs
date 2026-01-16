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
    [Authorize(Roles = "SuperAdmin")]
    public class SalaryConfigModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SalaryConfigModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class SalaryRow
        {
            public string UserName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public decimal BaseMonthlySalary { get; set; }
            public decimal OvertimeHourlyRate { get; set; }
            public decimal LateDeductionPerMinute { get; set; }
        }

        public List<SalaryRow> Items { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public string? Message { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Items = await _context.SalaryConfigs
                .Include(s => s.User)
                .Select(s => new SalaryRow
                {
                    UserName = s.User!.UserName ?? string.Empty,
                    Email = s.User!.Email,
                    BaseMonthlySalary = s.BaseMonthlySalary,
                    OvertimeHourlyRate = s.OvertimeHourlyRate,
                    LateDeductionPerMinute = s.LateDeductionPerMinute
                })
                .OrderBy(s => s.UserName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                Message = "Please select an Excel file.";
                await LoadAsync();
                return Page();
            }

            if (!UploadFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Only .xlsx files are supported.";
                await LoadAsync();
                return Page();
            }

            int imported = 0;
            int skipped = 0;

            using (var stream = UploadFile.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var ws = workbook.Worksheets.First();

                // Expected columns:
                // A: EmployeeName
                // B: BaseMonthlySalary
                // C: OTRatePerHour
                // D: LateDeductionPerMinute

                int row = 2;
                while (true)
                {
                    var rawName = ws.Cell(row, 1).GetString();
                    var name = rawName?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        // Empty name = end of data
                        break;
                    }

                    // Case-insensitive comparison
                    var normalized = name.ToUpperInvariant();

                    var user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized);

                    if (user == null)
                    {
                        // No matching user
                        skipped++;
                        row++;
                        continue;
                    }

                    var baseCell = ws.Cell(row, 2);
                    var otCell = ws.Cell(row, 3);
                    var lateCell = ws.Cell(row, 4);

                    // Base salary is REQUIRED
                    if (!TryParseDecimalFlexible(baseCell, out var baseSalary))
                    {
                        skipped++;
                        row++;
                        continue;
                    }

                    // OT rate is OPTIONAL: empty = 0
                    decimal otRate = 0;
                    var otText = otCell.GetString();
                    if (!string.IsNullOrWhiteSpace(otText))
                    {
                        if (!TryParseDecimalFlexible(otCell, out otRate))
                        {
                            skipped++;
                            row++;
                            continue;
                        }
                    }

                    // Late deduction is OPTIONAL: empty = 0
                    decimal latePenalty = 0;
                    var lateText = lateCell.GetString();
                    if (!string.IsNullOrWhiteSpace(lateText))
                    {
                        if (!TryParseDecimalFlexible(lateCell, out latePenalty))
                        {
                            skipped++;
                            row++;
                            continue;
                        }
                    }

                    // Upsert: one SalaryConfig per user
                    var existing = await _context.SalaryConfigs
                        .SingleOrDefaultAsync(s => s.UserId == user.Id);

                    if (existing != null)
                    {
                        existing.BaseMonthlySalary = baseSalary;
                        existing.OvertimeHourlyRate = otRate;
                        existing.LateDeductionPerMinute = latePenalty;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var cfg = new SalaryConfig
                        {
                            UserId = user.Id,
                            BaseMonthlySalary = baseSalary,
                            OvertimeHourlyRate = otRate,
                            LateDeductionPerMinute = latePenalty,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.SalaryConfigs.Add(cfg);
                    }

                    imported++;
                    row++;
                }
                await _context.SaveChangesAsync();
            }

            ImportedCount = imported;
            SkippedCount = skipped;
            Message = $"Imported {imported} row(s), skipped {skipped}.";

            await LoadAsync();
            return Page();
        }

        private bool TryParseDecimalFlexible(IXLCell cell, out decimal value)
        {
            // 1) Try direct typed value
            if (cell.TryGetValue(out value))
            {
                return true;
            }

            // 2) Fallback: parse from string
            var s = cell.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                value = 0;
                return false;
            }

            s = s.Trim();
            // strip common currency symbols / commas
            s = s.Replace("฿", string.Empty)
                 .Replace("THB", string.Empty)
                 .Replace(",", string.Empty);

            // invariant culture
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            // current culture as fallback
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }
    }
}
