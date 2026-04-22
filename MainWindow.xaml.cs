using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SideBarTaskSwitcher.Managers;
using SideBarTaskSwitcher.ViewModels;
using System.Reflection;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SideBarTaskSwitcher
{
    public partial class MainWindow : Window
    {
        private AppBarManager _appBarManager;
        private WindowManager _windowManager;
        private ObservableCollection<WindowItemViewModel> _windows;
        private DispatcherTimer _timer;
        private Forms.NotifyIcon _notifyIcon;
        private IntPtr _windowHandle;

        public MainWindow()
        {
            InitializeComponent();
            _windowManager = new WindowManager();
            _windows = new ObservableCollection<WindowItemViewModel>();
            WindowsList.ItemsSource = _windows;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            
            // Hide from Task View and Alt+Tab
            int exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
            SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
            
            int True = 1;
            DwmSetWindowAttribute(_windowHandle, DWMWA_EXCLUDED_FROM_PEEK, ref True, sizeof(int));

            _appBarManager = new AppBarManager(this);
            _appBarManager.Register((int)this.Width);

            // Pin to all virtual desktops (Approach A)
            VirtualDesktopHelper.PinWindowToAllDesktops(_windowHandle);

            RefreshWindowList();

            // Set up 1-second timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, ev) => 
            {
                VirtualDesktopHelper.MoveToCurrentDesktop(_windowHandle);
                RefreshWindowList();
            };
            _timer.Start();

            // Set up NotifyIcon for System Tray
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "SideBar Task Switcher";

            var contextMenuStrip = new Forms.ContextMenuStrip();
            var closeMenuItem = new Forms.ToolStripMenuItem("閉じる");
            closeMenuItem.Click += (s, args) => this.Close();
            contextMenuStrip.Items.Add(closeMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _appBarManager?.Unregister();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }

        private void RefreshWindowList()
        {
            var windows = _windowManager.GetRunningWindows();
            
            _windows.Clear();
            foreach (var w in windows)
            {
                _windows.Add(w);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void WindowsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (WindowsList.SelectedItem is WindowItemViewModel selected)
            {
                _windowManager.ActivateWindow(selected.Handle);
                WindowsList.SelectedItem = null; // Reset selection
            }
        }

        private double _tempWidth;

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _tempWidth = this.Width;
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // AppBar is docked to the right, so dragging thumb to the left (negative e.HorizontalChange) increases width
            _tempWidth -= e.HorizontalChange;
            
            // Set reasonable min/max width constraints
            if (_tempWidth >= 100 && _tempWidth <= 800)
            {
                this.Width = _tempWidth;
                // Update only Window Visual position, do NOT push other windows to prevent lagging
                _appBarManager?.PreviewWidth((int)_tempWidth);
            }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_tempWidth >= 100 && _tempWidth <= 800)
            {
                 // Push other windows now that drag has finished
                _appBarManager?.UpdateWidth((int)_tempWidth);
            }
        }
    }
}