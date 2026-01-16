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
    public class LeaveRequestsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveRequestsModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class LeaveRequestView
        {
            public LeaveRequest Request { get; set; } = null!;
            public string EmployeeEmail { get; set; } = string.Empty;
        }

        public List<LeaveRequestView> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var allRequests = await _context.LeaveRequests
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // attach email for each user
            Requests = new List<LeaveRequestView>();

            foreach (var r in allRequests)
            {
                var user = await _userManager.FindByIdAsync(r.UserId);
                var email = user?.Email ?? "(unknown)";

                Requests.Add(new LeaveRequestView
                {
                    Request = r,
                    EmployeeEmail = email
                });
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var request = await _context.LeaveRequests.FindAsync(id);
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
            var request = await _context.LeaveRequests.FindAsync(id);
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
