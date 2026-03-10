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

ExportEtimeSchedules();

// Example: Process LAT Details
await ProcessLatDetailsAsync(businessLogic);

Console.WriteLine("SmartOps Activity Processing completed.");

/// <summary>
/// Example method demonstrating LAT Detail operations.
/// </summary>
static async Task ProcessLatDetailsAsync(SmartOpsBusinessLogic businessLogic)
{
    // Get all LAT details
    var allDetails = await businessLogic.GetAllLatDetailsAsync();
    Console.WriteLine($"Found {allDetails.Count} LAT details in the database.");

    // Example: Get LAT details for a specific client
    // var clientDetails = await businessLogic.GetLatDetailsByClientAsync("CLIENT1");
    // Console.WriteLine($"Found {clientDetails.Count} LAT details for CLIENT1.");

    // Example: Get LAT details for a date range
    // var today = DateOnly.FromDateTime(DateTime.Today);
    // var weekAgo = today.AddDays(-7);
    // var dateRangeDetails = await businessLogic.GetLatDetailsByDateRangeAsync(weekAgo, today);
    // Console.WriteLine($"Found {dateRangeDetails.Count} LAT details in the last 7 days.");

    // Example: Save a new LAT detail
    // var newDetail = new Latdetail
    // {
    //     ClientAbbr = "TEST",
    //     CampAbbr = "CAMP1",
    //     WorkGroup = "WG1",
    //     RequiredDate = DateOnly.FromDateTime(DateTime.Today),
    //     RequiredTime = new TimeOnly(9, 0),
    //     RequiredHours = 8
    // };
    // var savedDetail = await businessLogic.SaveLatDetailAsync(newDetail);
    // Console.WriteLine($"Saved LAT detail with ID: {savedDetail?.LatdetailId}");
}

static void ExportEtimeSchedules()
{
    // Pull schedule data for this week
    var today = DateTime.Today;
    var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Sunday
    var endOfWeek = startOfWeek.AddDays(7); // Next Sunday

    // Define export path for schedules. Add start and end dates to the filename for clarity.
    string exportPath = $"C:\\Code\\EtimeSchedules_{startOfWeek:yyyyMMdd}_{endOfWeek:yyyyMMdd}.json";

    //Get and Export schedules to json file.
    if (!EtimeBusinessLogic.GetAndExportSchedules(startOfWeek, endOfWeek, exportPath))
    {
        Console.WriteLine("Failed to export Etime schedules.");
    }

}