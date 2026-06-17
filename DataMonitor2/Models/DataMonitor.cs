using System.Text;
using DataMonitor2.Db;

namespace DataMonitor2.Models
{
    internal sealed class DataMonitor(
        ILogger<DataMonitor> logger,
        MonitorTypeIo monitorTypeIo,
        SysConfigIo sysConfigIo,
        RecordIo recordIo,
        ILineNotify lineNotify,
        AlarmIo alarmIo,
        IHostEnvironment env) : IHostedService
    {

        private string CheckMinRecords(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip)
            => CheckRecords(records, rule, skip, true);


        private string CheckHourRecords(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip)
            => CheckRecords(records, rule, skip, false);

        private string CheckRecords(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip, bool minData)
        {
            StringBuilder sb = new();
            var realSkip = skip;
            if (records.Count < skip)
            {
                realSkip = 0;
            }
            
            
            var toCheck = records.Skip(realSkip).ToList();
            logger.LogInformation($"Record to be checked ({toCheck.Count})");
            
            foreach (var record in toCheck)
            {
                var monitorTypeRule = rule.Rules.Find(mtRule => mtRule.Item == record.ITEM);
                if (monitorTypeRule == null)
                    continue;

                if (record.Code2 != "010")
                    continue;

                if ((double)record.M_Val > monitorTypeRule.AlarmHigh.GetValueOrDefault(double.MaxValue))
                    sb.Append($"{record.GetDateTime():g} 測站{record.DP_NO} {monitorTypeIo.MapReadOnly[record.ITEM].Desp.Trim()} {record.M_Val} 超上限 {monitorTypeRule.AlarmHigh}\n");
                else if ((double)record.M_Val < monitorTypeRule.AlarmLow.GetValueOrDefault(double.MinValue))
                    sb.Append($"{record.GetDateTime():g} 測站{record.DP_NO} {monitorTypeIo.MapReadOnly[record.ITEM].Desp.Trim()} {record.M_Val} 超下限 {monitorTypeRule.AlarmLow}\n");
            }

            // Check constant
            {
                foreach (var mtRule in rule.Rules)
                {
                    var constantCount = mtRule.ConstantCount.GetValueOrDefault(0);
                    if (constantCount == 0)
                        continue;

                    var filtered = 
                        records.Where(record => record.ITEM == mtRule.Item).Reverse().ToList();
                    
                    if (filtered.Capacity == 0)
                        continue;

                    var checkSeq = filtered.Take(constantCount).ToList();
                    if (checkSeq.Count < constantCount)
                        continue;

                    var allSame = checkSeq.Count == 0 || checkSeq.All(x => x.Equals(checkSeq[0]));
                    if (allSame)
                    {
                        var head = checkSeq[0];
                        
                        sb.Append($"{head.GetDateTime():g} {rule.Monitor} {monitorTypeIo.MapReadOnly[mtRule.Item].Desp.Trim()} 定值\n");
                    }
                        
                }
            }

            return sb.ToString().TrimEnd();
        }
        
        delegate string RecordChecker(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip);

        private async void Handler(string monitor,
            IEnumerable<RecordIo.MonitorRecord> enumerableRecord,
            RecordChecker checker)
        {
            try
            {
                var alarmRule = MonitorAlarmRules.MonitorAlarmRuleMap[monitor];
                var records = enumerableRecord.ToList();
                var monitorSkip = MonitorSkips.MonitorSkipMap[monitor];
                var alarmMessage = checker(records, alarmRule, monitorSkip.Skip);
                if (!string.IsNullOrEmpty(alarmMessage))
                {
                    await alarmIo.AddAlarm(AlarmIo.AlarmLevel.Error, alarmMessage);
                    await lineNotify.Notify(alarmMessage);
                }

                var newSkip = monitorSkip with { Skip = records.Count };
                MonitorSkips.UpdateSkip(newSkip, sysConfigIo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handler failed");
            }
        }

        private async void MonitorTask(bool param1)
        {
            try
            {
                logger.LogInformation("MonitorTask start");
                var today = DateTime.Today.Subtract(TimeSpan.FromDays(10));
                foreach (var monitor in MonitorAlarmRules.Monitors)
                {
                    logger.LogInformation("Checking Monitor {MonitorName}", monitor);
                    if(monitor.StartsWith('S'))
                        Handler(monitor, await recordIo.GetRecords(monitor, today), CheckMinRecords);
                    else
                        Handler(monitor, await recordIo.GetRecords(monitor, today), CheckHourRecords);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MonitorTask failed");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Init
                await monitorTypeIo.Init();
                MonitorAlarmRules.Init(await sysConfigIo.GetMonitorAlarmRules());
                MonitorSkips.Init(await sysConfigIo.GetMonitorSkips());
                _ = alarmIo.AddAlarm(AlarmIo.AlarmLevel.Info, "開始監測");
                _ = SimplePeriodicAction(MonitorTask, true, TimeSpan.FromMinutes(3), "MonitorTask");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DataMonitor StartAsync error");
            }

            return;

            Task SimplePeriodicAction(Action<bool> action, bool param, TimeSpan ts, string actionName)
            {
                var primaryTask = Task.Run(async () =>
                {
                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            action(param);
                            using PeriodicTimer periodicTimer = new(ts);
                            while (await periodicTimer.WaitForNextTickAsync(cancellationToken)
                                       .ConfigureAwait(false))
                            {
                                action(param);
                            }

                            logger.LogInformation("{ActionName} is stopped", actionName);
                        }
                        else
                        {
                            logger.LogInformation("DataCollectManager is cancelled before started");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "{ActionName} is failed", actionName);
                    }
                }, cancellationToken);
                return primaryTask.ContinueWith(task =>
                {
                    Helper.CheckTask(task, logger, $"SimplePeriodicAction {actionName} failed", actionName);
                    if (cancellationToken.IsCancellationRequested) return;
                    logger.LogInformation("Try to restart {ActionName}", actionName);
                    SimplePeriodicAction(action, param, ts, actionName);
                }, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}