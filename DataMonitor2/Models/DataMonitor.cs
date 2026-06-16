using DataMonitor2.Db;

namespace DataMonitor2.Models
{
    internal sealed class DataMonitor(
        ILogger<DataMonitor> logger,
        MonitorTypeIo monitorTypeIo,
        IHostEnvironment env) : IHostedService
    {
        private readonly ILogger _logger = logger;
        private readonly MonitorTypeIo _monitorTypeI = monitorTypeIo;
        private readonly IHostEnvironment _env = env;

        private void MonitorTask(bool param1)
        {
            try
            {
                _logger.LogInformation("Data checking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MonitorTask failed");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Init
                monitorTypeIo.Init().Wait(cancellationToken);
                
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

                                    _logger.LogInformation("{ActionName} is stopped", actionName);
                                }
                                else
                                {
                                    _logger.LogInformation("DataCollectManager is cancelled before started");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "{ActionName} is failed", actionName);
                            }
                        }, cancellationToken);
                        return primaryTask.ContinueWith(task =>
                        {
                            Helper.CheckTask(task, _logger, $"SimplePeriodicAction {actionName} failed", actionName);
                            if (cancellationToken.IsCancellationRequested) return;
                            _logger.LogInformation("Try to restart {ActionName}", actionName);
                            SimplePeriodicAction(action, param, ts, actionName);
                        }, cancellationToken);
                    }

                    _ = SimplePeriodicAction(MonitorTask, true, TimeSpan.FromMinutes(1), "MonitorTask");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "DataCollectManager StartAsync error");
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