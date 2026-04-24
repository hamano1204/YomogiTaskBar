using Microsoft.Win32;
using System;
using System.Windows;

namespace YomogiTaskBar.Managers
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string mode)
        {
            string theme = mode;
            if (mode == "System")
            {
                theme = IsSystemInDarkMode() ? "Dark" : "Light";
            }

            ResourceDictionary dict = new ResourceDictionary();
            try
            {
                if (theme == "Dark")
                {
                    dict.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                }
                else
                {
                    dict.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                }

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
