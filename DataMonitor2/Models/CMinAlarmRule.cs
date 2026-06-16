namespace DataMonitor2.Models;

public static class CMinAlarmRule
{
    public static AlarmRule DefaultRule = new AlarmRule
    {
        Monitors = ["C001"],
        Rules =
        [
            new MonitorTypeRule("C211"),
            new MonitorTypeRule("C212"),
            new MonitorTypeRule("C213"),
            new MonitorTypeRule("C214"),
            new MonitorTypeRule("C215"),
            new MonitorTypeRule("C216"),
        ]
    };
}