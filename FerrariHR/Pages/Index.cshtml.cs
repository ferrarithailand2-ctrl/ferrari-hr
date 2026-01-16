using FerrariHR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string DisplayName { get; set; } = "";

        // Employee scope
        public int MyLeavePending { get; set; }
        public int MyLeaveApproved { get; set; }
        public int MyLeaveRejected { get; set; }

        public int MyOtPending { get; set; }
        public int MyOtApproved { get; set; }
        public int MyOtRejected { get; set; }

        public int MyLateMinutesThisMonth { get; set; }
        public int MyLateDaysThisMonth { get; set; }

        // SuperAdmin scope
        public int AllLeavePending { get; set; }
        public int AllOtPending { get; set; }

        // Admin scope
        public int LateImportedUsersThisMonth { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            DisplayName = user.UserName ?? user.Email ?? "User";

            // Current month
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;

            // Employee cards: my counts
            var uid = user.Id;

            MyLeavePending = await _context.LeaveRequests.CountAsync(x => x.UserId == uid && x.Status == "Pending");
            MyLeaveApproved = await _context.LeaveRequests.CountAsync(x => x.UserId == uid && x.Status == "Approved");
            MyLeaveRejected = await _context.LeaveRequests.CountAsync(x => x.UserId == uid && x.Status == "Rejected");

            MyOtPending = await _context.OvertimeRequests.CountAsync(x => x.UserId == uid && x.Status == "Pending");
            MyOtApproved = await _context.OvertimeRequests.CountAsync(x => x.UserId == uid && x.Status == "Approved");
            MyOtRejected = await _context.OvertimeRequests.CountAsync(x => x.UserId == uid && x.Status == "Rejected");

            var myLate = await _context.LateRecords
                .FirstOrDefaultAsync(x => x.UserId == uid && x.Year == year && x.Month == month);

            MyLateMinutesThisMonth = myLate?.TotalLateMinutes ?? 0;
            MyLateDaysThisMonth = myLate?.LateDays ?? 0;

            // SuperAdmin: pending approvals
            if (User.IsInRole("SuperAdmin"))
            {
                AllLeavePending = await _context.LeaveRequests.CountAsync(x => x.Status == "Pending");
                AllOtPending = await _context.OvertimeRequests.CountAsync(x => x.Status == "Pending");
            }

            // Admin (and SuperAdmin): late import coverage
            if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            {
                LateImportedUsersThisMonth = await _context.LateRecords.CountAsync(x => x.Year == year && x.Month == month);
            }
        }
    }
}
