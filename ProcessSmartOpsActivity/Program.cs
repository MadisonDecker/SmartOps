using Etime.Bus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartManagement.Repo.Models;
using SmartOpsManagement.Bus;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var connectionString = configuration.GetConnectionString("SmartOpsConnection");

var optionsBuilder = new DbContextOptionsBuilder<SmartOpsContext>();
optionsBuilder.UseSqlServer(connectionString);

using var context = new SmartOpsContext(optionsBuilder.Options);
var businessLogic = new SmartOpsBusinessLogic(context);

Console.WriteLine("SmartOps Activity Processing started.");

// Export and Import Etime schedules for this week
await ExportAndImportEtimeSchedules(businessLogic);

// Example: Process LAT Details
await ProcessLatDetailsAsync(businessLogic);

Console.WriteLine("SmartOps Activity Processing completed.");

/// <summary>
/// Exports Etime schedules to JSON and then imports them into the database.
/// </summary>
static async Task ExportAndImportEtimeSchedules(SmartOpsBusinessLogic businessLogic)
{
    // Pull schedule data for this week
    var today = DateTime.Today;
    var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Sunday
    var endOfWeek = startOfWeek.AddDays(7); // Next Sunday

    // Define export path for schedules
    string exportPath = $"D:\\Code\\EtimeSchedules_{startOfWeek:yyyyMMdd}_{endOfWeek:yyyyMMdd}.json";

    // Export schedules to json file
    Console.WriteLine($"Exporting Etime schedules from {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}...");
    if (!EtimeBusinessLogic.GetAndExportSchedules(startOfWeek, endOfWeek, exportPath))
    {
        Console.WriteLine("Failed to export Etime schedules.");
        return;
    }
    Console.WriteLine($"Exported schedules to {exportPath}");

    // Import schedules from json file to database
    Console.WriteLine($"Importing Etime schedules from {exportPath}...");
    var importedCount = await businessLogic.ImportEtimeSchedulesAsync(exportPath);
    if (importedCount >= 0)
    {
        Console.WriteLine($"Successfully imported {importedCount} Etime shifts to the database.");
    }
}

/// <summary>
/// Example method demonstrating LAT Detail operations.
/// </summary>
static async Task ProcessLatDetailsAsync(SmartOpsBusinessLogic businessLogic)
{
    // Get all LAT details
    var allDetails = await businessLogic.GetAllLatDetailsAsync();
    Console.WriteLine($"Found {allDetails.Count} LAT details in the database.");
}