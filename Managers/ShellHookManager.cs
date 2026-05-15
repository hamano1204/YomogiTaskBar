using System;
using System.Windows.Interop;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Managers
{
    public class ShellHookManager : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private uint _shellHookMsg;
        private HwndSource? _hwndSource;

        public event EventHandler<EventArgs>? WindowListNeedsRefresh;

        public ShellHookManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        public void Initialize()
        {
            try
            {
                _shellHookMsg = NativeMethods.RegisterWindowMessage("SHELLHOOK");
                NativeMethods.RegisterShellHookWindow(_windowHandle);
                
                _hwndSource = HwndSource.FromHwnd(_windowHandle);
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(WndProc);
                    Logger.LogInfo("Shell hook registered successfully", "ShellHookManager");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize shell hook", ex, "ShellHookManager");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _shellHookMsg)
            {
                int shellEvent = wParam.ToInt32();
                switch (shellEvent)
                {
                    case NativeMethods.HSHELL_WINDOWCREATED:
                    case NativeMethods.HSHELL_WINDOWDESTROYED:
                    case NativeMethods.HSHELL_WINDOWACTIVATED:
                    case NativeMethods.HSHELL_RUDEAPPACTIVATED:
                    case NativeMethods.HSHELL_REDRAW:
                    case NativeMethods.HSHELL_WINDOWREPLACED:
                        Logger.LogDebug($"Shell hook event: {shellEvent}. Requesting window list refresh.", "ShellHookManager");
                        WindowListNeedsRefresh?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
            NativeMethods.DeregisterShellHookWindow(_windowHandle);
            Logger.LogInfo("Shell hook deregistered", "ShellHookManager");
        }
    }
}
