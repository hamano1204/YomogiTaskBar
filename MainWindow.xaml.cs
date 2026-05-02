using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using YomogiTaskBar.Managers;
using YomogiTaskBar.ViewModels;
using YomogiTaskBar.Controllers;
using YomogiTaskBar.Utilities;
using System.Reflection;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Media;
using YomogiTaskBar.Models;

namespace YomogiTaskBar
{
    public partial class MainWindow : Window
    {
        private AppBarController? _appBarController;
        private WindowStateManager? _stateManager;
        private WindowManager _windowManager;
        private ObservableCollection<WindowItemViewModel> _windows;
        private DispatcherTimer? _timer;
        private Forms.NotifyIcon? _notifyIcon;
        private IntPtr _windowHandle;
        private AppSettings _settings;
        private DispatcherTimer? _autoHideTimer;
        private LayoutMode _currentLayoutMode;
        private bool _isExternalAppActive = false;
        private bool _isUserNavigating = false;
        private bool _selectionLocked = false;
        private bool _refreshDisabled = false;
        private bool _isWinEscActive = false; // Flag to prevent any refresh during Win+Esc
        private DispatcherTimer? _navigationTimer;

        /// <summary>
        /// Checks if the currently focused window is an external application (not this taskbar)
        /// </summary>
        private bool IsExternalAppFocused()
        {
            IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            return foregroundWindow != _windowHandle && foregroundWindow != IntPtr.Zero;
        }

        public MainWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Load();
            _currentLayoutMode = _settings.LayoutMode;
            _settings.LaunchOnStartup = StartupManager.IsEnabled();
            ThemeManager.ApplyTheme(_settings.ThemeMode);
            _windowManager = new WindowManager();
            _windows = new ObservableCollection<WindowItemViewModel>();
            WindowsList.ItemsSource = _windows;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            
            // Force pinned mode on startup for stability
            PinButton.Content = "📌";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.LogOperationStart("Window initialization", "MainWindow");

                // Hide from Task View and Alt+Tab
                int exStyle = NativeMethods.GetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(_windowHandle, NativeMethods.GWL_EXSTYLE, exStyle | (int)NativeMethods.WS_EX_TOOLWINDOW);
                
                int True = 1;
                NativeMethods.DwmSetWindowAttribute(_windowHandle, NativeMethods.DWMWA_EXCLUDED_FROM_PEEK, ref True, sizeof(int));

                // Initialize controllers
                _stateManager = new WindowStateManager(this, _windowHandle, _settings);
                _appBarController = new AppBarController(this, _windowHandle);
                _appBarController.Initialize();

                _appBarController.PinModeChanged += (s, isPinned) => 
                {
                    PinButton.Content = isPinned ? "📌" : "📍";
                    ApplyMode();
                };

                // Restore window settings
                var restoredSettings = _stateManager.RestoreWindowSettings();
                
                // Register AppBar with restored edge setting
                _appBarController.RegisterAppBar((int)this.Width, restoredSettings.Edge);
                
                // Update UI for restored edge
                UpdateUIForEdge(restoredSettings.Edge);

                RefreshWindowList();

                // Register global hotkey
                RegisterGlobalHotkey();
                ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

                // Set up refresh timer
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(WindowConstants.RefreshIntervalSeconds);
                _timer.Tick += (s, ev) =>
                {
                    Logger.LogDebug("Timer tick: calling RefreshWindowList", "MainWindow");
                    VirtualDesktopHelper.MoveToCurrentDesktop(_windowHandle);
                    RefreshWindowList();
                };
                _timer.Start();
                Logger.LogDebug("Refresh timer started", "MainWindow");

                // Set up focus event handlers to track when taskbar gets focus
                this.GotFocus += (s, e) =>
                {
                    Logger.LogDebug("MainWindow GotFocus", "MainWindow");
                    _isExternalAppActive = false;
                    // Don't clear navigation flags during navigation
                    if (!_isUserNavigating)
                    {
                        _selectionLocked = false;
                        _refreshDisabled = false;
                    }
                };
                this.Activated += (s, e) =>
                {
                    Logger.LogDebug("MainWindow Activated", "MainWindow");
                    _isExternalAppActive = false;
                    // Don't clear navigation flags during navigation
                    if (!_isUserNavigating)
                    {
                        _selectionLocked = false;
                        _refreshDisabled = false;
                    }
                };
                WindowsList.GotFocus += (s, e) =>
                {
                    Logger.LogDebug("WindowsList GotFocus", "MainWindow");
                    _isExternalAppActive = false;
                    // Don't clear navigation flags during navigation
                    if (!_isUserNavigating)
                    {
                        _selectionLocked = false;
                        _refreshDisabled = false;
                    }
                };
                WindowsList.LostFocus += (s, e) =>
                {
                    Logger.LogDebug("WindowsList LostFocus", "MainWindow");
                    // Don't clear selection if we're in navigation mode
                    if (_isUserNavigating)
                    {
                        Logger.LogDebug("Maintaining selection during navigation despite focus loss", "MainWindow");
                    }
                };

                // Set up navigation timer to clear user navigation flag after inactivity
                _navigationTimer = new DispatcherTimer();
                _navigationTimer.Interval = TimeSpan.FromSeconds(3);
                _navigationTimer.Tick += (s, e) =>
                {
                    Logger.LogDebug("Navigation timer tick: clearing navigation flags and restarting refresh timer", "MainWindow");
                    _isUserNavigating = false;
                    _selectionLocked = false;
                    _refreshDisabled = false;
                    _timer?.Start(); // Restart the refresh timer
                    _navigationTimer?.Stop(); // Stop the navigation timer after use
                };
                // Don't start navigation timer immediately - wait for first navigation key press
                Logger.LogDebug("Navigation timer initialized but not started", "MainWindow");

                // Set up NotifyIcon for System Tray
                SetupNotifyIcon();

                Logger.LogOperationComplete("Window initialization", "MainWindow");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize window", ex, "MainWindow");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Logger.LogOperationStart("Window cleanup", "MainWindow");

                // Save window settings on close
                if (_appBarController != null && _stateManager != null)
                {
                    _stateManager.SaveWindowSettings(_appBarController.CurrentEdge);
                }
                
                ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;
                HotkeyListener.Unregister(_windowHandle, HotkeyListener.HOTKEY_ID);

                // Cleanup timers
                _navigationTimer?.Stop();
                _navigationTimer = null;

                // Cleanup controllers
                _appBarController?.Dispose();
                
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }

                Logger.LogOperationComplete("Window cleanup", "MainWindow");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to cleanup window", ex, "MainWindow");
            }
        }

        private void SetupNotifyIcon()
        {
            try
            {
                _notifyIcon = new Forms.NotifyIcon();
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                _notifyIcon.Visible = true;
                _notifyIcon.Text = "YomogiTaskBar";

                var contextMenuStrip = new Forms.ContextMenuStrip();
                var closeMenuItem = new Forms.ToolStripMenuItem("閉じる");
                closeMenuItem.Click += (s, args) => this.Close();
                contextMenuStrip.Items.Add(closeMenuItem);

                _notifyIcon.ContextMenuStrip = contextMenuStrip;
                
                Logger.LogInfo("NotifyIcon setup completed", "MainWindow");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to setup NotifyIcon", ex, "MainWindow");
            }
        }

        private void UpdateUIForEdge(AppBarManager.ABEdge edge)
        {
            try
            {
                if (edge == AppBarManager.ABEdge.ABE_LEFT)
                {
                    ResizeThumb.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    WindowsList.Margin = new Thickness(0, 0, 6, 0);
                    HeaderBorder.Padding = new Thickness(10, 10, 16, 10);
                    FooterBorder.Padding = new Thickness(10, 10, 16, 10);
                }
                else
                {
                    ResizeThumb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    WindowsList.Margin = new Thickness(6, 0, 0, 0);
                    HeaderBorder.Padding = new Thickness(16, 10, 10, 10);
                    FooterBorder.Padding = new Thickness(16, 10, 10, 10);
                }
                
                Logger.LogDebug($"UI updated for edge: {edge}", "MainWindow");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to update UI for edge", ex, "MainWindow");
            }
        }

        private void RefreshWindowList()
        {
            Logger.LogDebug("RefreshWindowList called", "MainWindow");
            Logger.LogDebug($"_refreshDisabled={_refreshDisabled}, _isUserNavigating={_isUserNavigating}, _isExternalAppActive={_isExternalAppActive}, _isWinEscActive={_isWinEscActive}", "MainWindow");

            // Skip refresh if Win+Esc is active (complete prevention)
            if (_isWinEscActive)
            {
                Logger.LogDebug("RefreshWindowList skipped: Win+Esc active", "MainWindow");
                return;
            }

            // Skip refresh if refresh is disabled during navigation
            if (_refreshDisabled)
            {
                Logger.LogDebug("RefreshWindowList skipped: refresh disabled", "MainWindow");
                return;
            }

            // Skip refresh if user is actively navigating with keyboard
            if (_isUserNavigating)
            {
                Logger.LogDebug("RefreshWindowList skipped: user navigating", "MainWindow");
                return;
            }

            // Skip refresh if external app is active and has focus (to maintain Win+Esc selection)
            if (_isExternalAppActive && IsExternalAppFocused())
            {
                Logger.LogDebug("RefreshWindowList skipped: external app active", "MainWindow");
                return;
            }

            var windows = _windowManager.GetRunningWindows(_currentLayoutMode);

            // Preserve selected item, index, and focus state before clearing
            WindowItemViewModel? selectedItem = WindowsList.SelectedItem as WindowItemViewModel;
            IntPtr? selectedHandle = selectedItem?.Handle;
            int selectedIndex = WindowsList.SelectedIndex;
            bool hasFocus = WindowsList.IsFocused;
            Logger.LogDebug($"Preserving selection: Handle={selectedHandle}, Index={selectedIndex}, Title={selectedItem?.Title}, HasFocus={hasFocus}", "MainWindow");

            // Clear and rebuild the list (simpler approach for multiple separators)
            _windows.Clear();
            foreach (var window in windows)
            {
                _windows.Add(window);
            }

            Logger.LogDebug($"List rebuilt: {_windows.Count} items", "MainWindow");

            // Restore selected item - try by handle first, then by index as fallback
            if (selectedHandle.HasValue)
            {
                var restoredItem = _windows.FirstOrDefault(w => w.Handle == selectedHandle.Value);
                if (restoredItem != null)
                {
                    Logger.LogDebug($"Restoring selection by handle: {restoredItem.Title}", "MainWindow");
                    WindowsList.SelectedItem = restoredItem;
                }
                else if (selectedIndex >= 0 && selectedIndex < _windows.Count)
                {
                    // Fallback to index-based restoration
                    Logger.LogDebug($"Handle not found, restoring by index: {selectedIndex}", "MainWindow");
                    WindowsList.SelectedIndex = selectedIndex;
                }
                else
                {
                    Logger.LogDebug("Selection restoration failed: handle not found and index out of range", "MainWindow");
                }
            }
            else
            {
                Logger.LogDebug("No selection to restore", "MainWindow");
            }

            // Restore focus if it was lost during refresh
            if (hasFocus && !WindowsList.IsFocused)
            {
                Logger.LogDebug("Restoring focus to WindowsList", "MainWindow");
                WindowsList.Focus();
            }

            // Update current desktop name
            CurrentDesktopText.Text = VirtualDesktopHelper.GetCurrentDesktopName();
        }

        private void RegisterGlobalHotkey()
        {
            HotkeyListener.Register(_windowHandle, HotkeyListener.HOTKEY_ID, 
                HotkeyListener.GetWin32Modifiers(_settings.GlobalActivate.Modifiers), 
                HotkeyListener.GetWin32VirtualKey(_settings.GlobalActivate.Key));
        }

        private void UnregisterGlobalHotkey()
        {
            HotkeyListener.Unregister(_windowHandle, HotkeyListener.HOTKEY_ID);
        }

        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == 0x0312 && (int)msg.wParam == HotkeyListener.HOTKEY_ID)
            {
                OnHotkeyTriggered();
                handled = true;
            }
        }

        private void OnHotkeyTriggered()
        {
            IntPtr prevWindow = NativeMethods.GetForegroundWindow();

            // Reveal if hidden by Edge Trigger
            if (_appBarController?.IsHidden == true) _appBarController.ShowWindow();

            this.Activate();
            _isWinEscActive = true; // Set Win+Esc flag to prevent refresh
            Logger.LogDebug("Win+Esc activated: setting flag to prevent refresh", "MainWindow");
            RefreshWindowList();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Select the previously active window in the list
                var target = _windows.FirstOrDefault(w => w.Handle == prevWindow || w.Handle == NativeMethods.GetAncestor(prevWindow, NativeMethods.GA_ROOTOWNER));
                
                if (target == null && _windows.Count > 0)
                {
                    // Select the first non-separator item
                    target = _windows.FirstOrDefault(w => !w.IsSeparator);
                }

                if (target != null)
                {
                    WindowsList.SelectedItem = target;
                    WindowsList.ScrollIntoView(target);
                    
                    var item = (ListBoxItem)WindowsList.ItemContainerGenerator.ContainerFromItem(target);
                    if (item != null)
                    {
                        item.Focus();
                    }
                }
                else
                {
                    WindowsList.Focus();
                }
            }), DispatcherPriority.Loaded);
        }

        private void ActivateSelectedItem()
        {
            if (WindowsList.SelectedItem is WindowItemViewModel selected && !selected.IsSeparator)
            {
                Logger.LogDebug($"ActivateSelectedItem: {selected.Title}", "MainWindow");
                _isExternalAppActive = true; // Set flag to prevent refresh interruptions
                _isWinEscActive = false; // Clear Win+Esc flag on activation
                _isUserNavigating = false; // Clear navigation flag on activation
                _selectionLocked = false; // Clear selection lock on activation
                _refreshDisabled = false; // Clear refresh disabled flag on activation
                _timer?.Start(); // Restart the refresh timer
                Logger.LogDebug("Refresh timer restarted after activation", "MainWindow");
                _windowManager.ActivateWindow(selected.Handle);
                if (_appBarController?.IsPinned == false) _appBarController.HideWindow();
            }
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            UnregisterGlobalHotkey();
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings = settingsWindow.CurrentSettings;
                ThemeManager.ApplyTheme(_settings.ThemeMode);
                _stateManager?.UpdateSettings(_settings);
                
                // Reflect layout mode change immediately
                if (_currentLayoutMode != _settings.LayoutMode)
                {
                    _currentLayoutMode = _settings.LayoutMode;
                    RefreshWindowList();
                }
            }
            else
            {
                // Restore original theme if cancelled
                ThemeManager.ApplyTheme(_settings.ThemeMode);
            }
            RegisterGlobalHotkey();
        }

        private void WindowsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.LogDebug("MouseLeftButtonUp: clearing navigation flags and restarting timer", "MainWindow");
            // Clear navigation flag on mouse interaction
            _isUserNavigating = false;
            _selectionLocked = false;
            _refreshDisabled = false;
            _timer?.Start(); // Restart the refresh timer

            // Only activate if we actually clicked an item
            var dependencyObject = e.OriginalSource as DependencyObject;
            while (dependencyObject != null && !(dependencyObject is ListBoxItem))
            {
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            if (dependencyObject is ListBoxItem)
            {
                ActivateSelectedItem();
            }
        }

        private void WindowsList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Set navigation flag for arrow keys and navigation keys
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End)
            {
                Logger.LogDebug($"Navigation key pressed: {e.Key}", "MainWindow");
                if (!_isUserNavigating)
                {
                    Logger.LogDebug("Starting navigation: setting flags and stopping timer", "MainWindow");
                    _isUserNavigating = true;
                    _selectionLocked = true;
                    _refreshDisabled = true;
                    _timer?.Stop(); // Stop the refresh timer during navigation
                    _navigationTimer?.Start(); // Start navigation timer
                    Logger.LogDebug("Navigation started: timer stopped, navigation timer started", "MainWindow");
                }
                else
                {
                    // Reset navigation timer for continued navigation
                    Logger.LogDebug("Continuing navigation: resetting navigation timer", "MainWindow");
                    _navigationTimer?.Stop();
                    _navigationTimer?.Start();
                }
            }

            if (e.Key == Key.Enter)
            {
                ActivateSelectedItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Check if Win key is pressed (Win+Esc)
                if (Keyboard.Modifiers == ModifierKeys.Windows)
                {
                    // Win+Esc: maintain selection and focus on the window
                    if (WindowsList.SelectedItem is WindowItemViewModel selected && !selected.IsSeparator)
                    {
                        ActivateSelectedItem();
                    }
                    e.Handled = true;
                }
                else
                {
                    // Just Esc: clear selection
                    WindowsList.SelectedItem = null;
                    e.Handled = true;
                }
            }
            else
            {
                if (WindowsList.SelectedItem is WindowItemViewModel selected && !selected.IsSeparator)
                {
                    bool shouldReactivate = true;
                    
                    if (_settings.Minimize.IsPressed(e))
                    {
                        _windowManager.MinimizeWindow(selected.Handle);
                        e.Handled = true;
                    }
                    else if (_settings.ToggleMaximize.IsPressed(e))
                    {
                        _windowManager.ToggleMaximize(selected.Handle);
                        e.Handled = true;
                    }
                    else if (_settings.Close.IsPressed(e))
                    {
                        int index = WindowsList.SelectedIndex;
                        _windowManager.CloseWindow(selected.Handle);
                        
                        _windows.Remove(selected);
                        
                        if (_windows.Count > 0)
                        {
                            int nextIndex = Math.Min(index, _windows.Count - 1);
                            if (_windows[nextIndex].IsSeparator)
                            {
                                if (nextIndex + 1 < _windows.Count) nextIndex++;
                                else if (nextIndex - 1 >= 0) nextIndex--;
                            }
                            WindowsList.SelectedIndex = nextIndex;
                        }
                        
                        e.Handled = true;
                    }
                    else if (_settings.NextMonitor.IsPressed(e))
                    {
                        int selectedIndex = WindowsList.SelectedIndex;
                        _windowManager.MoveToMonitor(selected.Handle, true);
                        WindowsList.SelectedIndex = selectedIndex;
                        e.Handled = true;
                    }
                    else if (_settings.PrevMonitor.IsPressed(e))
                    {
                        int selectedIndex = WindowsList.SelectedIndex;
                        _windowManager.MoveToMonitor(selected.Handle, false);
                        WindowsList.SelectedIndex = selectedIndex;
                        e.Handled = true;
                    }
                    else
                    {
                        shouldReactivate = false;
                    }

                    if (shouldReactivate)
                    {
                        this.Activate();
                        WindowsList.Focus();
                    }
                }
            }
        }

        private void WindowsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Logger.LogDebug($"SelectionChanged: Added={e.AddedItems.Count}, Removed={e.RemovedItems.Count}, Locked={_selectionLocked}", "MainWindow");
            
            // Prevent selection changes when selection is locked during navigation
            if (_selectionLocked && e.RemovedItems.Count > 0)
            {
                Logger.LogDebug("Selection locked: restoring previous selection", "MainWindow");
                // Restore the previous selection
                if (e.RemovedItems[0] is WindowItemViewModel removedItem)
                {
                    WindowsList.SelectedItem = removedItem;
                }
            }
        }

        private void CloseItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is WindowItemViewModel windowItem)
            {
                _windowManager.CloseWindow(windowItem.Handle);
            }
        }

        private void MinimizeRestoreItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is WindowItemViewModel windowItem)
            {
                if (windowItem.IsMinimized)
                {
                    // 最小化されている → 通常画面に復元してアクティブ化
                    _windowManager.ActivateWindow(windowItem.Handle);
                }
                else
                {
                    // 通常・最大化状態 → 最小化
                    _windowManager.MinimizeWindow(windowItem.Handle);
                }
            }
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _appBarController?.TogglePinMode();
        }

        private void ApplyMode()
        {
            if (_appBarController?.IsPinned == true)
            {
                if (_autoHideTimer != null) _autoHideTimer.Stop();
                this.Topmost = false;
            }
            else
            {
                this.Topmost = true;
                
                if (_autoHideTimer == null)
                {
                    _autoHideTimer = new DispatcherTimer();
                    _autoHideTimer.Interval = TimeSpan.FromMilliseconds(500);
                    _autoHideTimer.Tick += (s, e) => _appBarController?.HideWindow();
                }
            }
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_appBarController?.IsPinned == false)
            {
                _autoHideTimer?.Stop();
                _appBarController?.ShowWindow();
            }
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_appBarController?.IsPinned == false && _appBarController?.IsHidden == false)
            {
                _autoHideTimer?.Start();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Temporarily unregister to allow free movement between monitors
                _appBarController?.UnregisterAppBar();
                
                this.DragMove();
                
                // Re-docking logic will re-register the AppBar if pinned
                _appBarController?.DetectEdgeAndDock();
                UpdateUIForEdge(_appBarController?.CurrentEdge ?? AppBarManager.ABEdge.ABE_RIGHT);
            }
        }

        private double _tempWidth;

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _tempWidth = this.Width;
        }

        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (_appBarController?.CurrentEdge == AppBarManager.ABEdge.ABE_RIGHT)
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
                _appBarController?.PreviewWidth((int)_tempWidth);
            }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_tempWidth >= 100 && _tempWidth <= 800)
            {
                _appBarController?.UpdateWidth((int)_tempWidth);
            }
        }

    }
}