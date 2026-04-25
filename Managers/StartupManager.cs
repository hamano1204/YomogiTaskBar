using Microsoft.Win32;
using System;
using System.Reflection;

namespace YomogiTaskBar.Managers
{
    public static class StartupManager
    {
        private const string AppName = "YomogiTaskBar";
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void Enable()
        {
            try
            {
                string exePath = Environment.ProcessPath
                    ?? Assembly.GetExecutingAssembly().Location;

                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            catch { }
        }

        public static void Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key?.GetValue(AppName) != null)
                    key.DeleteValue(AppName);
            }
            catch { }
        }

        public static void Apply(bool enabled)
        {
            if (enabled) Enable();
            else Disable();
        }
    }
}
