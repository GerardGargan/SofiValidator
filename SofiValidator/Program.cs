using System.Globalization;
using System.Net.Http.Headers;
using CsvHelper;
using SofiValidator;

DotNetEnv.Env.TraversePath().Load();
var apiUrl = "https://huhtamaki.cs.spheracloud.net/api/rc/flat-table/2127";
var apiToken = Environment.GetEnvironmentVariable("KEY") ?? throw new Exception("API Key not found");
    
Console.WriteLine("Loading data.. Please wait...");
var records = await ReadDataFromApi(apiUrl, apiToken);

static DateTime MonthKey(DateTime dt) => new(dt.Year, dt.Month, 1);

var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
var prevMonth1 = currentMonth.AddMonths(-1);
var prevMonth2 = currentMonth.AddMonths(-2);

var recordIndex = records.ToDictionary(
    r => (r.SiteId, r.PositionId, Month: MonthKey(r.TermStart)), 
    r => r
    );

var currentMonthSofiRecords = records.Where(x => MonthKey(x.TermStart) == MonthKey(currentMonth))
    .ToList();

var menuItems = new Dictionary<int, MenuItem>
{
    { 1, new MenuItem("Display TRI Breakdown for current month", PrintTris)},
    { 2, new MenuItem("Display Missing Working Hrs for current month", PrintMissingWorkingHrs)},
    { 3, new MenuItem("Display LTH Analysis", PrintLtiAndLthMonthly)},
    { 4, new MenuItem("Clear Console", Console.Clear)},
    { 5, new MenuItem("Exit", () => Environment.Exit(0))}
};

while (true)
{
    Console.WriteLine();
    ShowMenu(menuItems);
    var choice = ReadMenuOption(menuItems);
    menuItems[choice].Action();
}

static void ShowMenu(Dictionary<int, MenuItem> menuItems)
{
    Console.WriteLine("Select a menu option");
    foreach (var item in menuItems)
    {
        Console.WriteLine($"{item.Key}: {item.Value.Name}");
    }
}

static int ReadMenuOption(Dictionary<int, MenuItem> menuItems)
{
    while (true)
    {
        var input = Console.ReadLine();
        if (int.TryParse(input, out var option) && menuItems.ContainsKey(option))
        {
            Console.WriteLine();
            return option;
        }
        Console.WriteLine($"Invalid option: {input}");
    }
}

static async Task<List<SofiRecord>> ReadDataFromApi(string apiUrl, string apiToken)
{
    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    var csvContent = await client.GetStringAsync(apiUrl);
    using var reader = new StringReader(csvContent);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    var records = csv.GetRecords<SofiRecord>().ToList();

    return records;
}


void PrintTris()
{
    var ltiRecords = currentMonthSofiRecords.Where(x => (x.PositionId == Position.LtiEmployee || x.PositionId == Position.LtiContingent) & x.Value > 0);
    var mtiRecords = currentMonthSofiRecords.Where(x => (x.PositionId == Position.MtiEmployee || x.PositionId == Position.MtiContingent) & x.Value > 0);

    Console.WriteLine($"{"Site", -40} {"LTIs", -5}");
    foreach (var r in ltiRecords)
    {
        Console.WriteLine($"{r.Site, -40} {r.Value, 5}");
    }
    
    Console.WriteLine();
    Console.WriteLine($"{"Site", -40} {"MTIs", -4}");
    foreach (var r in mtiRecords)
    {
        Console.WriteLine($"{r.Site, -40} {r.Value, 4}");
    }
}

void PrintMissingWorkingHrs()
{
    var missingHuhtamakiWorkingHrsRecords = currentMonthSofiRecords.Where(x => x.PositionId == Position.HuhtamakiWorkingHrs & x.Value == 0);
    var missingContingentWorkingHrsRecords = currentMonthSofiRecords.Where(x => x.PositionId == Position.ContingentWorkingHrs & x.Value == 0);

    Console.WriteLine($"Huhtamaki Employee working hrs - Displaying any sites that have 0 hrs for the current month");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -50} {MonthKey(prevMonth1).ToShortDateString(), -50} {$"Current Month ({MonthKey(currentMonth).ToShortDateString()})", -50} ");
    foreach (var r in missingHuhtamakiWorkingHrsRecords)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -50} {prevMonth1Value?.Value, -50} {r.Value, -50}");
    }
    
    Console.WriteLine();
    
    Console.WriteLine($"Contingent working hrs - Displaying any sites that have 0 hrs for the current month");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -50} {MonthKey(prevMonth1).ToShortDateString(), -50} {$"Current Month ({MonthKey(currentMonth).ToShortDateString()})", -50} ");
    foreach (var r in missingContingentWorkingHrsRecords)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -50} {prevMonth1Value?.Value, -50} {r.Value, -50}");
    }
}

void PrintLtiAndLthMonthly()
{
    var currentMonthEmployeeRecordsWithLth =
        currentMonthSofiRecords.Where(x => x.PositionId == Position.LostTimeHrsEmployee & x.Value > 0);
    var currentMonthContingentRecordsWithLth =currentMonthSofiRecords.Where(x => x.PositionId == Position.LostTimeHrsContingent & x.Value > 0);
    
    Console.WriteLine($"Employee LTH - Displaying any sites that have recorded hrs for the current month & showing past months");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -50} {MonthKey(prevMonth1).ToShortDateString(), -50} {$"Current month ({MonthKey(currentMonth).ToShortDateString()})", 50}");
    foreach (var r in currentMonthEmployeeRecordsWithLth)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -50} {prevMonth1Value?.Value, -50} {r.Value, -50}");
    }
    
    Console.WriteLine();
    Console.WriteLine($"Contingent LTH - Displaying any sites that have recorded hrs for the current month & showing past months");
    foreach (var r in currentMonthContingentRecordsWithLth)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -50} {prevMonth1Value?.Value, -50} {r.Value, -50}");
    }

}