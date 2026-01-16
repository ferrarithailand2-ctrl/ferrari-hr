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
    public class OvertimeRequestsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OvertimeRequestsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class OvertimeRequestView
        {
            public OvertimeRequest Request { get; set; } = null!;
            public string EmployeeEmail { get; set; } = string.Empty;
        }

        public List<OvertimeRequestView> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var allRequests = await _context.OvertimeRequests
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            Requests = new List<OvertimeRequestView>();

            foreach (var r in allRequests)
            {
                var user = await _userManager.FindByIdAsync(r.UserId);
                var email = user?.Email ?? "(unknown)";

                Requests.Add(new OvertimeRequestView
                {
                    Request = r,
                    EmployeeEmail = email
                });
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var request = await _context.OvertimeRequests.FindAsync(id);
            if (request == null)
                return RedirectToPage();

            var admin = await _userManager.GetUserAsync(User);
            request.Status = "Approved";
            request.DecisionByUserId = admin?.Id;
            request.DecisionAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var request = await _context.OvertimeRequests.FindAsync(id);
            if (request == null)
                return RedirectToPage();

            var admin = await _userManager.GetUserAsync(User);
            request.Status = "Rejected";
            request.DecisionByUserId = admin?.Id;
            request.DecisionAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
