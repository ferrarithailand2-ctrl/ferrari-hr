using FerrariHR.Data;
using FerrariHR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FerrariHR.Pages.Training
{
    [Authorize] // view for all logged-in users
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<TrainingMaterial> Items { get; set; } = new();

        [BindProperty]
        public CreateInput Input { get; set; } = new();

        [BindProperty]
        public EditInput Edit { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        public class CreateInput
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string OneDriveUrl { get; set; } = "";
        }

        public class EditInput
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string OneDriveUrl { get; set; } = "";
        }

        private bool IsAdminOrSuperAdmin()
            => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        private static bool IsValidUrl(string url)
            => Uri.TryCreate(url, UriKind.Absolute, out _);

        public async Task OnGetAsync()
        {
            await LoadAsync();

            if (EditId.HasValue)
            {
                var item = await _context.TrainingMaterials.FirstOrDefaultAsync(x => x.Id == EditId.Value);
                if (item != null)
                {
                    Edit = new EditInput
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Description = item.Description,
                        OneDriveUrl = item.OneDriveUrl
                    };
                }
            }
        }

        private async Task LoadAsync()
        {
            Items = await _context.TrainingMaterials
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!IsAdminOrSuperAdmin())
                return Forbid();

            if (string.IsNullOrWhiteSpace(Input.Title) || string.IsNullOrWhiteSpace(Input.OneDriveUrl))
            {
                ModelState.AddModelError("", "Title and URL are required.");
                await LoadAsync();
                return Page();
            }

            if (!IsValidUrl(Input.OneDriveUrl))
            {
                ModelState.AddModelError("", "URL is not valid.");
                await LoadAsync();
                return Page();
            }

            var material = new TrainingMaterial
            {
                Title = Input.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                OneDriveUrl = Input.OneDriveUrl.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.TrainingMaterials.Add(material);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // ENTER EDIT MODE (querystring EditId)
        public IActionResult OnGetEdit(int id)
        {
            if (!IsAdminOrSuperAdmin())
                return Forbid();

            return RedirectToPage(new { EditId = id });
        }

        public IActionResult OnGetCancelEdit()
        {
            if (!IsAdminOrSuperAdmin())
                return Forbid();

            return RedirectToPage();
        }

        // UPDATE
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!IsAdminOrSuperAdmin())
                return Forbid();

            if (Edit.Id <= 0)
                return RedirectToPage();

            if (string.IsNullOrWhiteSpace(Edit.Title) || string.IsNullOrWhiteSpace(Edit.OneDriveUrl))
            {
                ModelState.AddModelError("", "Title and URL are required.");
                EditId = Edit.Id;
                await LoadAsync();
                return Page();
            }

            if (!IsValidUrl(Edit.OneDriveUrl))
            {
                ModelState.AddModelError("", "URL is not valid.");
                EditId = Edit.Id;
                await LoadAsync();
                return Page();
            }

            var item = await _context.TrainingMaterials.FirstOrDefaultAsync(x => x.Id == Edit.Id);
            if (item == null)
                return RedirectToPage();

            item.Title = Edit.Title.Trim();
            item.Description = string.IsNullOrWhiteSpace(Edit.Description) ? null : Edit.Description.Trim();
            item.OneDriveUrl = Edit.OneDriveUrl.Trim();

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // DELETE
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsAdminOrSuperAdmin())
                return Forbid();

            var item = await _context.TrainingMaterials.FirstOrDefaultAsync(x => x.Id == id);
            if (item != null)
            {
                _context.TrainingMaterials.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
