namespace DataMonitor2.Models;

public static class WMinAlarmRule
{
    public static AlarmRule DefaultRule = new AlarmRule
    {
        Monitors = ["W001", "W002"],
        Rules =
        [
            new MonitorTypeRule("W240"),
            new MonitorTypeRule("W241"),
            new MonitorTypeRule("W242"),
            new MonitorTypeRule("W243"),
            new MonitorTypeRule("W244"),
            new MonitorTypeRule("W246"),
            new MonitorTypeRule("W247"),
            new MonitorTypeRule("W248"),
            new MonitorTypeRule("W249"),
            new MonitorTypeRule("W251"),
            new MonitorTypeRule("W252"),
            new MonitorTypeRule("W253"),
            new MonitorTypeRule("W254"),
            new MonitorTypeRule("W255"),
            new MonitorTypeRule("W256"),
            new MonitorTypeRule("W257"),
        ]
    };
}