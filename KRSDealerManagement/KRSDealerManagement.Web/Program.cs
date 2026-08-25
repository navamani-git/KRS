using KRSDealerManagement.Application;
using KRSDealerManagement.Infrastructure;
using KRSDealerManagement.Web.Middleware;
using KRSDealerManagement.Web.Services;
using KRSDealerManagement.Web.Services.ExcelImport;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

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

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<QueryStringEncryptionMiddleware>();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}");

app.Run();
