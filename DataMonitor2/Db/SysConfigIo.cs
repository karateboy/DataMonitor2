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
            var ret = connection.QueryFirstOrDefault<string>("SELECT Value  FROM [dbo].[SysConfig] WHERE ConfigKey = @ConfigKey",
                new { ConfigKey = configKey });
            return ret ?? defaultValue;
        }catch(Exception e)
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
    
    private const string AMinAlarmRuleKey = "AMinAlarmRuleKey";
    public Task SetAMinAlarmRule(AlarmRule rule)
    {
        return UpsertSysConfig(
            new SysConfig(AMinAlarmRuleKey, JsonSerializer.Serialize(rule)));
    }
    
    public Task<AlarmRule?> GetAMinAlarmRule()
    {
        var defaultJson = JsonSerializer.Serialize(AMinAlarmRule.DefaultRule);
        return GetSysConfig(AMinAlarmRuleKey, defaultJson)
            .ContinueWith(ret=>JsonSerializer.Deserialize<AlarmRule>(ret.Result));
    }
    
    private const string WMinAlarmRuleKey = "WMinAlarmRuleKey";
    public Task SetWMinAlarmRule(AlarmRule rule)
    {
        return UpsertSysConfig(
            new SysConfig(WMinAlarmRuleKey, JsonSerializer.Serialize(rule)));
    }
    
    public Task<AlarmRule?> GetWMinAlarmRule()
    {
        var defaultJson = JsonSerializer.Serialize(WMinAlarmRule.DefaultRule);
        return GetSysConfig(WMinAlarmRuleKey, defaultJson)
            .ContinueWith(ret=>JsonSerializer.Deserialize<AlarmRule>(ret.Result));
    }
    
    private const string SHourAlarmRuleKey = "SHourAlarmRuleKey";
    public Task SetSHourAlarmRule(AlarmRule rule)
    {
        return UpsertSysConfig(
            new SysConfig(SHourAlarmRuleKey, JsonSerializer.Serialize(rule)));
    }
    
    public Task<AlarmRule?> GetSHourAlarmRule()
    {
        var defaultJson = JsonSerializer.Serialize(SHourAlarmRule.DefaultRule);
        return GetSysConfig(SHourAlarmRuleKey, defaultJson)
            .ContinueWith(ret=>JsonSerializer.Deserialize<AlarmRule>(ret.Result));
    }
    
    private const string CMinAlarmRuleKey = "CMinAlarmRuleKey";
    public Task SetCMinAlarmRule(AlarmRule rule)
    {
        return UpsertSysConfig(
            new SysConfig(CMinAlarmRuleKey, JsonSerializer.Serialize(rule)));
    }
    
    public Task<AlarmRule?> GetCMinAlarmRule()
    {
        var defaultJson = JsonSerializer.Serialize(CMinAlarmRule.DefaultRule);
        return GetSysConfig(CMinAlarmRuleKey, defaultJson)
            .ContinueWith(ret=>JsonSerializer.Deserialize<AlarmRule>(ret.Result));
    }
    
    private const string AMinSkipKey = "AMinSkipKey";
    public Task SetAMinSkip(int skip) => UpsertSysConfig(new SysConfig(AMinSkipKey, skip.ToString()));
    public Task<int> GetAMinSkip()=>GetSysConfig(AMinSkipKey, "0").ContinueWith(ret=>int.Parse(ret.Result));
    
    private const string WMinSkipKey = "WMinSkipKey";
    public Task SetWMinSkip(int skip) => UpsertSysConfig(new SysConfig(WMinSkipKey, skip.ToString()));
    public Task<int> GetWMinSkip()=>GetSysConfig(WMinSkipKey, "0").ContinueWith(ret=>int.Parse(ret.Result));
    
    private const string CMinSkipKey = "CMinSkipKey";
    public Task SetCMinSkip(int skip) => UpsertSysConfig(new SysConfig(CMinSkipKey, skip.ToString()));
    public Task<int> GetCMinSkip()=>GetSysConfig(CMinSkipKey, "0").ContinueWith(ret=>int.Parse(ret.Result));

    private const string SHourSkipKey = "SHourSkipKey";
    public Task SetSHourSkip(int skip) => UpsertSysConfig(new SysConfig(SHourSkipKey, skip.ToString()));
    public Task<int> GetSHourSkip()=>GetSysConfig(SHourSkipKey, "0").ContinueWith(ret=>int.Parse(ret.Result));

}