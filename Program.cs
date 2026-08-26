using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License =
    LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------
// Database
// ---------------------------------------

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


// ---------------------------------------
// ASP.NET Core Identity
// ---------------------------------------

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();


// ---------------------------------------
// MVC
// ---------------------------------------

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ExcelReportService>();
builder.Services.AddScoped<PdfReportService>();

var app = builder.Build();


// ---------------------------------------
// HTTP Pipeline
// ---------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();


// MVC routing
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new
    {
        area = "Identity",
        page = "/Account/Login"
    });

app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new
    {
        area = "Identity",
        page = "/Account/Register"
    });
    
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Identity Razor Pages
app.MapRazorPages();

app.Run();