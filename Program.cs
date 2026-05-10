using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Models;

using panelapp.Services;



var builder = WebApplication.CreateBuilder(args);
#pragma warning disable CA1416 // Validate platform compatibility
builder.Logging.AddEventLog(settings =>
{
    settings.LogName = "Application";
    settings.SourceName = "panelapp";
});
#pragma warning restore CA1416 // Validate platform compatibility


builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPanelCodeService, PanelCodeService>();
builder.Services.AddScoped<IPanelExportService, PanelExportService>();
builder.Services.AddScoped<IPanelMaterialService, PanelMaterialService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IMaterialImportService, MaterialImportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPanelService, PanelService>();



builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();
await UserPasswordMigration.MigratePlainTextPasswordsAsync(app.Services);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Επιτρεπτά paths χωρίς login
    var allowedPaths = new[]
    {
        "/account/login"
    };

    var isStaticFile = path != null &&
                       (path.StartsWith("/lib") ||
                        path.StartsWith("/css") ||
                        path.StartsWith("/js") ||
                        path.StartsWith("/images") ||
                        path.StartsWith("/favicon"));

    var isAllowed = allowedPaths.Any(p => path != null && path.StartsWith(p));
    var hasSession = context.Session.GetString("Username") != null;

    if (!hasSession && !isAllowed && !isStaticFile)
    {
        context.Response.Redirect("/Account/Login");
        return;
    }

    await next();
});

app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();