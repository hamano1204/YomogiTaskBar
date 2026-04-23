using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SideBarTaskSwitcher.Managers
{
    public class AppBarManager
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        private enum ABMsg : uint
        {
            ABM_NEW = 0,
            ABM_REMOVE = 1,
            ABM_QUERYPOS = 2,
            ABM_SETPOS = 3,
            ABM_WINDOWPOSCHANGED = 9
        }

        public enum ABEdge : uint
        {
            ABE_LEFT = 0,
            ABE_TOP = 1,
            ABE_RIGHT = 2,
            ABE_BOTTOM = 3
        }

        [DllImport("shell32.dll")]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private IntPtr _windowHandle;
        private int _currentWidth;
        private bool _isRegistered = false;
        private ABEdge _currentEdge = ABEdge.ABE_RIGHT;

        public ABEdge Edge
        {
            get => _currentEdge;
            set
            {
                if (_currentEdge != value)
                {
                    _currentEdge = value;
                    if (_isRegistered)
                    {
                        SizeAppBar();
                    }
                }
            }
        }

        public AppBarManager(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
        }

        public void Register(int width)
        {
            if (_isRegistered) return;

            _currentWidth = width;

            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;

            SHAppBarMessage((uint)ABMsg.ABM_NEW, ref abd);

            SizeAppBar();

            _isRegistered = true;
        }

        public void UpdateWidth(int width)
        {
            if (!_isRegistered) return;
            _currentWidth = width;
            SizeAppBar();
        }

        public void PreviewWidth(int width)
        {
            if (!_isRegistered) return;
            
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            int left, right;
            if (_currentEdge == ABEdge.ABE_LEFT)
            {
                left = 0;
                right = width;
            }
            else
            {
                left = screenWidth - width;
                right = screenWidth;
            }

            int top = 0;
            int bottom = screenHeight;

            // SWP_NOACTIVATE | SWP_NOZORDER
            SetWindowPos(_windowHandle, IntPtr.Zero, left, top, right - left, bottom - top, 0x0014);
        }

        public void Unregister()
        {
            if (!_isRegistered) return;

            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;

            SHAppBarMessage((uint)ABMsg.ABM_REMOVE, ref abd);
            _isRegistered = false;
        }

        private void SizeAppBar()
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;
            abd.uEdge = (uint)_currentEdge;

            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            abd.rc.top = 0;
            abd.rc.bottom = screenHeight;

            if (_currentEdge == ABEdge.ABE_LEFT)
            {
                abd.rc.left = 0;
                abd.rc.right = _currentWidth;
            }
            else
            {
                abd.rc.left = screenWidth - _currentWidth;
                abd.rc.right = screenWidth;
            }

            // Query the system for an approved size and position.
            SHAppBarMessage((uint)ABMsg.ABM_QUERYPOS, ref abd);

            // Calculate the actual size
            if (_currentEdge == ABEdge.ABE_LEFT)
            {
                abd.rc.right = abd.rc.left + _currentWidth;
            }
            else
            {
                abd.rc.left = abd.rc.right - _currentWidth;
            }

            // Set the new position
            SHAppBarMessage((uint)ABMsg.ABM_SETPOS, ref abd);

            // Move the WPF window
            SetWindowPos(abd.hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, 0x0014);
        }
    }
}
// End of file
