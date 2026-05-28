using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StoreManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 1. ADD LOCAL IDENTITY WITH ROLE SUPPORT ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // THIS LINE IS CRUCIAL FOR RBAC!
    .AddEntityFrameworkStores<StoreManagementDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // For real-time notifications
builder.Services.AddRazorPages();

var app = builder.Build();

// --- 2. DATA SEEDER: AUTO-GENERATE DB, ROLES, AND ADMIN ACCOUNT ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StoreManagementDbContext>();
        // This will create the DB and all Identity tables (AspNetUsers, AspNetRoles, etc.)
        context.Database.EnsureCreated();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Create Roles if they don't exist
        string[] roleNames = { "Admin", "Customer" };
        foreach (var roleName in roleNames)
        {
            var roleExist = roleManager.RoleExistsAsync(roleName).Result;
            if (!roleExist)
            {
                roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
            }
        }

        // Create a default Admin user if none exists
        var adminEmail = "admin@store.com";
        var adminUser = userManager.FindByEmailAsync(adminEmail).Result;

        if (adminUser == null)
        {
            adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
            // Note: Passwords must have an uppercase, lowercase, number, and special character by default
            var createPowerUser = userManager.CreateAsync(adminUser, "Admin@123!").Result;

            if (createPowerUser.Succeeded)
            {
                // Assign this new user to the Admin role
                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication MUST be before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Needed for Identity UI routing

app.MapHub<StoreManagementSystem.Hubs.NotificationHub>("/notificationHub");
app.Run();