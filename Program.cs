using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using StoreManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StoreManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- ADD ASP.NET CORE IDENTITY ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<StoreManagementDbContext>();

// --- ADD ENTRA ID AUTHENTICATION ---
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI(); // This adds the UI for login/logout and other auth-related pages

builder.Services.AddRazorPages();
var app = builder.Build();

// Auto-Generate the database if it doesn't exist (No Migrations)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StoreManagementDbContext>();

        //This will bypass the migrations and directly create the database based on the current model if DB doesn't exist.
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        //if something goes wrong during DB creation, it log the error )
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // This enables authentication middleware to process incoming requests and set the user context
app.UseAuthorization();

// --- NEW ROUTING FOR AREAS ---
app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// --- DEFAULT ROUTING ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();