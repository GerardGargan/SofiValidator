namespace SofiValidator;
using CsvHelper.Configuration.Attributes;

public class SofiRecord
{
    [Name("Position")] public string Position { get; set; } = String.Empty;

    [Name("Position ID")]
    public int PositionID { get; set; }

    [Name("Site ID")]
    public int SiteID { get; set; }

    [Name("Site")]
    public string Site { get; set; } = String.Empty;

    [Name("Site path")]
    public string SitePath { get; set; } = String.Empty;

    [Name("Is Tag")]
    public bool IsTag { get; set; } 

    [Name("Term Start")]
    public DateTime TermStart { get; set; }

    [Name("Term End")]
    public DateTime TermEnd { get; set; }

    [Name("Unit")]
    public string Unit { get; set; } = String.Empty;

    [Name("Filter by custom field name")]
    public string FilterByCustomFieldName { get; set; } = String.Empty;

    [Name("Value")]
    public string Value { get; set; } = String.Empty;

    [Name("estimated")]
    public bool Estimated { get; set; }

    [Name("Calculated Time (UTC)")]
    public DateTime CalculatedTimeUTC { get; set; }

    [Name("Site Type")]
    public string SiteType { get; set; } = String.Empty;

    public override string ToString()
    {
        return $"{Position} - {PositionID} - {SiteType} - {SitePath}";
    }
}
