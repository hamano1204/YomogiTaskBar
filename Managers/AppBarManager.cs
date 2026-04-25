using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Managers
{
    public class AppBarManager
    {
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
                    // Removed automatic SizeAppBar call here to prevent flickering
                    // when multiple properties are updated at once.
                }
            }
        }

        public AppBarManager(Window window)
        {
            _windowHandle = new WindowInteropHelper(window).Handle;
        }

        public void Register(int width, ABEdge? initialEdge = null)
        {
            if (_isRegistered) return;

            _currentWidth = width;
            if (initialEdge.HasValue)
            {
                _currentEdge = initialEdge.Value;
            }

            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;

            NativeMethods.SHAppBarMessage((uint)ABMsg.ABM_NEW, ref abd);

            _isRegistered = true;
            // Caller should call SizeAppBar after registration
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
            
            var screen = System.Windows.Forms.Screen.FromHandle(_windowHandle);
            var bounds = screen.Bounds;

            int left, right;
            if (_currentEdge == ABEdge.ABE_LEFT)
            {
                left = bounds.Left;
                right = bounds.Left + width;
            }
            else
            {
                left = bounds.Right - width;
                right = bounds.Right;
            }

            int top = bounds.Top;
            int bottom = bounds.Bottom;

            // SWP_NOACTIVATE | SWP_NOZORDER
            NativeMethods.SetWindowPos(_windowHandle, IntPtr.Zero, left, top, right - left, bottom - top, 0x0014);
        }

        public void Unregister()
        {
            if (!_isRegistered) return;

            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;

            NativeMethods.SHAppBarMessage((uint)ABMsg.ABM_REMOVE, ref abd);
            _isRegistered = false;
        }

        public void SizeAppBar(System.Drawing.Rectangle? targetBounds = null)
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            abd.hWnd = _windowHandle;
            abd.uEdge = (uint)_currentEdge;

            var bounds = targetBounds ?? System.Windows.Forms.Screen.FromHandle(_windowHandle).Bounds;

            abd.rc.top = bounds.Top;
            abd.rc.bottom = bounds.Bottom;

            if (_currentEdge == ABEdge.ABE_LEFT)
            {
                abd.rc.left = bounds.Left;
                abd.rc.right = bounds.Left + _currentWidth;
            }
            else
            {
                abd.rc.left = bounds.Right - _currentWidth;
                abd.rc.right = bounds.Right;
            }

            // Query the system for an approved size and position.
            NativeMethods.SHAppBarMessage((uint)ABMsg.ABM_QUERYPOS, ref abd);

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
            NativeMethods.SHAppBarMessage((uint)ABMsg.ABM_SETPOS, ref abd);

            // Move the WPF window
            NativeMethods.SetWindowPos(abd.hWnd, IntPtr.Zero, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, 0x0014);
        }
    }
}

// End of file
