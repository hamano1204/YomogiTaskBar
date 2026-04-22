using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SideBarTaskSwitcher.Managers
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    public interface IVirtualDesktopManager
    {
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out int onCurrentDesktop);
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);
        int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }

    public class VirtualDesktopInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsCurrent { get; set; }
    }

    public static class VirtualDesktopHelper
    {
        private static readonly IVirtualDesktopManager _manager;

        static VirtualDesktopHelper()
        {
            try
            {
                _manager = (IVirtualDesktopManager)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")));
            }
            catch { }
        }

        public static List<VirtualDesktopInfo> GetDesktops()
        {
            var results = new List<VirtualDesktopInfo>();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops");
                if (key != null)
                {
                    var virtualDesktops = (byte[]?)key.GetValue("VirtualDesktopIDs") ?? (byte[]?)key.GetValue("VirtualDesktopList");
                    var currentDesktop = (byte[]?)key.GetValue("CurrentVirtualDesktop");
                    Guid currentGuid = (currentDesktop != null && currentDesktop.Length >= 16) ? new Guid(currentDesktop) : Guid.Empty;

                    if (virtualDesktops != null)
                    {
                        for (int i = 0; i < virtualDesktops.Length; i += 16)
                        {
                            byte[] guidBytes = new byte[16];
                            Array.Copy(virtualDesktops, i, guidBytes, 0, 16);
                            Guid id = new Guid(guidBytes);
                            results.Add(new VirtualDesktopInfo
                            {
                                Id = id,
                                Name = GetDesktopName(id, results.Count + 1),
                                IsCurrent = id == currentGuid
                            });
                        }
                    }
                }
            }
            catch { }
            if (results.Count == 0) results.Add(new VirtualDesktopInfo { Name = "Desktop 1", IsCurrent = true });
            return results;
        }

        private static string GetDesktopName(Guid id, int index)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops\Desktops\{{{id}}}");
                if (key != null)
                {
                    var name = key.GetValue("Name") as string;
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            catch { }
            return $"Desktop {index}";
        }

        public static string GetCurrentDesktopName()
        {
            var desktops = GetDesktops();
            return desktops.FirstOrDefault(d => d.IsCurrent)?.Name ?? "Desktop 1";
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        const uint KEYEVENTF_KEYUP = 0x0002;
        const byte VK_LWIN = 0x5B;
        const byte VK_CONTROL = 0x11;
        const byte VK_LEFT = 0x25;
        const byte VK_RIGHT = 0x27;
        const byte VK_D = 0x44;
        const byte VK_F4 = 0x73;

        private static void SendKeyCombo(byte directionKey)
        {
            keybd_event(VK_LWIN, 0, 0, 0);
            keybd_event(VK_CONTROL, 0, 0, 0);
            keybd_event(directionKey, 0, 0, 0);
            
            keybd_event(directionKey, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, 0);
        }

        public static async Task SwitchToDesktop(Guid id)
        {
            var desktops = GetDesktops();
            int currentIndex = desktops.FindIndex(d => d.IsCurrent);
            int targetIndex = desktops.FindIndex(d => d.Id == id);

            if (currentIndex == -1 || targetIndex == -1 || currentIndex == targetIndex) return;

            int diff = targetIndex - currentIndex;
            byte key = diff > 0 ? VK_RIGHT : VK_LEFT;
            int count = Math.Abs(diff);

            for (int i = 0; i < count; i++)
            {
                SendKeyCombo(key);
                await Task.Delay(200); // Wait for OS animation/processing
            }
        }

        public static void CreateNewDesktop()
        {
            SendKeyCombo(VK_D);
        }

        public static void RemoveCurrentDesktop()
        {
            SendKeyCombo(VK_F4);
        }

        public static bool IsWindowOnCurrentDesktop(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || _manager == null) return true;
            try
            {
                _manager.IsWindowOnCurrentVirtualDesktop(hWnd, out int onCurrent);
                return onCurrent != 0;
            }
            catch { return true; }
        }

        public static void PinWindowToAllDesktops(IntPtr hWnd) { }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static void MoveToCurrentDesktop(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || _manager == null) return;
            try
            {
                if (!IsWindowOnCurrentDesktop(hWnd))
                {
                    IntPtr fg = GetForegroundWindow();
                    if (fg != IntPtr.Zero && _manager.GetWindowDesktopId(fg, out Guid currentId) == 0)
                    {
                        _manager.MoveWindowToDesktop(hWnd, ref currentId);
                    }
                }
            }
            catch { }
        }
    }
}
