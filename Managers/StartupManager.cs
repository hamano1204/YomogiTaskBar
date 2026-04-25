using Microsoft.Win32;
using System;
using System.Reflection;
using YomogiTaskBar.Utilities;

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
            catch (Exception ex)
            {
                Logger.LogError("スタートアップ登録状態の確認に失敗しました。", ex, "StartupManager");
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
                Logger.LogInfo("スタートアップ登録を有効にしました。", "StartupManager");
            }
            catch (Exception ex)
            {
                Logger.LogError("スタートアップ登録の有効化に失敗しました。", ex, "StartupManager");
            }
        }

        public static void Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key?.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                    Logger.LogInfo("スタートアップ登録を無効にしました。", "StartupManager");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("スタートアップ登録の無効化に失敗しました。", ex, "StartupManager");
            }
        }

        public static void Apply(bool enabled)
        {
            if (enabled) Enable();
            else Disable();
        }
    }
}
