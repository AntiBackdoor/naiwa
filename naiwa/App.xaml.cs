using System.Windows;
using System.Windows.Threading;

namespace naiwa
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "naiwa_error.log");
                try
                {
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Unhandled: {args.ExceptionObject}\n");
                }
                catch { }
            };
            DispatcherUnhandledException += (sender, args) =>
            {
                string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "naiwa_error.log");
                try
                {
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Dispatcher: {args.Exception}\n");
                }
                catch { }
                args.Handled = true;
            };
        }
    }
}
