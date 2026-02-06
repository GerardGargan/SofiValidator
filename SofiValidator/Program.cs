using System.Globalization;
using System.Net.Http.Headers;
using CsvHelper;
using SofiValidator;

DotNetEnv.Env.TraversePath().Load();
var apiUrl = "https://huhtamaki.cs.spheracloud.net/api/rc/flat-table/1642";
var apiToken = Environment.GetEnvironmentVariable("KEY") ?? "No key found";
    
Console.WriteLine("Loading data.. Please wait...");
List<SofiRecord> records = await ReadDataFromApi(apiUrl, apiToken);
var menuItems = new Dictionary<int, MenuItem>
{
    { 1, new MenuItem("Print data", PrintData) },
    { 2, new MenuItem("Exit", () => Environment.Exit(0) )}
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
    Console.WriteLine("Select a menu option");
    while (true)
    {
        var input = Console.ReadLine();
        if (int.TryParse(input, out var option) && menuItems.ContainsKey(option))
        {
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

void PrintData()
{
    for (int i = 0; i < 10; i++)
    {
        Console.WriteLine(records[i].ToString());
    }
}