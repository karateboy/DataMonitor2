namespace DataMonitor2.Models;

public static class SHourAlarmRule
{
    public static AlarmRule Factory(string monitor) => new AlarmRule(monitor)
    {
        Rules =
        [
            new MonitorTypeRule("S220"),
            new MonitorTypeRule("S221"),
            new MonitorTypeRule("S222"),
            new MonitorTypeRule("S223"),
            new MonitorTypeRule("S224"),
            new MonitorTypeRule("S225"),
            new MonitorTypeRule("S226"),
        ]
    };
}