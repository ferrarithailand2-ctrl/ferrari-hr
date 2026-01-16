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
    public class SalaryModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SalaryModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int SelectedYear { get; set; }

        [BindProperty]
        public int SelectedMonth { get; set; }

        public List<int> YearOptions { get; set; } = new();
        public List<int> MonthOptions { get; set; } = Enumerable.Range(1, 12).ToList();

        public class SalaryRow
        {
            public string UserName { get; set; } = string.Empty;
            public string? Email { get; set; }

            public decimal BaseSalary { get; set; }

            public double OvertimeHours { get; set; }
            public decimal OvertimePay { get; set; }

            public int WorkDays { get; set; }
            public int LateDays { get; set; }
            public int LateMinutes { get; set; }
            public decimal LateDeduction { get; set; }

            public decimal TotalSalary { get; set; }
        }

        public List<SalaryRow> Items { get; set; } = new();

        public async Task OnGetAsync(int? year, int? month)
        {
            InitYearOptions();

            SelectedYear = year ?? DateTime.Today.Year;
            SelectedMonth = month ?? DateTime.Today.Month;

            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            InitYearOptions();

            if (SelectedYear <= 0)
                SelectedYear = DateTime.Today.Year;
            if (SelectedMonth < 1 || SelectedMonth > 12)
                SelectedMonth = DateTime.Today.Month;

            await LoadAsync();
            return Page();
        }

        private void InitYearOptions()
        {
            var currentYear = DateTime.Today.Year;
            YearOptions = new List<int> { currentYear - 1, currentYear, currentYear + 1 };
        }

        private async Task LoadAsync()
        {
            Items = new List<SalaryRow>();

            int year = SelectedYear;
            int month = SelectedMonth;

            // Load salary configs with user
            var configs = await _context.SalaryConfigs
                .Include(s => s.User)
                .ToListAsync();

            // Approved OT for this month, grouped by user
            var otQuery = await _context.OvertimeRequests
                .Where(o =>
                    o.Status == "Approved" &&
                    o.Date.Year == year &&
                    o.Date.Month == month)
                .ToListAsync();

            var otByUser = otQuery
                .GroupBy(o => o.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => (double)x.Hours)
                );

            // Late summary for this month, per user
            var lateRecords = await _context.LateRecords
                .Where(l => l.Year == year && l.Month == month)
                .ToListAsync();

            var lateByUser = lateRecords
                .ToDictionary(
                    l => l.UserId,
                    l => l
                );

            foreach (var cfg in configs)
            {
                if (cfg.User == null)
                    continue;

                var userId = cfg.UserId;
                var userName = cfg.User.UserName ?? string.Empty;
                var email = cfg.User.Email;

                // OT
                double otHours = 0;
                if (otByUser.TryGetValue(userId, out var h))
                {
                    otHours = h;
                }
                var otHoursDec = Convert.ToDecimal(otHours);
                var overtimePay = otHoursDec * cfg.OvertimeHourlyRate;

                // Late
                int workDays = 0;
                int lateDays = 0;
                int lateMinutes = 0;
                decimal lateDeduction = 0;

                if (lateByUser.TryGetValue(userId, out var late))
                {
                    workDays = late.WorkDays;
                    lateDays = late.LateDays;
                    lateMinutes = late.TotalLateMinutes;
                    var lateMinutesDec = Convert.ToDecimal(lateMinutes);
                    lateDeduction = lateMinutesDec * cfg.LateDeductionPerMinute;
                }

                var total = cfg.BaseMonthlySalary + overtimePay - lateDeduction;

                Items.Add(new SalaryRow
                {
                    UserName = userName,
                    Email = email,
                    BaseSalary = cfg.BaseMonthlySalary,
                    OvertimeHours = otHours,
                    OvertimePay = overtimePay,
                    WorkDays = workDays,
                    LateDays = lateDays,
                    LateMinutes = lateMinutes,
                    LateDeduction = lateDeduction,
                    TotalSalary = total
                });
            }

            Items = Items.OrderBy(i => i.UserName).ToList();
        }
    }
}
