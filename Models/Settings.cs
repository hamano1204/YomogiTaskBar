using System.Windows.Input;
using System.Collections.Generic;
using YomogiTaskBar.Managers;

namespace YomogiTaskBar.Models
{
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
        public bool IsAppBarMode { get; set; } = true; // AppBar vs Floating mode
        public AppBarManager.ABEdge Edge { get; set; } = AppBarManager.ABEdge.ABE_RIGHT; // Left/Right edge
        public int MonitorIndex { get; set; } = 0; // 0 = first monitor
        public double WindowWidth { get; set; } = 300; // Window width
        public int LastMonitorCount { get; set; } = 1; // For detecting monitor changes
    }

    public class AppSettings
    {
        public string ThemeMode { get; set; } = "System"; // Light, Dark, System
        public bool LaunchOnStartup { get; set; } = false;
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
