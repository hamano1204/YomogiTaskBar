using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using YomogiTaskBar.Managers;
using YomogiTaskBar.Models;
using YomogiTaskBar.Utilities;
using Forms = System.Windows.Forms;

namespace YomogiTaskBar.Controllers
{
    /// <summary>
    /// Manages window state persistence and restoration
    /// </summary>
    public class WindowStateManager
    {
        private readonly Window _window;
        private readonly IntPtr _windowHandle;
        private AppSettings _settings;

        public WindowStateManager(Window window, IntPtr windowHandle, AppSettings settings)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _windowHandle = windowHandle;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Saves current window settings
        /// </summary>
        public void SaveWindowSettings(AppBarManager.ABEdge currentEdge)
        {
            try
            {
                Logger.LogOperationStart("Saving window settings", "WindowState");

                // Save current window settings
                _settings.WindowSettings.IsAppBarMode = true; // Always pinned mode for stability
                _settings.WindowSettings.Edge = currentEdge;
                _settings.WindowSettings.WindowWidth = Math.Clamp(_window.Width, WindowConstants.MinWindowWidth, WindowConstants.MaxWindowWidth);
                
                // Get current monitor information
                var currentScreen = Forms.Screen.FromHandle(_windowHandle);
                var screens = Forms.Screen.AllScreens;
                _settings.WindowSettings.MonitorIndex = Array.IndexOf(screens, currentScreen);
                _settings.WindowSettings.LastMonitorCount = screens.Length;
                
                // Save settings
                SettingsManager.Save(_settings);
                
                Logger.LogOperationComplete("Window settings saved", "WindowState");
                Logger.LogDebug($"Saved: Edge={currentEdge}, Width={_settings.WindowSettings.WindowWidth}, Monitor={_settings.WindowSettings.MonitorIndex}", "WindowState");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save window settings", ex, "WindowState");
            }
        }

        /// <summary>
        /// Restores window settings
        /// </summary>
        public WindowSettings RestoreWindowSettings()
        {
            try
            {
                Logger.LogOperationStart("Restoring window settings", "WindowState");

                var screens = Forms.Screen.AllScreens;
                var restoredSettings = new WindowSettings(); // Default settings
                
                // Check if monitor count has changed - if so, reset to first monitor
                if (_settings.WindowSettings.LastMonitorCount != screens.Length)
                {
                    Logger.LogWarning("Monitor count changed, resetting to first monitor", "WindowState");
                    _settings.WindowSettings.MonitorIndex = 0;
                    _settings.WindowSettings.LastMonitorCount = screens.Length;
                }
                
                // Validate monitor index
                if (_settings.WindowSettings.MonitorIndex < 0 || _settings.WindowSettings.MonitorIndex >= screens.Length)
                {
                    Logger.LogWarning("Invalid monitor index, resetting to first monitor", "WindowState");
                    _settings.WindowSettings.MonitorIndex = 0;
                }
                
                // Set window width with validation
                if (_settings.WindowSettings.WindowWidth >= WindowConstants.MinWindowWidth && 
                    _settings.WindowSettings.WindowWidth <= WindowConstants.MaxWindowWidth)
                {
                    _window.Width = _settings.WindowSettings.WindowWidth;
                    restoredSettings.WindowWidth = _settings.WindowSettings.WindowWidth;
                }
                else
                {
                    _window.Width = WindowConstants.DefaultWindowWidth;
                    restoredSettings.WindowWidth = WindowConstants.DefaultWindowWidth;
                    Logger.LogWarning($"Invalid window width, using default: {WindowConstants.DefaultWindowWidth}", "WindowState");
                }
                
                // Move to specified monitor and position based on edge
                if (_settings.WindowSettings.MonitorIndex >= 0 && _settings.WindowSettings.MonitorIndex < screens.Length)
                {
                    var targetScreen = screens[_settings.WindowSettings.MonitorIndex];
                    
                    // Calculate position based on edge
                    var source = PresentationSource.FromVisual(_window);
                    double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? WindowConstants.DefaultDpiX;
                    double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? WindowConstants.DefaultDpiY;
                    
                    _window.Top = targetScreen.Bounds.Top / dpiY;
                    _window.Height = targetScreen.Bounds.Height / dpiY;
                    
                    // Position window according to saved edge
                    if (_settings.WindowSettings.Edge == AppBarManager.ABEdge.ABE_LEFT)
                    {
                        _window.Left = targetScreen.Bounds.Left / dpiX;
                    }
                    else
                    {
                        _window.Left = (targetScreen.Bounds.Right / dpiX) - _window.Width;
                    }
                    
                    restoredSettings.Edge = _settings.WindowSettings.Edge;
                    restoredSettings.MonitorIndex = _settings.WindowSettings.MonitorIndex;
                    
                    Logger.LogInfo($"Positioned window on monitor {_settings.WindowSettings.MonitorIndex} at edge {_settings.WindowSettings.Edge}", "WindowState");
                }
                
                Logger.LogOperationComplete("Window settings restored", "WindowState");
                return restoredSettings;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to restore window settings", ex, "WindowState");
                
                // Return default settings
                return new WindowSettings
                {
                    IsAppBarMode = true,
                    Edge = AppBarManager.ABEdge.ABE_RIGHT,
                    MonitorIndex = 0,
                    WindowWidth = WindowConstants.DefaultWindowWidth,
                    LastMonitorCount = Forms.Screen.AllScreens.Length
                };
            }
        }

        /// <summary>
        /// Validates and clamps window width within acceptable bounds
        /// </summary>
        public double ValidateWindowWidth(double width)
        {
            return Math.Clamp(width, WindowConstants.MinWindowWidth, WindowConstants.MaxWindowWidth);
        }

        /// <summary>
        /// Gets the current monitor index
        /// </summary>
        public int GetCurrentMonitorIndex()
        {
            try
            {
                var currentScreen = Forms.Screen.FromHandle(_windowHandle);
                var screens = Forms.Screen.AllScreens;
                return Array.IndexOf(screens, currentScreen);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to get current monitor index", ex, "WindowState");
                return 0;
            }
        }

        /// <summary>
        /// Checks if the current monitor configuration has changed
        /// </summary>
        public bool HasMonitorConfigurationChanged()
        {
            try
            {
                var currentScreenCount = Forms.Screen.AllScreens.Length;
                return _settings.WindowSettings.LastMonitorCount != currentScreenCount;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to check monitor configuration", ex, "WindowState");
                return false;
            }
        }
    }
}
