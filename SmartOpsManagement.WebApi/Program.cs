using Microsoft.EntityFrameworkCore;
using SmartManagement.Repo.Models;
using SmartOpsManagement.Bus;
using SmartOpsManagement.WebApi.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SmartOpsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartOpsConnection")));

builder.Services.AddScoped<SmartOpsBusinessLogic>();

builder.Services.AddControllers();

builder.Services.AddOpenApi(); // Add this line to register OpenAPI services

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map minimal API endpoints
app.MapWeeklyStaffingMetricsEndpoints();
app.MapLineAdherenceEndpoints();
app.MapScheduleEndpoints();
app.MapTimeOffRequestEndpoints();

// Add this line to map OpenAPI endpoints
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Adds UI at /scalar/v1
}

app.Run();
