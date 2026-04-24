using System;
using System.Runtime.InteropServices;

namespace SideBarTaskSwitcher.Managers
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
    }
}
