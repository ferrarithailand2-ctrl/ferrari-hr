using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FerrariHR.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public class UserRow
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public IList<string> Roles { get; set; } = new List<string>();
        }

        public List<UserRow> Users { get; set; } = new();

        // CREATE NEW USER
        [BindProperty]
        public CreateUserInput NewUser { get; set; } = new();

        public class CreateUserInput
        {
            [Required]
            [Display(Name = "Employee code/name (for login & Excel)")]
            public string UserName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; } = "Employee"; // Employee or Admin
        }

        public async Task OnGetAsync()
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            Users = new List<UserRow>();

            var allUsers = _userManager.Users.ToList();

            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);

                Users.Add(new UserRow
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email,
                    Roles = roles
                });
            }

            Users = Users
                .OrderByDescending(u => u.Roles.Contains("SuperAdmin"))
                .ThenBy(u => u.UserName)
                .ToList();
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUsersAsync();
                return Page();
            }

            // Ensure roles exist
            if (!await _roleManager.RoleExistsAsync("Employee"))
                await _roleManager.CreateAsync(new IdentityRole("Employee"));
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var userName = NewUser.UserName.Trim();

            var user = new IdentityUser
            {
                UserName = userName,
                Email = NewUser.Email
            };

            var result = await _userManager.CreateAsync(user, NewUser.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await LoadUsersAsync();
                return Page();
            }

            var roleToAssign = NewUser.Role == "Admin" ? "Admin" : "Employee";
            await _userManager.AddToRoleAsync(user, roleToAssign);

            return RedirectToPage();
        }

        // TOGGLE ADMIN ROLE
        public async Task<IActionResult> OnPostToggleAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToPage();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                // Do not change roles for SuperAdmin
                return RedirectToPage();
            }

            if (roles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }
            else
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));

                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return RedirectToPage();
        }

        // DELETE USER
        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToPage();
            }

            // Do not allow deleting SuperAdmin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                return RedirectToPage();
            }

            // Do not allow deleting yourself (logged-in SuperAdmin)
            var current = await _userManager.GetUserAsync(User);
            if (current != null && current.Id == user.Id)
            {
                return RedirectToPage();
            }

            await _userManager.DeleteAsync(user);

            return RedirectToPage();
        }
    }
}
