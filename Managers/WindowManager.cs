using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using SideBarTaskSwitcher.ViewModels;

namespace SideBarTaskSwitcher.Managers
{
    public class WindowManager
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        private static extern IntPtr GetClassLongPtr32(IntPtr hWnd, int nIndex);

        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size > 4)
                return GetClassLongPtr64(hWnd, nIndex);
            else
                return GetClassLongPtr32(hWnd, nIndex);
        }

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int GW_OWNER = 4;
        private const uint GA_ROOTOWNER = 3;

        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WS_EX_APPWINDOW = 0x00040000;
        
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;

        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int ICON_SMALL2 = 2;
        private const int GCLP_HICON = -14;
        private const int GCLP_HICONSM = -34;

        public List<WindowItemViewModel> GetRunningWindows()
        {
            var windows = new List<WindowItemViewModel>();
            var currentProcessId = Process.GetCurrentProcess().Id;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsTaskbarWindow(hWnd))
                {
                    GetWindowThreadProcessId(hWnd, out uint processId);

                    // Exclude our own app
                    if (processId == currentProcessId)
                        return true;

                    StringBuilder titleBuilder = new StringBuilder(256);
                    if (GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity) > 0)
                    {
                        var title = titleBuilder.ToString();
                        
                        // Filter out empty descriptions, Program Manager, and known overlay/hidden windows
                        string[] ignoredTitles = { "Program Manager", "Recording", "Microsoft Text Input Application" };
                        
                        if (!string.IsNullOrWhiteSpace(title) && !ignoredTitles.Contains(title))
                        {
                            windows.Add(new WindowItemViewModel
                            {
                                Handle = hWnd,
                                Title = title,
                                ProcessId = (int)processId,
                                IconSource = GetWindowIcon(hWnd),
                                IsMinimized = IsIconic(hWnd)
                            });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            var normalWindows = windows.Where(w => !w.IsMinimized).ToList();
            var minimizedWindows = windows.Where(w => w.IsMinimized).ToList();

            var sortedWindows = new List<WindowItemViewModel>();
            sortedWindows.AddRange(normalWindows);

            if (normalWindows.Count > 0 && minimizedWindows.Count > 0)
            {
                sortedWindows.Add(new WindowItemViewModel { IsSeparator = true, Title = "Separator" });
            }

            sortedWindows.AddRange(minimizedWindows);

            return sortedWindows;
        }

        public void ActivateWindow(IntPtr handle)
        {
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }
            SetForegroundWindow(handle);
        }

        public void CloseWindow(IntPtr handle)
        {
            SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const int DWMWA_CLOAKED = 14;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_LAYERED = 0x00080000;

        private bool IsTaskbarWindow(IntPtr hWnd)
        {
            if (!IsWindowVisible(hWnd))
                return false;

            int cloakedVal;
            DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloakedVal, sizeof(int));
            if (cloakedVal != 0)
                return false;

            // Check if window is on current virtual desktop
            if (!VirtualDesktopHelper.IsWindowOnCurrentDesktop(hWnd))
                return false;

            // 1. サイズが異常（0x0のような見えないウィンドウ）を除外
            if (GetWindowRect(hWnd, out RECT rect))
            {
                if (rect.right - rect.left <= 0 || rect.bottom - rect.top <= 0)
                    return false;
            }

            IntPtr rootOwner = GetAncestor(hWnd, GA_ROOTOWNER);
            if (GetLastActivePopup(rootOwner) != hWnd)
                return false;

            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            
            // 2. マウスクリックを透過する設定のオーバーレイ表示（透明レイヤー）を除外
            if ((exStyle & WS_EX_LAYERED) != 0 && (exStyle & WS_EX_TRANSPARENT) != 0)
                return false;

            if ((exStyle & WS_EX_APPWINDOW) != 0)
                return true;

            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return false;

            IntPtr owner = GetWindow(hWnd, GW_OWNER);
            return owner == IntPtr.Zero;
        }

        private ImageSource? GetWindowIcon(IntPtr hWnd)
        {
            IntPtr hIcon = SendMessage(hWnd, WM_GETICON, new IntPtr(ICON_SMALL2), IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, new IntPtr(ICON_SMALL), IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, new IntPtr(ICON_BIG), IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = GetClassLongPtr(hWnd, GCLP_HICONSM);
            if (hIcon == IntPtr.Zero)
                hIcon = GetClassLongPtr(hWnd, GCLP_HICON);

            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                catch
                {
                    // Ignore errors fetching custom/UWP icons
                }
            }

            return null;
        }
    }
}
