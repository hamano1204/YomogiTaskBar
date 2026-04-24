using System.Windows.Input;
using System.Collections.Generic;

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

    public class AppSettings
    {
        public string ThemeMode { get; set; } = "System"; // Light, Dark, System
        public ShortcutConfig GlobalActivate { get; set; } = new ShortcutConfig { Key = Key.Escape, Modifiers = ModifierKeys.Windows };
        public ShortcutConfig Minimize { get; set; } = new ShortcutConfig { Key = Key.J, Modifiers = ModifierKeys.Control };
        public ShortcutConfig ToggleMaximize { get; set; } = new ShortcutConfig { Key = Key.K, Modifiers = ModifierKeys.Control };
        public ShortcutConfig Close { get; set; } = new ShortcutConfig { Key = Key.L, Modifiers = ModifierKeys.Control };
        public ShortcutConfig NextMonitor { get; set; } = new ShortcutConfig { Key = Key.F, Modifiers = ModifierKeys.Control };
        public ShortcutConfig PrevMonitor { get; set; } = new ShortcutConfig { Key = Key.D, Modifiers = ModifierKeys.Control };
    }
}
