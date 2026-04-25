using System;
using System.Runtime.InteropServices;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Managers
{
    public class HotkeyListener
    {
        public const uint MOD_WIN = 0x0008;
        public const uint VK_ESCAPE = 0x1B;
        public const int HOTKEY_ID = 9000;

        /// <summary>
        /// グローバルホットキーを登録します。登録失敗時はログに警告を出力します。
        /// </summary>
        public static bool Register(IntPtr hWnd, int id, uint modifiers, uint vk)
        {
            bool success = NativeMethods.RegisterHotKey(hWnd, id, modifiers, vk);
            if (!success)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.LogWarning(
                    $"ホットキー登録失敗 (ID={id}, Modifiers={modifiers}, VK={vk}, Error={err}). 別のアプリが同じキーを使用中の可能性があります。",
                    "HotkeyListener");
            }
            return success;
        }

        /// <summary>
        /// グローバルホットキーの登録を解除します。
        /// </summary>
        public static void Unregister(IntPtr hWnd, int id)
        {
            NativeMethods.UnregisterHotKey(hWnd, id);
        }

        public static uint GetWin32Modifiers(System.Windows.Input.ModifierKeys modifiers)
        {
            uint win32Modifiers = 0;
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) win32Modifiers |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))     win32Modifiers |= 0x0001; // MOD_ALT
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))   win32Modifiers |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows)) win32Modifiers |= 0x0008; // MOD_WIN
            return win32Modifiers;
        }

        public static uint GetWin32VirtualKey(System.Windows.Input.Key key)
        {
            return (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
        }
    }
}
