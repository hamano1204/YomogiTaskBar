using System.Windows;
using System.Threading;

namespace YomogiTaskBar;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static Mutex _mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "YomogiTaskBar-Unique-ID";
        bool createdNew;

        _mutex = new Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            // App is already running!
            _mutex = null;
            System.Windows.MessageBox.Show("アプリは既に起動しています。", "YomogiTaskBar", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Windows.Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch { }
            _mutex.Close();
        }
        base.OnExit(e);
    }
}

