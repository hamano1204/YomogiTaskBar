using System;
using System.Runtime.InteropServices;

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

    // Approach A: Internal COM interfaces for pinning
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4ce81783-1e84-4576-9d88-345c19747617")]
    internal interface IApplicationView { /* minimal definition */ }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1841c6d7-4f9d-42a0-af6d-245967278523")]
    internal interface IApplicationViewCollection
    {
        int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("836d9796-0165-4a6d-968a-24020a402377")]
    internal interface IVirtualDesktopPinnedViewManager
    {
        int IsViewPinned(IApplicationView view, out bool pinned);
        int PinView(IApplicationView view);
        int UnpinView(IApplicationView view);
    }

    public class VirtualDesktopHelper
    {
        private static readonly IVirtualDesktopManager _manager;
        private static readonly IApplicationViewCollection _viewCollection;
        private static readonly IVirtualDesktopPinnedViewManager _pinnedViewManager;

        static VirtualDesktopHelper()
        {
            try
            {
                _manager = (IVirtualDesktopManager)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")));
                
                // Internal API objects
                var shellType = Type.GetTypeFromCLSID(new Guid("1841c6d7-4f9d-42a0-af6d-245967278523")); // CLSID_ImmersiveShell
                // Note: These CLSIDs and IIDs are internal and can change.
                // We use common ones for Win10/11.
                
                _viewCollection = (IApplicationViewCollection)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("1841c6d7-4f9d-42a0-af6d-245967278523")));
                _pinnedViewManager = (IVirtualDesktopPinnedViewManager)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("2C362F4A-3920-4B3A-AA31-1606AD69CD2A")));
            }
            catch
            {
                // Fallback if internal APIs are not available or changed
            }
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

        public static void PinWindowToAllDesktops(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || _viewCollection == null || _pinnedViewManager == null) return;

            try
            {
                if (_viewCollection.GetViewForHwnd(hWnd, out var view) == 0)
                {
                    _pinnedViewManager.PinView(view);
                }
            }
            catch
            {
                // If Approach A (Internal API) fails, we rely on the timer-based MoveToCurrentDesktop
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static void MoveToCurrentDesktop(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || _manager == null) return;

            try
            {
                if (!IsWindowOnCurrentDesktop(hWnd))
                {
                    // Find a window that is on the current desktop to get its Desktop ID
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
