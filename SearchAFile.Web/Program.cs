using Microsoft.EntityFrameworkCore;
using SearchAFile.Web.Models;
using SearchAFile.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add the database contexts. // *** DELETE THIS!! ***
builder.Services.AddDbContext<SearchAFileDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SearchAFileConnection")));

// Add services to the container.
builder.Services.AddRazorPages();

// Register the authenticated api client. 
builder.Services.AddHttpClient<AuthenticatedApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
