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
    { 4, new MenuItem("LTI Information", PrintLtiInformation)},
    { 5, new MenuItem("MTI Information", PrintMtiInformation)},
    { 6, new MenuItem("Injury Ratings", PrintInjuryRatings)},
    { 7, new MenuItem("Clear Console", Console.Clear)},
    { 8, new MenuItem("Exit", () => Environment.Exit(0))}
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

    if (records.Count == 0) throw new Exception("No records found");
    
    return records;
}


void PrintTris()
{
    var ltiRecords = currentMonthSofiRecords.Where(x => (x.PositionId == Position.LtiEmployee || x.PositionId == Position.LtiContingent) & x.Value > 0).ToList();
    var mtiRecords = currentMonthSofiRecords.Where(x => (x.PositionId == Position.MtiEmployee || x.PositionId == Position.MtiContingent) & x.Value > 0).ToList();
    
    var ltiTotal = ltiRecords.Select(x => x.Value).Sum();
    var mtiTotal = mtiRecords.Select(x => x.Value).Sum();
    
    Console.WriteLine($"{"Site", -40} {"LTIs", -5}");
    foreach (var r in ltiRecords)
    {
        Console.WriteLine($"{r.Site, -40} {r.Value, 5}");
    }
    Console.WriteLine($"{"Total", -40} {ltiTotal, 5}");
    
    Console.WriteLine();
    Console.WriteLine($"{"Site", -40} {"MTIs", -5}");
    foreach (var r in mtiRecords)
    {
        Console.WriteLine($"{r.Site, -40} {r.Value, 5}");
    }

    Console.WriteLine();
    Console.WriteLine($"{"Total LTIs", -40} {mtiTotal, 5}");
    Console.WriteLine($"{"Total MTIs", -40} {ltiTotal, 5}");
    Console.WriteLine($"{"Total TRIs", -40} {ltiTotal + mtiTotal, 5}");

}

void PrintMissingWorkingHrs()
{
    var missingHuhtamakiWorkingHrsRecords = currentMonthSofiRecords.Where(x => x.PositionId == Position.HuhtamakiWorkingHrs & x.Value == 0);
    var missingContingentWorkingHrsRecords = currentMonthSofiRecords.Where(x => x.PositionId == Position.ContingentWorkingHrs & x.Value == 0);

    Console.WriteLine($"Huhtamaki Employee working hrs - Displaying any sites that have 0 hrs in the current month, and have recorded more than 0 hrs in the previous 2 months");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -20} {MonthKey(prevMonth1).ToShortDateString(), -20} {$"Current Month ({MonthKey(currentMonth).ToShortDateString()})", -20} ");
    foreach (var r in missingHuhtamakiWorkingHrsRecords)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        if (prevMonth1Value?.Value == 0 && prevMonth2Value?.Value == 0) continue;
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -20} {prevMonth1Value?.Value, -20} {r.Value, -20}");
    }
    
    Console.WriteLine();
    
    Console.WriteLine($"Contingent working hrs - Displaying any sites that have 0 hrs in the current month, and have recorded more than 0 hrs in the previous 2 months");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -20} {MonthKey(prevMonth1).ToShortDateString(), -20} {$"Current Month ({MonthKey(currentMonth).ToShortDateString()})", -20} ");
    foreach (var r in missingContingentWorkingHrsRecords)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        if (prevMonth1Value?.Value == 0 && prevMonth2Value?.Value == 0) continue;
        Console.WriteLine($"{r.Site, -40} {prevMonth2Value?.Value, -20} {prevMonth1Value?.Value, -20} {r.Value, -20}");
    }
}

void PrintLtiAndLthMonthly()
{
    var currentMonthEmployeeRecordsWithLth =
        currentMonthSofiRecords.Where(x => (x.PositionId == Position.LostTimeHrsEmployee & x.Value > 0));
    var currentMonthContingentRecordsWithLth =currentMonthSofiRecords.Where(x => x.PositionId == Position.LostTimeHrsContingent & x.Value > 0);
    
    Console.WriteLine($"Employee LTH - Displaying any sites that have recorded hrs for the current month & showing past months");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -20} {MonthKey(prevMonth1).ToShortDateString(), -20} {$"Current month ({MonthKey(currentMonth).ToShortDateString()})", -20}");
    Console.WriteLine($"{"", -40} {"LTI", -8}{"LTH", -12} {"LTI", -8}{"LTH", -12} {"LTI", -8}{"LTH", -12}");

    foreach (var r in currentMonthEmployeeRecordsWithLth)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        recordIndex.TryGetValue((r.SiteId, Position.LtiEmployee, MonthKey(currentMonth)), out var currentMonthLti);
        recordIndex.TryGetValue((r.SiteId, Position.LtiEmployee, MonthKey(prevMonth1)), out var prevMonth1Lti);
        recordIndex.TryGetValue((r.SiteId, Position.LtiEmployee, MonthKey(prevMonth2)), out var prevMonth2Lti);

        
        Console.WriteLine($"{r.Site, -40} {prevMonth2Lti?.Value, -8}{prevMonth2Value?.Value, -12} {prevMonth1Lti?.Value, -8}{prevMonth1Value?.Value, -12} {currentMonthLti?.Value, -8}{r.Value, -12}");
    }
    
    Console.WriteLine();
    Console.WriteLine($"Contingent LTH - Displaying any sites that have recorded hrs for the current month & showing past months");
    Console.WriteLine($"{"", -40} {"LTI", -8}{"LTH", -12} {"LTI", -8}{"LTH", -12} {"LTI", -8}{"LTH", -12}");
    Console.WriteLine($"{"Site", -40} {MonthKey(prevMonth2).ToShortDateString(), -20} {MonthKey(prevMonth1).ToShortDateString(), -20} {$"Current month ({MonthKey(currentMonth).ToShortDateString()})", -20}");
    foreach (var r in currentMonthContingentRecordsWithLth)
    {
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth1)), out var prevMonth1Value);
        recordIndex.TryGetValue((r.SiteId, r.PositionId, MonthKey(prevMonth2)), out var prevMonth2Value);
        recordIndex.TryGetValue((r.SiteId, Position.LtiContingent, MonthKey(currentMonth)), out var currentMonthLti);
        recordIndex.TryGetValue((r.SiteId, Position.LtiContingent, MonthKey(prevMonth1)), out var prevMonth1Lti);
        recordIndex.TryGetValue((r.SiteId, Position.LtiContingent, MonthKey(prevMonth2)), out var prevMonth2Lti);
        Console.WriteLine($"{r.Site, -40} {prevMonth2Lti?.Value, -8}{prevMonth2Value?.Value, -12} {prevMonth1Lti?.Value, -8}{prevMonth1Value?.Value, -12} {currentMonthLti?.Value, -8}{r.Value, -12}");
    }
}

void PrintLtiInformation()
{
    Console.WriteLine($"Lti Information");
    var siteIds = currentMonthSofiRecords.Select(x => x.SiteId).ToHashSet();

    Console.WriteLine($"{"Site", -40} {"LTI Total", -10} {"Class (duration) Of Lti", -25} {"Cause of LTI", -15} {"Type Of Injury", -20} {"Injured Body Part", -20} {"Age Group", -10} {"Location", -10} {"Service Age", -15} {"Shift Type", -15}");
    foreach (var siteId in siteIds)
    {
        recordIndex.TryGetValue((siteId, Position.LtiEmployee, MonthKey(currentMonth)), out var ltiEmployee);
        recordIndex.TryGetValue((siteId, Position.LtiContingent, MonthKey(currentMonth)), out var ltiContingent);
        var totalLti = (ltiEmployee?.Value ?? 0) + (ltiContingent?.Value ?? 0);
        
        recordIndex.TryGetValue((siteId, Position.DurationOfLti, MonthKey(currentMonth)), out var durationOfLti);
        recordIndex.TryGetValue((siteId, Position.CauseOfLti, MonthKey(currentMonth)), out var causeOfLti);
        recordIndex.TryGetValue((siteId, Position.TypeOfInjuryLti, MonthKey(currentMonth)), out var typeOfInjury);
        recordIndex.TryGetValue((siteId, Position.InjuredBodyPartLti, MonthKey(currentMonth)), out var injuredBodyPartLti);
        recordIndex.TryGetValue((siteId, Position.AgeGroupLti, MonthKey(currentMonth)), out var ageGroupLti);
        recordIndex.TryGetValue((siteId, Position.LocationLti, MonthKey(currentMonth)), out var locationLti);
        recordIndex.TryGetValue((siteId, Position.ServiceAgeLti, MonthKey(currentMonth)), out var serviceAgeLti);
        recordIndex.TryGetValue((siteId, Position.ShiftTypeLti, MonthKey(currentMonth)), out var shiftTypeLti);
        var siteName = ltiEmployee?.Site;
        
        var totalInfoAdded = new[] { durationOfLti, causeOfLti, typeOfInjury, injuredBodyPartLti,
                ageGroupLti, locationLti, serviceAgeLti, shiftTypeLti }
            .Sum(x => x?.Value ?? 0);
        var isValid = Math.Abs(totalInfoAdded - (totalLti * 8.0m)) == 0;

        if (!isValid)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (isValid && totalLti > 0)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.WriteLine($"{siteName, -40} {totalLti, -10} {durationOfLti?.Value, -25} {causeOfLti?.Value, -15} {typeOfInjury?.Value, -20} {injuredBodyPartLti?.Value, -20} {ageGroupLti?.Value, -10} {locationLti?.Value, -10} {serviceAgeLti?.Value, -15} {shiftTypeLti?.Value, -15}");
        Console.ResetColor();
    }
}

void PrintMtiInformation()
{
    Console.WriteLine($"Mti Information");
    var siteIds = currentMonthSofiRecords.Select(x => x.SiteId).ToHashSet();

    Console.WriteLine($"{"Site", -40} {"Mti Total", -10} {"Cause of Mti", -15} {"Type Of Injury", -20} {"Injured Body Part", -20} {"Age Group", -10} {"Location", -10} {"Service Age", -15} {"Shift Type", -15}");
    foreach (var siteId in siteIds)
    {
        recordIndex.TryGetValue((siteId, Position.MtiEmployee, MonthKey(currentMonth)), out var mtiEmployee);
        recordIndex.TryGetValue((siteId, Position.MtiContingent, MonthKey(currentMonth)), out var mtiContingent);
        var totalMti = (mtiEmployee?.Value ?? 0) + (mtiContingent?.Value ?? 0);

        recordIndex.TryGetValue((siteId, Position.CauseOfMti, MonthKey(currentMonth)), out var causeOfMti);
        recordIndex.TryGetValue((siteId, Position.TypeOfInjuryMti, MonthKey(currentMonth)), out var typeOfInjury);
        recordIndex.TryGetValue((siteId, Position.InjuredBodyPartMti, MonthKey(currentMonth)),
            out var injuredBodyPartMti);
        recordIndex.TryGetValue((siteId, Position.AgeGroupMti, MonthKey(currentMonth)), out var ageGroupMti);
        recordIndex.TryGetValue((siteId, Position.LocationMti, MonthKey(currentMonth)), out var locationMti);
        recordIndex.TryGetValue((siteId, Position.ServiceAgeMti, MonthKey(currentMonth)), out var serviceAgeMti);
        recordIndex.TryGetValue((siteId, Position.ShiftTypeMti, MonthKey(currentMonth)), out var shiftTypeMti);
        var siteName = mtiEmployee?.Site;

        var totalInfoAdded = new[]
            {
                causeOfMti, typeOfInjury, injuredBodyPartMti,
                ageGroupMti, locationMti, serviceAgeMti, shiftTypeMti
            }
            .Sum(x => x?.Value ?? 0);
        var isValid = Math.Abs(totalInfoAdded - (totalMti * 7.0m)) == 0;

        if (!isValid)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (isValid && totalMti > 0)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.WriteLine(
            $"{siteName,-40} {totalMti,-10} {causeOfMti?.Value,-15} {typeOfInjury?.Value,-20} {injuredBodyPartMti?.Value,-20} {ageGroupMti?.Value,-10} {locationMti?.Value,-10} {serviceAgeMti?.Value,-15} {shiftTypeMti?.Value,-15}");
        Console.ResetColor();
    }
}

void PrintInjuryRatings()
    {
        var siteNameLookup = currentMonthSofiRecords.DistinctBy(x => x.SiteId).ToDictionary(x => x.SiteId, x => x.Site);
        
        Console.WriteLine($"{"Site", -40} {"# LTI", -7} {"Actual LTI Ratings", -20} {"Potential LTI Ratings", -25} | {"# MTI", -7} {"Actual MTI Ratings", -20} {"Potential MTI Ratings", -25} | {"# Total", -8} {"Actual FAI Ratings", -20} {"Potential FAI Ratings", -23}");

        foreach (var siteId in siteNameLookup.Keys)
        {
            bool siteNameFound = siteNameLookup.TryGetValue(siteId, out var siteName);
            siteName = siteNameFound ? siteName : "";
            var totalLti = GetSitePositionValue(siteId, Position.LtiContingent, MonthKey(currentMonth)) + GetSitePositionValue(siteId, Position.LtiEmployee, MonthKey(currentMonth));
            var totalMti = GetSitePositionValue(siteId, Position.MtiContingent, MonthKey(currentMonth)) + GetSitePositionValue(siteId, Position.MtiEmployee, MonthKey(currentMonth));
            var totalFai = GetSitePositionValue(siteId, Position.FaiTotal, MonthKey(currentMonth)) + GetSitePositionValue(siteId, Position.LtiEmployee, MonthKey(currentMonth));
            
            var actualLtiRatings = GetSitePositionValue(siteId, Position.ActualRatingsLti, MonthKey(currentMonth));
            var actualMtiRatings = GetSitePositionValue(siteId, Position.ActualRatingsMti, MonthKey(currentMonth));
            var actualFaiRatings = GetSitePositionValue(siteId, Position.ActualRatingsFai, MonthKey(currentMonth));
            
            var potentialLtiRatings = GetSitePositionValue(siteId, Position.PotentialRatingsLti, MonthKey(currentMonth));
            var potentialMtiRatings = GetSitePositionValue(siteId, Position.PotentialRatingsMti, MonthKey(currentMonth));
            var potentialFaiRatings = GetSitePositionValue(siteId, Position.PotentialRatingsFai, MonthKey(currentMonth));

            var isLtiMatchingRatings = (totalLti * 2) == (actualLtiRatings + potentialLtiRatings);
            var isMtiMatchingRatings = (totalMti * 2) ==  (actualMtiRatings + potentialMtiRatings);
            var isFaiMatchingRatings =  (totalFai * 2) == (actualFaiRatings + potentialFaiRatings);
            var isValid = isLtiMatchingRatings && isMtiMatchingRatings && isFaiMatchingRatings;
            
            if (!isValid)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
            } 
            Console.WriteLine($"{siteName, -40} {totalLti, -7
            } {actualLtiRatings, -20} {potentialLtiRatings, -25} | {totalMti, -7} {actualMtiRatings, -20} {potentialMtiRatings, -25} | {totalFai, -8} {actualFaiRatings, -20} {potentialFaiRatings, -23}");
            Console.ResetColor();
        }
    }
    
    decimal GetSitePositionValue(int siteId, int position, DateTime month)
    {
        var found = recordIndex.TryGetValue((siteId, position, MonthKey(month)), out var val);
        return val?.Value ?? 0;
    }