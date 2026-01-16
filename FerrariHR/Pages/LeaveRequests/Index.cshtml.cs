using System.ComponentModel.DataAnnotations;
using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.LeaveRequests
{
    [Authorize] // user must be logged in
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<LeaveRequest> MyRequests { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; }

            [Required]
            [DataType(DataType.Date)]
            public DateTime EndDate { get; set; }

            [Required]
            [Display(Name = "Leave type")]
            public string Type { get; set; } = "Annual";

            [Display(Name = "Reason")]
            public string? Reason { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Set default dates for new leave request form
            if (Input == null)
            {
                Input = new InputModel();
            }

            if (Input.StartDate == default)
            {
                Input.StartDate = DateTime.Today;
            }

            if (Input.EndDate == default)
            {
                Input.EndDate = DateTime.Today;
            }

            // Load existing requests as before
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                MyRequests = await _context.LeaveRequests
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.StartDate)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload current list and show validation errors
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var request = new LeaveRequest
            {
                UserId = user.Id,
                StartDate = Input.StartDate,
                EndDate = Input.EndDate,
                Type = Input.Type,
                Reason = Input.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
