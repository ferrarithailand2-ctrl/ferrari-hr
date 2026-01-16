using System.ComponentModel.DataAnnotations;
using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.Overtime
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

        public List<OvertimeRequest> MyRequests { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; }

            [Required]
            [Range(0.25, 24)]
            [Display(Name = "Hours")]
            public double Hours { get; set; }

            [Display(Name = "Reason")]
            public string? Reason { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (Input == null)
            {
                Input = new InputModel();
            }

            if (Input.Date == default)
            {
                Input.Date = DateTime.Today;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                MyRequests = await _context.OvertimeRequests
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.Date)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var request = new OvertimeRequest
            {
                UserId = user.Id,
                Date = Input.Date,
                Hours = Input.Hours,
                Reason = Input.Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.OvertimeRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
