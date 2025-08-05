using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SearchAFile;
using SearchAFile.Web.Helpers;
using SearchAFile.Services;
using SearchAFile.Web.Helpers;
using SearchAFile.Web.Interfaces;
using SearchAFile.Web.Services;
using System.Net.Http.Headers;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Determine the environment
var env = builder.Environment;

// Add the account controller for shared account funtionality. 
builder.Services.AddScoped<AccountController>();

// Uses the appsettings.Development.json file when developing.
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Assign configuration to the static class
SystemFunctions.Configuration = builder.Configuration;

// Add Application Insights telemetry using the connection string from appsettings.json
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(720);
    options.LoginPath = new PathString("/");
    options.AccessDeniedPath = PathString.FromUriComponent("/Home/AccessDenied");
    options.LogoutPath = new PathString("/");
    options.SlidingExpiration = true;
});

// These are authorization groups.
builder.Services.AddAuthorization(options =>
{
    // User Roles:
    options.AddPolicy("AdminsPolicy", policy => policy.RequireRole("System Admin", "Admin"));
    options.AddPolicy("EmployeePolicy", policy => policy.RequireRole("System Admin", "Employee"));
    options.AddPolicy("SystemAdminsPolicy", policy => policy.RequireRole("System Admin"));

    // Misc.
    options.AddPolicy("CommonPolicy", policy => policy.RequireRole("System Admin", "Admin", "Employee")); // Add all user roles to this policy.

    // Maintenance Pages:
    options.AddPolicy("MaintainCompaniesPolicy", policy => policy.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainFilesPolicy", policy => policy.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainCollectionsPolicy", policy => policy.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainSystemInfoPolicy", policy => policy.RequireRole("System Admin", "Admin"));
    options.AddPolicy("MaintainUsersPolicy", policy => policy.RequireRole("System Admin", "Admin"));
});

// This is the link between authorization groups and folders (OR pages).
builder.Services.AddRazorPages(options =>
{
    // Folder Permissions:
    // User Roles:
    options.Conventions.AuthorizeFolder("/Admins", "AdminsPolicy");
    options.Conventions.AuthorizeFolder("/Employees", "EmployeePolicy");
    options.Conventions.AuthorizeFolder("/SystemAdmins", "SystemAdminsPolicy");

    // Common and Home:
    options.Conventions.AuthorizeFolder("/Common", "CommonPolicy");
    options.Conventions.AllowAnonymousToFolder("/Home");

    // Maintenance Pages:
    options.Conventions.AuthorizeFolder("/MaintainCompanies", "MaintainCompaniesPolicy");
    options.Conventions.AuthorizeFolder("/MaintainFiles", "MaintainFilesPolicy");
    options.Conventions.AuthorizeFolder("/MaintainCollections", "MaintainCollectionsPolicy");
    options.Conventions.AuthorizeFolder("/MaintainSystemInfo", "MaintainSystemInfoPolicy");
    options.Conventions.AuthorizeFolder("/MaintainUsers", "MaintainUsersPolicy");
});

// Set session cookie name based on environment
string sessionCookieName = env.IsDevelopment()
    ? ".SearchAFile.Development.Session"
    : ".SearchAFile.Session";

builder.Services.AddSession(options =>
{
    options.Cookie.Name = sessionCookieName;
    options.IdleTimeout = TimeSpan.FromMinutes(720);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// This is to be able to access the httpcontext in the account controller.
builder.Services.AddHttpContextAccessor();

// This is for AppContext used in the SystemFunction.css file.
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add for email sending
builder.Services.AddTransient<IEmailService, EmailServiceMailKit>();

// Add for SMS sending
builder.Services.AddTransient<ISMSService, SMSServiceAzure>();

// Register the Login Service.
builder.Services.AddScoped<AuthClient>();

// Register the authenticated api client. 
builder.Services.AddHttpClient<AuthenticatedApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
});

// Register the client factory with the OpenAI service.
builder.Services.AddHttpClient("SearchAFIleClient", client =>
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", builder.Configuration["OpenAI:APIKey"]);
    client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
});

// Register the OpenAI service.
builder.Services.AddScoped<OpenAIFileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for Companyion scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Default}/{id?}");

    endpoints.MapRazorPages();
});

// This is for AppContext used in the SystemFunction.cs file.
HttpContextHelper.Services = app.Services;

app.Run();
