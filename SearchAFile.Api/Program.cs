using Microsoft.EntityFrameworkCore;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add the database contexts.
builder.Services.AddDbContext<SearchAFileDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SearchAFileConnection")));

// Add Services.
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ISystemInfoService, SystemInfoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ClientAuthMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
