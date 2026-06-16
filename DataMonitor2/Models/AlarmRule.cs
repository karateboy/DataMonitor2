namespace DataMonitor2.Models;

public class MonitorTypeRule(string item)
{
    public string Item {get; set;} = item;
    public double? AlarmLow {get; set;}
    public double? AlarmHigh {get; set;}
    public double? AlarmHighHigh {get; set;}
}


public class AlarmRule
{
    public required List<string> Monitors { get; set; }
    public required List<MonitorTypeRule> Rules { get; set; }
}