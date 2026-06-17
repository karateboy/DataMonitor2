using System.Collections.Concurrent;
using DataMonitor2.Db;

namespace DataMonitor2.Models;

public record MonitorSkip(string Monitor, int Skip);
public static class MonitorSkips
{
    public static readonly List<MonitorSkip> DefaultSkips =
    [
        new("A001", 0),
        new("A002", 0),
        new("A003", 0),
        new("W001", 0),
        new("W002", 0),
        new("C001", 0),
        new("S001", 0),
        new("S002", 0),
        new("S003", 0),
    ];

    public static ConcurrentDictionary<string, MonitorSkip> MonitorSkipMap = new();

    public static void Init(List<MonitorSkip> skips)
    {
        foreach (var skip in skips)
        {
            MonitorSkipMap.TryAdd(skip.Monitor, skip);
        }
    }
    
    public static void UpdateSkip(MonitorSkip newSkip, SysConfigIo sysConfigIo)
    {
        MonitorSkipMap[newSkip.Monitor] = newSkip;
        sysConfigIo.SetMonitorSkips(GetEntries());
    }
    
    private static List<MonitorSkip> GetEntries() => MonitorSkipMap.Values.ToList();
}