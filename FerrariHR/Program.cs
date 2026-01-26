using FerrariHR.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=ferrarihr.db";

// Add SQLite DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add Identity with roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;         
        options.Password.RequireLowercase = true;      
        options.Password.RequireUppercase = true;       
        options.Password.RequireNonAlphanumeric = false; 
        options.Password.RequiredLength = 6;            
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages(options =>
{
    // Require auth for all pages by default
    options.Conventions.AuthorizeFolder("/");

    // Allow anonymous access to the Login page
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});

var app = builder.Build();

// Ensure database and Identity schema exist, then seed roles/user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    await DataSeeder.SeedAsync(services);
}

// pipeline...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
