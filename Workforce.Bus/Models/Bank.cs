namespace Workforce.Bus.Models;

public class Bank
{
    public string RecordId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public string DataChange { get; set; } = string.Empty;
    public string ExternalMatchId { get; set; } = string.Empty;
    public string BankId { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Name { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string Unit { get; set; } = string.Empty;
    public List<Balance> Balances { get; set; } = new();
}

public class Balance
{
    public DateOnly Date { get; set; }
    public decimal Balance_ { get; set; }
    public string? EmployeeJob { get; set; }
}
