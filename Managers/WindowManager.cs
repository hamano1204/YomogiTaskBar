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
using YomogiTaskBar.ViewModels;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Managers
{
    public class WindowManager
    {
        private readonly Dictionary<IntPtr, ImageSource> _iconCache = new Dictionary<IntPtr, ImageSource>();

        public List<WindowItemViewModel> GetRunningWindows()
        {
            var windows = new List<WindowItemViewModel>();
            var currentProcessId = Process.GetCurrentProcess().Id;
            var allScreens = System.Windows.Forms.Screen.AllScreens;
            var foregroundWindow = NativeMethods.GetForegroundWindow();

            // Clean up cache for closed windows
            var currentHandles = new HashSet<IntPtr>();

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (IsTaskbarWindow(hWnd))
                {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);

                    currentHandles.Add(hWnd);

                    // Exclude our own app
                    if (processId == currentProcessId)
                        return true;

                    StringBuilder titleBuilder = new StringBuilder(256);
                    if (NativeMethods.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity) > 0)
                    {
                        var title = titleBuilder.ToString();
                        
                        // Filter out empty descriptions, Program Manager, and known overlay/hidden windows
                        string[] ignoredTitles = { "Program Manager", "Recording", "Microsoft Text Input Application" };
                        
                        if (!string.IsNullOrWhiteSpace(title) && !ignoredTitles.Contains(title))
                        {
                            var screen = System.Windows.Forms.Screen.FromHandle(hWnd);
                            int monitorIndex = 0;
                            if (allScreens.Length > 1)
                            {
                                monitorIndex = Array.IndexOf(allScreens, allScreens.FirstOrDefault(s => s.DeviceName == screen.DeviceName)) + 1;
                            }

                            windows.Add(new WindowItemViewModel
                            {
                                Handle = hWnd,
                                Title = title,
                                ProcessId = (int)processId,
                                IconSource = GetWindowIcon(hWnd),
                                IsMinimized = NativeMethods.IsIconic(hWnd),
                                MonitorIndex = monitorIndex,
                                IsActive = (hWnd == foregroundWindow)
                            });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            // Remove closed windows from cache
            var keysToRemove = _iconCache.Keys.Where(k => !currentHandles.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                _iconCache.Remove(key);
            }

            var normalWindows = windows.Where(w => !w.IsMinimized)
                .OrderBy(w => w.MonitorIndex)
                .ThenBy(w => w.ProcessId)
                .ToList();
            var minimizedWindows = windows.Where(w => w.IsMinimized)
                .OrderBy(w => w.MonitorIndex)
                .ThenBy(w => w.ProcessId)
                .ToList();

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
            if (NativeMethods.IsIconic(handle))
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
            }
            NativeMethods.SetForegroundWindow(handle);
        }

        public void CloseWindow(IntPtr handle)
        {
            NativeMethods.SendMessage(handle, NativeMethods.WM_SYSCOMMAND, new IntPtr((int)NativeMethods.SC_CLOSE), IntPtr.Zero);
        }

        public void MinimizeWindow(IntPtr handle)
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);
        }

        public void ToggleMaximize(IntPtr handle)
        {
            if (NativeMethods.IsZoomed(handle))
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
            }
            else
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_MAXIMIZE);
            }
        }

        public void MoveToMonitor(IntPtr handle, bool next)
        {
            var allScreens = System.Windows.Forms.Screen.AllScreens;
            if (allScreens.Length <= 1) return;

            var currentScreen = System.Windows.Forms.Screen.FromHandle(handle);
            int currentIndex = Array.IndexOf(allScreens, allScreens.FirstOrDefault(s => s.DeviceName == currentScreen.DeviceName));
            
            int targetIndex;
            if (next)
                targetIndex = (currentIndex + 1) % allScreens.Length;
            else
                targetIndex = (currentIndex - 1 + allScreens.Length) % allScreens.Length;

            var targetScreen = allScreens[targetIndex];

            // Restore if maximized for smooth transition (minimized windows stay minimized)
            bool wasMaximized = NativeMethods.IsZoomed(handle);
            if (wasMaximized)
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
            }

            if (NativeMethods.GetWindowRect(handle, out RECT rect))
            {
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                // Calculate center position on target screen
                int newX = targetScreen.Bounds.Left + (targetScreen.Bounds.Width - width) / 2;
                int newY = targetScreen.Bounds.Top + (targetScreen.Bounds.Height - height) / 2;

                // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
                NativeMethods.SetWindowPos(handle, IntPtr.Zero, newX, newY, 0, 0, 0x0001 | 0x0004 | 0x0010);

                if (wasMaximized)
                {
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_MAXIMIZE);
                }
            }
        }

        private bool IsTaskbarWindow(IntPtr hWnd)
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return false;

            int cloakedVal;
            NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_CLOAKED, out cloakedVal, sizeof(int));
            if (cloakedVal != 0)
                return false;

            // Check if window is on current virtual desktop
            if (!VirtualDesktopHelper.IsWindowOnCurrentDesktop(hWnd))
                return false;

            // 1. サイズが異常（0x0のような見えないウィンドウ）を除外
            if (NativeMethods.GetWindowRect(hWnd, out RECT rect))
            {
                if (rect.right - rect.left <= 0 || rect.bottom - rect.top <= 0)
                    return false;
            }

            // ルートオーナーの最後のアクティブなポップアップでない場合に除外
            // Edgeで検索したときにEdgeがタスクバーから見えなくなる不具合があるためコメントアウトして様子見。
            //IntPtr rootOwner = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOTOWNER);
            //if (NativeMethods.GetLastActivePopup(rootOwner) != hWnd)
            //    return false;

            int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            
            // 2. マウスクリックを透過する設定のオーバーレイ表示（透明レイヤー）を除外
            if ((exStyle & NativeMethods.WS_EX_LAYERED) != 0 && (exStyle & NativeMethods.WS_EX_TRANSPARENT) != 0)
                return false;

            // 透明レイヤー（WS_EX_LAYERED + WS_EX_TRANSPARENT）を除外
            if ((exStyle & NativeMethods.WS_EX_APPWINDOW) != 0)
                return true;

            // WS_EX_APPWINDOWがない場合、WS_EX_TOOLWINDOWなら除外。FansyWMが除外できる
            if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                return false;

            //オーナーウィンドウがある場合はタスクバーから除外。 Brotherのスキャナーが除外できる
            IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
            return owner == IntPtr.Zero;
        }

        private ImageSource? GetWindowIcon(IntPtr hWnd)
        {
            if (_iconCache.TryGetValue(hWnd, out var cachedIcon))
            {
                return cachedIcon;
            }

            string? path = GetProcessPath(hWnd);
            bool isUwp = path != null && path.Contains("\\WindowsApps\\", StringComparison.OrdinalIgnoreCase);

            ImageSource? hIconSrc = null;
            IntPtr hIcon = IntPtr.Zero;
            bool needsDestroy = false;

            if (isUwp)
            {
                // Try Windows 10 SDK extraction first (most reliable for UWP)
                if (!string.IsNullOrEmpty(path))
                {
                    hIconSrc = GetUwpIconNative(hWnd);
                    if (hIconSrc != null)
                    {
                        // Cache it immediately and return
                        _iconCache[hWnd] = hIconSrc;
                        return hIconSrc;
                    }
                }

                // Try to get icon from UWP child window (CoreWindow)
                NativeMethods.EnumChildWindows(hWnd, (childHWnd, lParam) =>
                {
                    IntPtr childIcon = NativeMethods.SendMessage(childHWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL2), IntPtr.Zero);
                    if (childIcon == IntPtr.Zero) childIcon = NativeMethods.SendMessage(childHWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL), IntPtr.Zero);
                    if (childIcon == IntPtr.Zero) childIcon = NativeMethods.GetClassLongPtr(childHWnd, NativeMethods.GCLP_HICONSM);
                    
                    if (childIcon != IntPtr.Zero)
                    {
                        hIcon = childIcon;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                // For UWP, ExtractIconEx directly from the exe can work if it has embedded icons
                if (hIcon == IntPtr.Zero && !string.IsNullOrEmpty(path))
                {
                    IntPtr hLarge = IntPtr.Zero;
                    IntPtr hSmall = IntPtr.Zero;
                    try
                    {
                        NativeMethods.ExtractIconEx(path, 0, out hLarge, out hSmall, 1);
                        if (hSmall != IntPtr.Zero)
                        {
                            hIcon = hSmall;
                            if (hLarge != IntPtr.Zero) NativeMethods.DestroyIcon(hLarge);
                            needsDestroy = true;
                        }
                        else if (hLarge != IntPtr.Zero)
                        {
                            hIcon = hLarge;
                            needsDestroy = true;
                        }
                    }
                    catch { }
                }
            }

            if (hIcon == IntPtr.Zero)
            {
                hIcon = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL2), IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL), IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_BIG), IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICONSM);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICON);
            }

            if (hIcon == IntPtr.Zero)
            {
                // Fallback: Get icon from process executable
                if (!string.IsNullOrEmpty(path))
                {
                    SHFILEINFO shinfo = new SHFILEINFO();
                    NativeMethods.SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON);
                    hIcon = shinfo.hIcon;
                    needsDestroy = true; // We created this icon via SHGetFileInfo, so we must destroy it
                }
            }

            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    bitmapSource.Freeze();
                    
                    // Only cache if the window is NOT minimized, 
                    // because minimized UWP apps often return generic icons.
                    if (!NativeMethods.IsIconic(hWnd))
                    {
                        _iconCache[hWnd] = bitmapSource;
                    }

                    return bitmapSource;
                }
                catch
                {
                    // Ignore errors fetching custom/UWP icons
                }
                finally
                {
                    if (needsDestroy)
                    {
                        NativeMethods.DestroyIcon(hIcon);
                    }
                }
            }

            return null;
        }

        private ImageSource? GetUwpIconNative(IntPtr hWnd)
        {
            try
            {
                uint pid;
                NativeMethods.GetWindowThreadProcessId(hWnd, out pid);

                // Get real pid for UWP apps
                uint realPid = 0;
                NativeMethods.EnumChildWindows(hWnd, (childHWnd, lParam) =>
                {
                    uint childPid;
                    NativeMethods.GetWindowThreadProcessId(childHWnd, out childPid);
                    if (childPid != pid)
                    {
                        realPid = childPid;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                uint targetPid = realPid != 0 ? realPid : pid;

                IntPtr hProcess = NativeMethods.OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, targetPid);
                if (hProcess != IntPtr.Zero)
                {
                    try
                    {
                        uint len = 0;
                        NativeMethods.GetPackageFullName(hProcess, ref len, null);
                        if (len > 0)
                        {
                            var sb = new StringBuilder((int)len);
                            if (NativeMethods.GetPackageFullName(hProcess, ref len, sb) == 0) // ERROR_SUCCESS
                            {
                                string pkgName = sb.ToString();
                                var pm = new Windows.Management.Deployment.PackageManager();
                                var pkg = pm.FindPackageForUser(string.Empty, pkgName);
                                
                                if (pkg != null)
                                {
                                    // Use the exact logo provided by the OS
                                    Uri logoUri = pkg.Logo;
                                    if (logoUri != null)
                                    {
                                        var bitmap = new BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmap.UriSource = logoUri;
                                        bitmap.EndInit();
                                        bitmap.Freeze();
                                        return bitmap;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(hProcess);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        private string? GetProcessPath(IntPtr hWnd)
        {
            uint pid;
            NativeMethods.GetWindowThreadProcessId(hWnd, out pid);

            // Handle ApplicationFrameHost (UWP Apps)
            string? path = GetPathFromPid(pid);
            if (path != null && path.EndsWith("ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase))
            {
                uint realPid = 0;
                NativeMethods.EnumChildWindows(hWnd, (childHWnd, lParam) =>
                {
                    uint childPid;
                    NativeMethods.GetWindowThreadProcessId(childHWnd, out childPid);
                    if (childPid != pid)
                    {
                        realPid = childPid;
                        return false; // Stop enumeration
                    }
                    return true;
                }, IntPtr.Zero);

                if (realPid != 0)
                {
                    path = GetPathFromPid(realPid);
                }
            }

            return path;
        }

        private string? GetPathFromPid(uint pid)
        {
            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    StringBuilder sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        return sb.ToString();
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hProcess);
                }
            }
            return null;
        }
    }
}
