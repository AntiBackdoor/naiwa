using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace naiwa
{
    public partial class MainWindow : Window
    {
        private bool _allowClose = false;
        private VideoPlayerService _videoPlayerService;
        private ProcessMonitorService _processMonitorService;
        private KeyboardHookService _keyboardHookService;
        private SystemEventService _systemEventService;

        public MainWindow()
        {
            InitializeComponent();

            _videoPlayerService = new VideoPlayerService(mediaElement, errorText);
            _processMonitorService = new ProcessMonitorService();
            _keyboardHookService = new KeyboardHookService();
            _systemEventService = new SystemEventService();

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            mediaElement.MediaOpened += (s, e) => _videoPlayerService.OnMediaOpened();
            mediaElement.MediaEnded += (s, e) => _videoPlayerService.OnMediaEnded();
            mediaElement.MediaFailed += _videoPlayerService.OnMediaFailed;
            _systemEventService.RegisterSessionEndingHandler(SystemEvents_SessionEnding);
            KeyDown += Window_KeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _processMonitorService.KillExplorer();
            _videoPlayerService.LoadVideo();
            _processMonitorService.StartMonitoring();
            _keyboardHookService.InstallHook();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _allowClose = true;
                Close();
                return;
            }

            if (_keyboardHookService.HandleSpecialKey(e.Key, e.SystemKey, Keyboard.Modifiers))
            {
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _keyboardHookService.Dispose();
            _processMonitorService.StopMonitoring();
            _systemEventService.UnregisterSessionEndingHandler(SystemEvents_SessionEnding);
            e.Cancel = !_allowClose;
            _allowClose = false;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
