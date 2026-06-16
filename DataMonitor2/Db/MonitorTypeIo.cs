using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DataMonitor2.Db;

public class MonitorTypeIo
{
    private readonly ISqlServer _sqlServer;
    private readonly ILogger<MonitorTypeIo> _logger;

    public MonitorTypeIo(ISqlServer sqlServer, ILogger<MonitorTypeIo> logger)
    {
        _sqlServer = sqlServer;
        _logger = logger;
    }

    
    public interface IMonitorType
    {
        string Item { get; init; }
        string Desp { get; init; }
        string Unit { get; init; }
        
        string Remark { get; init; }
    }

    public record MonitorType(string Item, string Desp, string Unit, string Remark) : IMonitorType;
    
    private readonly Dictionary<string, MonitorType> _map=new();
    public IReadOnlyDictionary<string, MonitorType> MapReadOnly => _map;
    public async Task Init()
    {
        var monitorTypes = await GetMonitorTypeAsync();
        foreach (var monitorType in monitorTypes)
        {
            _map.Add(monitorType.Item, monitorType);
        }
        _logger.LogInformation("MonitorTypeIo init. map= {map}", _map.Count);
    }
    
    private async Task<IEnumerable<MonitorType>> GetMonitorTypeAsync()
    {
        await using var connection = new SqlConnection(_sqlServer.ConnectionString);
        return await connection.QueryAsync<MonitorType>("SELECT *  FROM [dbo].[code1]");
    }

}