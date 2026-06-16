namespace DataMonitor2.Models;

public static class AMinAlarmRule
{
    public static AlarmRule DefaultRule = new()
    {
        Monitors = ["A001", "A002", "A003", "W001", "W002"],
        Rules =
        [
            new MonitorTypeRule("A222"),
            new MonitorTypeRule("A223"),
            new MonitorTypeRule("A224"),
            new MonitorTypeRule("A225"),
            new MonitorTypeRule("A226"),
            new MonitorTypeRule("A227"),
            new MonitorTypeRule("A228"),
            new MonitorTypeRule("A283"),
            new MonitorTypeRule("A286"),
            new MonitorTypeRule("A289"),
            new MonitorTypeRule("A293"),
            new MonitorTypeRule("A296"),
            new MonitorTypeRule("A923"),
            new MonitorTypeRule("A924"),
            new MonitorTypeRule("A925"),
            new MonitorTypeRule("A926")
        ]
    };
}