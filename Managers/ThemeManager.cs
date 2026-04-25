using Microsoft.Win32;
using System;
using System.Windows;

namespace YomogiTaskBar.Managers
{
    public static class ThemeManager
    {
        private static string _currentMode = "System";

        static ThemeManager()
        {
            // OS のライト/ダークモード変更イベントを購読
            // UserPreferenceChanged は バックグラウンドスレッドから呼ばれるため Dispatcher 経由で UI スレッドに戻す
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
            {
                // システムモードのときのみ自動追従する
                if (_currentMode == "System")
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() => ApplyTheme("System"));
                }
            }
        }

        public static void ApplyTheme(string mode)
        {
            _currentMode = mode;

            string theme = mode;
            if (mode == "System")
            {
                theme = IsSystemInDarkMode() ? "Dark" : "Light";
            }

            ResourceDictionary dict = new ResourceDictionary();
            try
            {
                dict.Source = theme == "Dark"
                    ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                    : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

                App.Current.Resources.MergedDictionaries.Clear();
                App.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme Error: {ex.Message}");
            }
        }

        public static bool IsSystemInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value is int i)
                        {
                            return i == 0;
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
