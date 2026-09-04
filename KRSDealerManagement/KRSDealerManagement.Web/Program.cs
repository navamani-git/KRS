using KRSDealerManagement.Application;
using KRSDealerManagement.Infrastructure;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Middleware;
using KRSDealerManagement.Web.Services;
using KRSDealerManagement.Web.Services.ExcelImport;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<KRSDealerManagement.Web.Filters.ReadOnlyMenuGuardFilter>();
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 600_000_000;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 600_000_000;
});

// Query string encryption (single service used across the app)
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IQueryStringCrypto, QueryStringCrypto>();

// Get connection string from configuration (appsettings / web.config env vars)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

// Register Application layer services (MediatR, AutoMapper, FluentValidation, AuditService)
builder.Services.AddApplicationServices();

// Register Infrastructure layer services (UnitOfWork, Repositories, DbContext)
builder.Services.AddInfrastructureServices(connectionString);

builder.Services.AddExcelImport();

// Add session support for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HTTP context accessor for accessing HTTP context in services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

var migratedLegacyFiles = AppFileStorageHelper.MigrateLegacyWwwrootFiles(app.Environment);
if (migratedLegacyFiles > 0)
{
    app.Logger.LogInformation(
        "Migrated {Count} legacy upload(s) from wwwroot/Files to {Target}",
        migratedLegacyFiles,
        Path.Combine(app.Environment.ContentRootPath, AppFileStorageHelper.RootFolder));
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Uploaded files must be served through controller actions, not as public static files.
        if (ctx.Context.Request.Path.StartsWithSegments("/Files", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.StatusCode = StatusCodes.Status404NotFound;
            ctx.Context.Response.ContentLength = 0;
        }
    }
});

app.UseMiddleware<QueryStringEncryptionMiddleware>();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}");

app.Run();
