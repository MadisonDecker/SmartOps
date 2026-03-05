namespace Workforce.Bus.Models;

public class User
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public string LoginId { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public bool Enabled { get; set; }
    public PersonName? Name { get; set; }
}
