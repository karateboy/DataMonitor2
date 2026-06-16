namespace DataMonitor2.Models;

public record MonitorTypeRule(
    string Item,
    double? LAlarm = null,
    double? HAlarm = null,
    double? HhAlarm = null);

public class AlarmRule
{
    public required List<string> Monitors { get; set; }
    public required List<MonitorTypeRule> Rules { get; set; }
}