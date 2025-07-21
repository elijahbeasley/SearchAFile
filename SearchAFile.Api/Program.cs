using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Interfaces;
using SearchAFile.Infrastructure.Data;
using SearchAFile.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add the database contexts.
builder.Services.AddDbContext<SearchAFileDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SearchAFileConnection")));

// Add Services.
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileGroupService, FileGroupService>();
builder.Services.AddScoped<ISystemInfoService, SystemInfoService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
