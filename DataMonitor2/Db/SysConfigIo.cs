using System.Globalization;
using System.Text.Json;
using Dapper;
using DataMonitor2.Models;
using Microsoft.Data.SqlClient;

namespace DataMonitor2.Db;

public class SysConfigIo
{
    private readonly ISqlServer _sqlServer;
    private readonly ILogger<SysConfigIo> _logger;

    public SysConfigIo(ISqlServer sqlServer, ILogger<SysConfigIo> logger)
    {
        _sqlServer = sqlServer;
        _logger = logger;
    }


    public interface ISysConfig
    {
        string ConfigKey { get; init; }
        string Value { get; init; }
    }

    public record SysConfig(string ConfigKey, string Value) : ISysConfig;

    public async Task UpsertSysConfig(ISysConfig sysConfig)
    {
        try
        {
            await using var connection = new SqlConnection(_sqlServer.ConnectionString);
            await connection.ExecuteAsync(@"
                MERGE into [dbo].[SysConfig] AS target 
                USING (SELECT @ConfigKey AS ConfigKey) AS source
                ON (target.ConfigKey = source.ConfigKey)
                WHEN MATCHED THEN
                    UPDATE SET                        
                        Value = @value
                WHEN NOT MATCHED THEN
                    INSERT (ConfigKey, Value)
                    VALUES (@ConfigKey, @Value);                                
                ", sysConfig);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpsertSysConfig");
            throw;
        }
    }

    public async Task<string> GetSysConfig(string configKey, string defaultValue = "")
    {
        try
        {
            await using var connection = new SqlConnection(_sqlServer.ConnectionString);
            var ret = connection.QueryFirstOrDefault<string>(
                "SELECT Value  FROM [dbo].[SysConfig] WHERE ConfigKey = @ConfigKey",
                new { ConfigKey = configKey });
            return ret ?? defaultValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSysConfig {ConfigKey}", configKey);
            throw;
        }
    }

    private async Task<bool> GetSysConfigBool(string configKey)
    {
        var ret = await GetSysConfig(configKey);
        return string.IsNullOrEmpty(ret) || bool.Parse(ret);
    }

    private const string EmailReceiptKey = "EmailReceiptKey";

    public Task SetEmailReceipt(string email)
    {
        return UpsertSysConfig(
            new SysConfig(EmailReceiptKey, email));
    }

    public Task<string> GetEmailReceipt()
    {
        return GetSysConfig(EmailReceiptKey);
    }

    private const string PhoneNoKey = "PhoneNoKey";

    public Task SetPhoneNo(string email)
    {
        return UpsertSysConfig(
            new SysConfig(PhoneNoKey, email));
    }

    public Task<string> GetPhoneNo()
    {
        return GetSysConfig(PhoneNoKey);
    }

    public const string MonitorAlarmRulesKey = "MonitorAlarmRulesKey";
    public Task SetMonitorAlarmRules(List<AlarmRule> rules) =>
        UpsertSysConfig(new SysConfig(MonitorAlarmRulesKey, JsonSerializer.Serialize(rules)));

    public Task<List<AlarmRule>> GetMonitorAlarmRules() =>
        GetSysConfig(MonitorAlarmRulesKey, JsonSerializer.Serialize(MonitorAlarmRules.DefaultRules))
            .ContinueWith(ret => JsonSerializer.Deserialize<List<AlarmRule>>(ret.Result))!;
    
    public const string MonitorSkipsKey = "MonitorSkipsKey";
    public Task SetMonitorSkips(List<MonitorSkip> skips) =>
        UpsertSysConfig(new SysConfig(MonitorSkipsKey, JsonSerializer.Serialize(skips)));
    public Task<List<MonitorSkip>> GetMonitorSkips() =>
        GetSysConfig(MonitorSkipsKey, JsonSerializer.Serialize(MonitorSkips.DefaultSkips))
            .ContinueWith(ret => JsonSerializer.Deserialize<List<MonitorSkip>>(ret.Result))!;
}