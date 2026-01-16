using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace FerrariHR.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class LateSummaryModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LateSummaryModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int Year { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Month { get; set; }

        public class RowView
        {
            public string EmployeeName { get; set; } = string.Empty;
            public string EmployeeEmail { get; set; } = string.Empty;
            public LateRecord Late { get; set; } = null!;
        }

        public List<RowView> Records { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (Year == 0 || Month == 0)
            {
                var now = DateTime.Now;
                Year = now.Year;
                Month = now.Month;
            }

            var lateRecords = await _context.LateRecords
                .Where(x => x.Year == Year && x.Month == Month)
                .OrderBy(x => x.UserId)
                .ToListAsync();

            var list = new List<RowView>();

            foreach (var rec in lateRecords)
            {
                var user = await _userManager.FindByIdAsync(rec.UserId);
                list.Add(new RowView
                {
                    Late = rec,
                    EmployeeName = user?.UserName ?? "(unknown)",
                    EmployeeEmail = user?.Email ?? ""
                });
            }

            Records = list;
        }
    }
}
