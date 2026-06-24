using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Options;
using KIGHolding.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KIGHolding.AdminAuth";
        options.LoginPath = "/Admin/Auth/Login";
        options.AccessDeniedPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.EventsType = typeof(AdminCookieAuthenticationEvents);
    });
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache(options =>
{
    // Each rate-limit entry declares Size = 1; cap at 50 000 concurrent tracked identities.
    options.SizeLimit = 50_000;
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
    [
        "image/svg+xml",
        "application/xml",
        "text/xml"
    ]);
});

var dataProtectionKeyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys"));
dataProtectionKeyDirectory.Create();
builder.Services.AddDataProtection()
    .SetApplicationName("KIGHolding.Web")
    .PersistKeysToFileSystem(dataProtectionKeyDirectory);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection("ResendSettings"));
builder.Services.Configure<AdminBootstrapSettings>(builder.Configuration.GetSection("AdminBootstrap"));
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["ResendSettings:ApiKey"] ?? string.Empty;
});
builder.Services.AddScoped<AdminBootstrapConfigurationResolver>();
builder.Services.AddScoped<IAdminLegacyCredentialGuard, AdminLegacyCredentialGuard>();
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();
builder.Services.AddScoped<IMenuGroupService, MenuGroupService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<AdminCookieAuthenticationEvents>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminLegacyCredentialGuard = scope.ServiceProvider.GetRequiredService<IAdminLegacyCredentialGuard>();
    await adminLegacyCredentialGuard.EnsureSecureAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path;
        var headers = context.Context.Response.Headers;

        if (path.StartsWithSegments("/uploads"))
        {
            headers[HeaderNames.CacheControl] = "public,max-age=86400";
            return;
        }

        if (context.Context.Request.Query.ContainsKey("v"))
        {
            headers[HeaderNames.CacheControl] = "public,max-age=31536000,immutable";
            return;
        }

        if (path.StartsWithSegments("/css") ||
            path.StartsWithSegments("/js") ||
            path.StartsWithSegments("/images") ||
            path.StartsWithSegments("/lib"))
        {
            headers[HeaderNames.CacheControl] = "public,max-age=3600";
        }
    }
});
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin-root",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Configuration.GetValue<bool>("Database:RunInitializer"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
