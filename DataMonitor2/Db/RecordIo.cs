using Dapper;
using Microsoft.Data.SqlClient;

namespace DataMonitor2.Db;

public class RecordIo
{
    private readonly ISqlServer _sqlServer;
    private readonly ILogger<RecordIo> _logger;

    public RecordIo(ISqlServer sqlServer, ILogger<RecordIo> logger)
    {
        _sqlServer = sqlServer;
        _logger = logger;
    }

    public record MonitorRecord(
        string CNO,
        string DP_NO,
        string ITEM,
        string M_Year,
        string M_Month,
        string M_Day,
        string M_Time,
        decimal M_Val,
        string Code2,
        string CHK)
    {
        public DateTime GetDateTime() => new DateTime(int.Parse(M_Year) + 1911,
            int.Parse(M_Month),
            int.Parse(M_Day),
            int.Parse(M_Time.Substring(0, 2)),
            int.Parse(M_Time.Substring(2, 2)),
            0);
    }

    public async Task<IEnumerable<MonitorRecord>> GetRecords(string monitor, DateTime today)
    {
        string GetTableName()
        {
            switch (monitor[0])
            {
                case 'A':
                case 'W':
                case 'C':
                    return $"{monitor[0]}_AVGR{today.Year - 1911}";
                case 'S':
                    return $"{monitor[0]}_AVGHR{today.Year - 1911}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitor), monitor, null);
            }
        }
        
        var tableName = GetTableName();
        
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);    
        return await connection.QueryAsync<MonitorRecord>(
            $"SELECT * FROM {tableName} " +
            $"WHERE DP_NO = '{monitor}' " +
            $"AND M_MONTH = {today.Month} AND M_DAY = {today.Day} " +
            $"ORDER BY M_TIME ASC");        
    }
}