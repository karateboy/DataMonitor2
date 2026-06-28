using System.Collections.Concurrent;
using DataMonitor2.Db;

namespace DataMonitor2.Models;

public class MonitorTypeRule(string item)
{
    public string Item {get; set;} = item;
    public double? AlarmLow {get; set;}
    public double? AlarmHigh {get; set;}
    public int? ConstantCount {get; set;}
    public double? EfficiencyLowAlarm {get; set;} = 75;
    
    public bool AlarmRaised {get; set;}
    private bool Notified {get; set;}
    private DateTime NotificationTime {get; set;} = DateTime.MinValue;

    public bool SkipAlarm()
    {
        if(NotificationTime.AddHours(2)>DateTime.Now)
            return AlarmRaised && Notified;
        
        Notified = false;
        return false;
    } 
    
    public void AckNotification()
    {
        Notified = true;
        NotificationTime = DateTime.Now;
    }

    public bool NotificationNeeded() => AlarmRaised && !SkipAlarm();

}


public class AlarmRule(string monitor)
{
    public string Monitor { get; set; } = monitor;
    public required List<MonitorTypeRule> Rules { get; init; }
    
    public bool CheckCommunication { get; set; }

    public bool CheckMonitorTypeRules { get; set; } = true;
}

public static class MonitorAlarmRules
{
    public static readonly List<string> Monitors = ["A001","A002","A003","W001","W002","C001","S001","S002","S003"] ;
    
    public static readonly List<AlarmRule> DefaultRules =
    [
        AMinAlarmRule.Factory("A001"),
        AMinAlarmRule.Factory("A002"),
        AMinAlarmRule.Factory("A003"),
        WMinAlarmRule.Factory("W001"),
        WMinAlarmRule.Factory("W002"),
        CMinAlarmRule.Factory("C001"),
        SHourAlarmRule.Factory("S001"),
        SHourAlarmRule.Factory("S002"),
        SHourAlarmRule.Factory("S003"),
    ];

    public static readonly ConcurrentDictionary<string, AlarmRule> MonitorAlarmRuleMap = new();

    public static void Init(List<AlarmRule> rules)
    {
        foreach (var rule in rules)
        {
            MonitorAlarmRuleMap.TryAdd(rule.Monitor, rule);
        }
    }
    
    public static void UpdateAlarmRule(AlarmRule newRule, SysConfigIo sysConfigIo)
    {
        MonitorAlarmRuleMap[newRule.Monitor] = newRule;
        sysConfigIo.SetMonitorAlarmRules(GetEntries());
    }
    
    private static List<AlarmRule> GetEntries() => MonitorAlarmRuleMap.Values.ToList();
}