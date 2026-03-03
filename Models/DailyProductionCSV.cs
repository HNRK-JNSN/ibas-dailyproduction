
using CsvHelper.Configuration.Attributes;

public class DailyProductionCSV
{
    [Name("PartitionKey")]
    public int Model { get; set; }

    [Name("RowKey")]
    public DateTime Date { get; set; }
    
    [Name("itemsProduced")]
    public int ItemsProduced { get; set; }
}