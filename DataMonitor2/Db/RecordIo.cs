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

    public async Task<IEnumerable<MonitorRecord>> GetAMinRecords(DateTime today)
    {
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);
        return await connection.QueryAsync<MonitorRecord>(
            $"SELECT * FROM A_AVGR{today.Year - 1911} " +
            $"WHERE M_YEAR = {today.Year - 1911} AND M_MONTH = {today.Month} AND M_DAY = {today.Day}");
    }

    public async Task<IEnumerable<MonitorRecord>> GetWMinRecords(DateTime today)
    {
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);
        return await connection.QueryAsync<MonitorRecord>(
            $"SELECT * FROM W_AVGR{today.Year - 1911} " +
            $"WHERE M_YEAR = {today.Year - 1911} AND M_MONTH = {today.Month} AND M_DAY = {today.Day} ORDER BY M_TIME ASC");
    }

    public async Task<IEnumerable<MonitorRecord>> GetSHourRecords(DateTime today)
    {
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);
        return await connection.QueryAsync<MonitorRecord>(
            $"SELECT * FROM S_AVGHR{today.Year - 1911} " +
            $"WHERE M_YEAR = {today.Year - 1911} AND M_MONTH = {today.Month} AND M_DAY = {today.Day} ORDER BY M_TIME ASC");
    }

    public async Task<IEnumerable<MonitorRecord>> GetCMinRecords(DateTime today)
    {
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);
        return await connection.QueryAsync<MonitorRecord>(
            $"SELECT * FROM C_AVGR{today.Year - 1911} " +
            $"WHERE M_YEAR = {today.Year - 1911} AND M_MONTH = {today.Month} AND M_DAY = {today.Day} ORDER BY M_TIME ASC");
    }
}