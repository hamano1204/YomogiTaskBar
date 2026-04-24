using System;
using System.Runtime.InteropServices;

namespace YomogiTaskBar.Managers
{
    public class HotkeyListener
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_WIN = 0x0008;
        public const uint VK_ESCAPE = 0x1B;
        public const int HOTKEY_ID = 9000;

        public static void Register(IntPtr hWnd, int id, uint modifiers, uint vk)
        {
            RegisterHotKey(hWnd, id, modifiers, vk);
        }

        public static void Unregister(IntPtr hWnd, int id)
        {
            UnregisterHotKey(hWnd, id);
        }

        public static uint GetWin32Modifiers(System.Windows.Input.ModifierKeys modifiers)
        {
            uint win32Modifiers = 0;
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) win32Modifiers |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) win32Modifiers |= 0x0001; // MOD_ALT
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) win32Modifiers |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows)) win32Modifiers |= 0x0008; // MOD_WIN
            return win32Modifiers;
        }

        public static uint GetWin32VirtualKey(System.Windows.Input.Key key)
        {
            return (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
        }
    }
}
