using System;
using System.IO;
using System.Text.Json;
using YomogiTaskBar.Models;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Managers
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YomogiTaskBar",
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("設定ファイルの読み込みに失敗しました。デフォルト設定を使用します。", ex, "SettingsManager");
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError("設定ファイルの保存に失敗しました。", ex, "SettingsManager");
            }
        }
    }
}
