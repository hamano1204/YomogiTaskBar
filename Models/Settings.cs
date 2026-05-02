using System.Windows.Input;
using System.Collections.Generic;
using YomogiTaskBar.Managers;

namespace YomogiTaskBar.Models
{
    public enum LayoutMode
    {
        Simple,         // シンプルレイアウト（現在のデスクトップのみ）
        AllDesktops     // デスクトップをすべて表示
    }

    public enum MonitorIndicatorDisplay
    {
        None,           // 表示しない
        Left,           // 左側に表示
        Right           // 右側に表示（現在の構成）
    }

    public class ShortcutConfig
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public override string ToString()
        {
            if (Key == Key.None) return "None";
            var parts = new List<string>();
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }

        public bool IsPressed(System.Windows.Input.KeyEventArgs e)
        {
            return e.Key == Key && Keyboard.Modifiers == Modifiers;
        }
    }

    public class WindowSettings
    {
        private double _windowWidth = WindowConstants.DefaultWindowWidth;
        private int _monitorIndex = 0;
        private int _lastMonitorCount = 1;

        public bool IsAppBarMode { get; set; } = true; // Always pinned mode for stability

        public AppBarManager.ABEdge Edge { get; set; } = AppBarManager.ABEdge.ABE_RIGHT;

        public int MonitorIndex
        {
            get => _monitorIndex;
            set => _monitorIndex = Math.Max(0, value);
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set => _windowWidth = Math.Clamp(value, WindowConstants.MinWindowWidth, WindowConstants.MaxWindowWidth);
        }

        public int LastMonitorCount
        {
            get => _lastMonitorCount;
            set => _lastMonitorCount = Math.Max(1, value);
        }

        /// <summary>
        /// Validates and fixes any invalid settings
        /// </summary>
        public void ValidateAndFix()
        {
            // Ensure window width is within bounds
            WindowWidth = _windowWidth;
            
            // Ensure monitor index is non-negative
            MonitorIndex = _monitorIndex;
            
            // Ensure last monitor count is at least 1
            LastMonitorCount = _lastMonitorCount;
        }
    }

    public class AppSettings
    {
        public string ThemeMode { get; set; } = "System"; // Light, Dark, System
        public bool LaunchOnStartup { get; set; } = false;
        public LayoutMode LayoutMode { get; set; } = LayoutMode.Simple; // アプリ一覧のレイアウトモード
        public MonitorIndicatorDisplay MonitorIndicatorDisplay { get; set; } = MonitorIndicatorDisplay.Right; // モニターインジケーターの表示設定
        public ShortcutConfig GlobalActivate { get; set; } = new ShortcutConfig { Key = Key.Escape, Modifiers = ModifierKeys.Windows };
        public ShortcutConfig Minimize { get; set; } = new ShortcutConfig { Key = Key.J, Modifiers = ModifierKeys.Control };
        public ShortcutConfig ToggleMaximize { get; set; } = new ShortcutConfig { Key = Key.K, Modifiers = ModifierKeys.Control };
        public ShortcutConfig Close { get; set; } = new ShortcutConfig { Key = Key.L, Modifiers = ModifierKeys.Control };
        public ShortcutConfig NextMonitor { get; set; } = new ShortcutConfig { Key = Key.I, Modifiers = ModifierKeys.Control };
        public ShortcutConfig PrevMonitor { get; set; } = new ShortcutConfig { Key = Key.U, Modifiers = ModifierKeys.Control };
        
        // Window position and display settings
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();
    }
}
