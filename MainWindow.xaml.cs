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
using System.Linq;
using System.Windows.Controls;

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
            _appBarManager.SizeAppBar();

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

            // Update current desktop name
            CurrentDesktopText.Text = VirtualDesktopHelper.GetCurrentDesktopName();
        }

        private void DesktopFooter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var desktops = VirtualDesktopHelper.GetDesktops();
            DesktopContextMenu.Items.Clear();

            foreach (var desktop in desktops)
            {
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = desktop.Name,
                    IsCheckable = true,
                    IsChecked = desktop.IsCurrent,
                    Tag = desktop.Id
                };
                item.Click += async (s, args) =>
                {
                    if (s is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is Guid id)
                    {
                        await VirtualDesktopHelper.SwitchToDesktop(id);
                        await System.Threading.Tasks.Task.Delay(500); // Wait for switch to complete
                        RefreshWindowList();
                    }
                };
                DesktopContextMenu.Items.Add(item);
            }

            DesktopContextMenu.Items.Add(new System.Windows.Controls.Separator());

            var addItem = new System.Windows.Controls.MenuItem { Header = "新しいデスクトップを作成 (+)" };
            addItem.Click += (s, args) =>
            {
                VirtualDesktopHelper.CreateNewDesktop();
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (st, et) => { timer.Stop(); RefreshWindowList(); };
                timer.Start();
            };
            DesktopContextMenu.Items.Add(addItem);

            var removeItem = new System.Windows.Controls.MenuItem { Header = "現在のデスクトップを閉じる (✕)" };
            removeItem.Click += (s, args) =>
            {
                VirtualDesktopHelper.RemoveCurrentDesktop();
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (st, et) => { timer.Stop(); RefreshWindowList(); };
                timer.Start();
            };
            DesktopContextMenu.Items.Add(removeItem);

            DesktopContextMenu.PlacementTarget = sender as UIElement;
            DesktopContextMenu.IsOpen = true;
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

        private void CloseItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is WindowItemViewModel windowItem)
            {
                _windowManager.CloseWindow(windowItem.Handle);
            }
        }

        private bool _isPinned = true;
        private DispatcherTimer _autoHideTimer;
        private bool _isHidden = false;

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            PinButton.Content = _isPinned ? "📌" : "📍";
            ApplyMode();
        }

        private void ApplyMode()
        {
            if (_isPinned)
            {
                if (_autoHideTimer != null) _autoHideTimer.Stop();
                _isHidden = false;
                _appBarManager?.Register((int)this.Width);
                _appBarManager?.SizeAppBar();
                this.Topmost = false;
            }
            else
            {
                _appBarManager?.Unregister();
                this.Topmost = true;
                
                if (_autoHideTimer == null)
                {
                    _autoHideTimer = new DispatcherTimer();
                    _autoHideTimer.Interval = TimeSpan.FromMilliseconds(500);
                    _autoHideTimer.Tick += (s, e) => HideWindow();
                }
            }
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isPinned)
            {
                _autoHideTimer?.Stop();
                ShowWindow();
            }
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isPinned && !_isHidden)
            {
                _autoHideTimer?.Start();
            }
        }

        private void ShowWindow()
        {
            if (!_isHidden) return;
            _isHidden = false;
            
            var screen = Forms.Screen.FromHandle(_windowHandle);
            var bounds = screen.Bounds;
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

            double left, top = bounds.Top / dpiY;
            if (_appBarManager.Edge == AppBarManager.ABEdge.ABE_LEFT)
            {
                left = bounds.Left / dpiX;
            }
            else
            {
                left = (bounds.Right / dpiX) - this.Width;
            }

            this.Left = left;
            this.Top = top;
        }

        private void HideWindow()
        {
            if (_isPinned || _isHidden) return;
            _autoHideTimer?.Stop();
            _isHidden = true;

            var screen = Forms.Screen.FromHandle(_windowHandle);
            var bounds = screen.Bounds;
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

            double left, top = bounds.Top / dpiY;
            const double visibleStrip = 2.0; // logical units

            if (_appBarManager.Edge == AppBarManager.ABEdge.ABE_LEFT)
            {
                left = (bounds.Left / dpiX) - this.Width + visibleStrip;
            }
            else
            {
                left = (bounds.Right / dpiX) - visibleStrip;
            }

            this.Left = left;
            this.Top = top;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Temporarily unregister to allow free movement between monitors
                _appBarManager?.Unregister();
                
                this.DragMove();
                
                // Re-docking logic will re-register the AppBar if pinned
                DetectEdgeAndDock();
            }
        }

        private void DetectEdgeAndDock()
        {
            var mousePos = Forms.Control.MousePosition;
            var screen = Forms.Screen.FromPoint(mousePos);
            var bounds = screen.Bounds;

            double threshold = bounds.Width * 0.1;

            if (mousePos.X < bounds.Left + threshold)
            {
                DockTo(AppBarManager.ABEdge.ABE_LEFT, force: true, targetBounds: bounds);
            }
            else if (mousePos.X > bounds.Right - threshold)
            {
                DockTo(AppBarManager.ABEdge.ABE_RIGHT, force: true, targetBounds: bounds);
            }
            else
            {
                DockTo(_appBarManager.Edge, force: true, targetBounds: bounds);
            }
        }

        private void DockTo(AppBarManager.ABEdge edge, bool force = false, System.Drawing.Rectangle? targetBounds = null)
        {
            if (_isPinned)
            {
                _appBarManager?.Register((int)this.Width, initialEdge: edge);
            }

            _appBarManager.Edge = edge;
            
            if (_isPinned)
            {
                _appBarManager.SizeAppBar(targetBounds);
            }
            else
            {
                // Manually position if not pinned
                var bounds = targetBounds ?? Forms.Screen.FromHandle(_windowHandle).Bounds;
                var source = PresentationSource.FromVisual(this);
                double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
                double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

                this.Top = bounds.Top / dpiY;
                this.Height = bounds.Height / dpiY;
                if (edge == AppBarManager.ABEdge.ABE_LEFT)
                {
                    this.Left = bounds.Left / dpiX;
                }
                else
                {
                    this.Left = (bounds.Right / dpiX) - this.Width;
                }
            }

            // Adjust UI for the new edge
            if (edge == AppBarManager.ABEdge.ABE_LEFT)
            {
                ResizeThumb.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                var dockPanel = (ResizeThumb.Parent as Grid).Children.OfType<DockPanel>().FirstOrDefault();
                if (dockPanel != null)
                {
                    dockPanel.Margin = new Thickness(0, 0, 6, 0);
                }
            }
            else
            {
                ResizeThumb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                var dockPanel = (ResizeThumb.Parent as Grid).Children.OfType<DockPanel>().FirstOrDefault();
                if (dockPanel != null)
                {
                    dockPanel.Margin = new Thickness(6, 0, 0, 0);
                }
            }
        }

        private double _tempWidth;

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _tempWidth = this.Width;
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // If docked to the right, dragging left (negative delta) increases width
            // If docked to the left, dragging right (positive delta) increases width
            if (_appBarManager.Edge == AppBarManager.ABEdge.ABE_RIGHT)
            {
                _tempWidth -= e.HorizontalChange;
            }
            else
            {
                _tempWidth += e.HorizontalChange;
            }
            
            if (_tempWidth >= 100 && _tempWidth <= 800)
            {
                this.Width = _tempWidth;
                _appBarManager?.PreviewWidth((int)_tempWidth);
            }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_tempWidth >= 100 && _tempWidth <= 800)
            {
                _appBarManager?.UpdateWidth((int)_tempWidth);
            }
        }
    }
}