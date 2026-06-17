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
            logger.LogInformation($"Checking {toCheck.Count}");
            
            foreach (var record in toCheck)
            {
                if (!rule.Monitors.Contains(record.DP_NO))
                    continue;
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
            foreach (var monitor in rule.Monitors)
            {
                foreach (var mtRule in rule.Rules)
                {
                    var constantCount = mtRule.ConstantCount.GetValueOrDefault(0);
                    if (constantCount == 0)
                        continue;

                    var filtered = 
                        records.Where(record => 
                            record.DP_NO == monitor && record.ITEM == mtRule.Item).Reverse().ToList();
                    if (filtered.Capacity == 0)
                        continue;

                    var checkSeq = filtered.Take(constantCount).ToList();
                    if (checkSeq.Count < constantCount)
                        continue;

                    var allSame = checkSeq.Count == 0 || checkSeq.All(x => x.Equals(checkSeq[0]));
                    if (allSame)
                    {
                        var head = checkSeq[0];
                        
                        sb.Append($"{head.GetDateTime():g} {monitor} {monitorTypeIo.MapReadOnly[mtRule.Item].Desp.Trim()} 定值\n");
                    }
                        
                }
            }

            return sb.ToString().TrimEnd();
        }


        delegate Task<int> SkipGetter();

        delegate Task SkipSetter(int skip);

        delegate string RecordChecker(List<RecordIo.MonitorRecord> records, AlarmRule rule, int skip);

        async void Handler(AlarmRule? alarmRule,
            IEnumerable<RecordIo.MonitorRecord> enumerableRecord,
            SkipGetter skipGetter,
            SkipSetter skipSetter,
            RecordChecker checker)
        {
            try
            {
                if (alarmRule == null) return;
                var records = enumerableRecord.ToList();
                var skip = await skipGetter();
                var alarmMessage = checker(records, alarmRule, skip);
                if (!string.IsNullOrEmpty(alarmMessage))
                {
                    await alarmIo.AddAlarm(AlarmIo.AlarmLevel.Error, alarmMessage);
                    await lineNotify.Notify(alarmMessage);
                }
                    
                await skipSetter(records.Count);
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
                Handler(await sysConfigIo.GetAMinAlarmRule(),
                    await recordIo.GetAMinRecords(today),
                    sysConfigIo.GetAMinSkip, sysConfigIo.SetAMinSkip, CheckMinRecords);

                Handler(await sysConfigIo.GetWMinAlarmRule(),
                    await recordIo.GetWMinRecords(today),
                    sysConfigIo.GetWMinSkip, sysConfigIo.SetWMinSkip, CheckMinRecords);

                Handler(await sysConfigIo.GetCMinAlarmRule(),
                    await recordIo.GetCMinRecords(today),
                    sysConfigIo.GetCMinSkip, sysConfigIo.SetCMinSkip, CheckMinRecords);

                Handler(await sysConfigIo.GetSHourAlarmRule(),
                    await recordIo.GetSHourRecords(today),
                    sysConfigIo.GetSHourSkip, sysConfigIo.SetSHourSkip, CheckHourRecords);
                logger.LogInformation("MonitorTask end");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MonitorTask failed");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Init
                monitorTypeIo.Init().Wait(cancellationToken);
                _ = alarmIo.AddAlarm(AlarmIo.AlarmLevel.Info, "開始監測");
                try
                {
                    // Helper function
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

                    _ = SimplePeriodicAction(MonitorTask, true, TimeSpan.FromMinutes(3), "MonitorTask");
                }
                catch (Exception e)
                {
                    logger.LogError(e, "DataCollectManager StartAsync error");
                    throw;
                }

                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}