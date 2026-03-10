using Microsoft.Data.SqlClient;

namespace Etime.Bus;

public class ScheduleRecord
{
    public int ShiftCodeId { get; set; }
    public string PersonNum { get; set; } = string.Empty;
    public string? PayGroup { get; set; }
    public int? PayCodeId { get; set; }
    public string? PayCode { get; set; }
    public DateTime StartDtm { get; set; }
    public DateTime EndDtm { get; set; }
    public int BreakMin { get; set; }
}

public class EtimeBusinessLogic
{
    private const string ConnectionString = "Server=SvrPitSqlAdp;Database=SSO;Trusted_Connection=True;TrustServerCertificate=True;";

    public static List<ScheduleRecord> GetSchedules(DateTime shiftStartDate, DateTime shiftEndDate)
    {
        var results = new List<ScheduleRecord>();

        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand("dbo.GetSchedules", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@ShiftStartDate", shiftStartDate.Date);
        command.Parameters.AddWithValue("@ShiftEndDate", shiftEndDate.Date);

        connection.Open();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new ScheduleRecord
            {
                ShiftCodeId = reader.GetInt32(reader.GetOrdinal("SHIFTCODEID")),
                PersonNum = reader.GetString(reader.GetOrdinal("PERSONNUM")),
                PayGroup = reader.IsDBNull(reader.GetOrdinal("PayGroup")) ? null : reader.GetString(reader.GetOrdinal("PayGroup")),
                PayCodeId = reader.IsDBNull(reader.GetOrdinal("PAYCODEID")) ? null : reader.GetInt32(reader.GetOrdinal("PAYCODEID")),
                PayCode = reader.IsDBNull(reader.GetOrdinal("PayCode")) ? null : reader.GetString(reader.GetOrdinal("PayCode")),
                StartDtm = reader.GetDateTime(reader.GetOrdinal("STARTDTM")),
                EndDtm = reader.GetDateTime(reader.GetOrdinal("ENDDTM")),
                BreakMin = reader.GetInt32(reader.GetOrdinal("BreakMin"))
            });
        }

        return results;
    }
}
