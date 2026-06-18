using System.Diagnostics;
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
        AlarmIo alarmIo) : IHostedService
    {
        private string CheckMinRecords(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip)
            => CheckRecords(records, rule, skip, true);


        private string CheckHourRecords(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip)
            => CheckRecords(records, rule, skip, false);

        private string CheckRecords(List<RecordIo.MonitorRecord> todayRecords, AlarmRule rule, int skip, bool minData)
        {
            var monitor = rule.Monitor;
            StringBuilder sb = new();
            var realSkip = skip;
            if (todayRecords.Count < skip)
            {
                realSkip = 0;
            }


            var toCheck = todayRecords.Skip(realSkip).ToList();
            logger.LogInformation($"Record to be checked ({toCheck.Count})");

            foreach (var record in toCheck)
            {
                var monitorTypeRule = rule.Rules.Find(mtRule => mtRule.Item == record.ITEM);
                if (monitorTypeRule == null)
                    continue;

                if (record.Code2 != "010")
                    continue;

                if ((double)record.M_Val > monitorTypeRule.AlarmHigh.GetValueOrDefault(double.MaxValue))
                    sb.Append(
                        $"{record.GetDateTime():g} 測站{record.DP_NO} {monitorTypeIo.MapReadOnly[record.ITEM].Desp.Trim()} {record.M_Val} 超上限 {monitorTypeRule.AlarmHigh}\n");
                else if ((double)record.M_Val < monitorTypeRule.AlarmLow.GetValueOrDefault(double.MinValue))
                    sb.Append(
                        $"{record.GetDateTime():g} 測站{record.DP_NO} {monitorTypeIo.MapReadOnly[record.ITEM].Desp.Trim()} {record.M_Val} 超下限 {monitorTypeRule.AlarmLow}\n");
            }

            // Check Time delay only for minData
            var latestRecord = todayRecords.Last();
            if (minData && latestRecord.GetDateTime().AddMinutes(30) < DateTime.Now)
                sb.Append($"測站{monitor} 通信異常 (超過30分鐘無資料)");

            // Check constant
            foreach (var mtRule in rule.Rules)
            {
                var constantCount = mtRule.ConstantCount.GetValueOrDefault(0);
                if (constantCount == 0)
                    continue;

                var filtered =
                    todayRecords.Where(record => record.ITEM == mtRule.Item).Reverse().ToList();

                if (filtered.Capacity == 0)
                    continue;

                var checkSeq = filtered.Take(constantCount).ToList();
                if (checkSeq.Count < constantCount)
                    continue;

                var head = checkSeq.First();
                var allSame = checkSeq.All(x => x.M_Val == head.M_Val);
                if (allSame)
                {
                    sb.Append(
                        $"{head.GetDateTime():g} {rule.Monitor} {monitorTypeIo.MapReadOnly[mtRule.Item].Desp.Trim()} 定值\n");
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
                var newSkip = monitorSkip with { Skip = records.Count };
                MonitorSkips.UpdateSkip(newSkip, sysConfigIo);
                if (string.IsNullOrEmpty(alarmMessage)) return;

                await alarmIo.AddAlarm(AlarmIo.AlarmLevel.Error, alarmMessage);
                await lineNotify.Notify(alarmMessage);
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
                
                var today = DateTime.Today;
                foreach (var monitor in MonitorAlarmRules.Monitors)
                {
                    logger.LogInformation("Checking Monitor {MonitorName}", monitor);
                    if (monitor.StartsWith('S'))
                        Handler(monitor, await recordIo.GetRecords(monitor, today), CheckHourRecords);
                    else
                        Handler(monitor, await recordIo.GetRecords(monitor, today), CheckMinRecords);
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
                _ = SimplePeriodicAction(MonitorTask, true, TimeSpan.FromMinutes(10), "MonitorTask");
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