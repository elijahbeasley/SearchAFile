using Microsoft.AspNetCore.Authentication.Cookies;
using SearchAFile;
using SearchAFile.Core.Options;
using SearchAFile.Services;
using SearchAFile.Web.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// ---- Configuration sources ----
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Make configuration available to your static helper (your pattern)
SystemFunctions.Configuration = builder.Configuration;

// Add the account controller for shared account funtionality. 
builder.Services.AddScoped<AccountController>();

// ---- Telemetry ----
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// ---- AuthN / AuthZ ----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(720);
        options.LoginPath = "/";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.LogoutPath = "/";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Roles / policies (your originals preserved)
    options.AddPolicy("AdminsPolicy", p => p.RequireRole("System Admin", "Admin"));
    options.AddPolicy("UserPolicy", p => p.RequireRole("System Admin", "User"));
    options.AddPolicy("SystemAdminsPolicy", p => p.RequireRole("System Admin"));
    options.AddPolicy("CommonPolicy", p => p.RequireRole("System Admin", "Admin", "User"));
    options.AddPolicy("MaintainCompaniesPolicy", p => p.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainFilesPolicy", p => p.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainCollectionsPolicy", p => p.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainSystemInfoPolicy", p => p.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainUsersPolicy", p => p.RequireRole("System Admin", "Admin"));
});

// ---- Razor Pages + (optional) Controllers ----
builder.Services.AddRazorPages(options =>
{
    // Folder conventions (your originals preserved)
    options.Conventions.AuthorizeFolder("/Admins", "AdminsPolicy");
    options.Conventions.AuthorizeFolder("/Users", "UserPolicy");
    options.Conventions.AuthorizeFolder("/SystemAdmins", "SystemAdminsPolicy");
    options.Conventions.AuthorizeFolder("/Common", "CommonPolicy");
    options.Conventions.AllowAnonymousToFolder("/Home");
    options.Conventions.AuthorizeFolder("/Collections", "MaintainCollectionsPolicy");
    options.Conventions.AuthorizeFolder("/Companies", "MaintainCompaniesPolicy");
    options.Conventions.AuthorizeFolder("/Files", "MaintainFilesPolicy");
    options.Conventions.AuthorizeFolder("/SystemInfo", "MaintainSystemInfoPolicy");
    options.Conventions.AuthorizeFolder("/Users", "MaintainUsersPolicy");
});

// If you actually use MVC controllers (you map a controller route below), register them:
builder.Services.AddControllers();

// ---- Session ----
builder.Services.AddSession(options =>
{
    options.Cookie.Name = env.IsDevelopment() ? ".SearchAFile.Development.Session" : ".SearchAFile.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(720);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---- Cross-cutting helpers ----
// Only one HttpContextAccessor registration needed
builder.Services.AddHttpContextAccessor();

// Email + SMS services (your originals)
builder.Services.AddTransient<IEmailService, EmailServiceMailKit>();
builder.Services.AddTransient<ISMSService, SMSServiceAzure>();

// Your auth/login helper
builder.Services.AddScoped<AuthClient>();

// ---- Typed API client (recommended pattern) ----

// Bind ApiAuth options (ClientId/Secret)
builder.Services.Configure<ApiAuthOptions>(builder.Configuration.GetSection(ApiAuthOptions.SectionName));

// Typed HttpClient for your API (AuthenticatedApiClient ctor accepts HttpClient)
builder.Services.AddHttpClient<AuthenticatedApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);

    // Simple: set headers from config at startup (rotate requires app restart)
    client.DefaultRequestHeaders.Add("X-Client-Id", builder.Configuration["ApiAuth:ClientId"]!);
    client.DefaultRequestHeaders.Add("X-Client-Secret", builder.Configuration["ApiAuth:ClientSecret"]!);
});

// ---- OpenAI + physical storage (typed clients + singleton storage) ----
builder.Services.AddOpenAIAndStorage(builder.Configuration);

var app = builder.Build();

// ---- Middleware pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // default 30 days
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Default}/{id?}");

app.MapRazorPages();

// Make DI available to your static helpers (your existing pattern)
HttpContextHelper.Services = app.Services;

app.Run();