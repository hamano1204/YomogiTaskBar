using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using YomogiTaskBar.Managers;
using YomogiTaskBar.Models;
using YomogiTaskBar.Utilities;
using Forms = System.Windows.Forms;

namespace YomogiTaskBar.Controllers
{
    /// <summary>
    /// Manages AppBar functionality and edge detection
    /// </summary>
    public class AppBarController
    {
        private readonly Window _window;
        private readonly IntPtr _windowHandle;
        private AppBarManager? _appBarManager;
        private bool _isPinned = true;
        private bool _isHidden = false;

        public AppBarManager.ABEdge CurrentEdge => _appBarManager?.Edge ?? AppBarManager.ABEdge.ABE_RIGHT;
        public bool IsPinned => _isPinned;
        public bool IsHidden => _isHidden;

        public AppBarController(Window window, IntPtr windowHandle)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _windowHandle = windowHandle;
        }

        /// <summary>
        /// Initializes the AppBar manager
        /// </summary>
        public void Initialize()
        {
            try
            {
                Logger.LogOperationStart("Initializing AppBar", "AppBar");
                _appBarManager = new AppBarManager(_window);
                Logger.LogOperationComplete("AppBar initialized", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize AppBar", ex, "AppBar");
                throw;
            }
        }

        /// <summary>
        /// Registers the AppBar with the specified edge and width
        /// </summary>
        public void RegisterAppBar(int width, AppBarManager.ABEdge edge = AppBarManager.ABEdge.ABE_RIGHT)
        {
            try
            {
                if (_appBarManager == null)
                {
                    Logger.LogWarning("AppBar manager not initialized", "AppBar");
                    return;
                }

                Logger.LogInfo($"Registering AppBar with edge {edge}, width {width}", "AppBar");
                _appBarManager.Register(width, edge);
                _appBarManager.SizeAppBar();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to register AppBar", ex, "AppBar");
            }
        }

        /// <summary>
        /// Unregisters the AppBar
        /// </summary>
        public void UnregisterAppBar()
        {
            try
            {
                _appBarManager?.Unregister();
                Logger.LogInfo("AppBar unregistered", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to unregister AppBar", ex, "AppBar");
            }
        }

        /// <summary>
        /// Toggles between pinned and unpinned modes
        /// </summary>
        public void TogglePinMode()
        {
            try
            {
                _isPinned = !_isPinned;
                Logger.LogInfo($"Pin mode toggled to: {_isPinned}", "AppBar");
                
                if (_isPinned)
                {
                    _isHidden = false;
                    RegisterAppBar((int)_window.Width, CurrentEdge);
                    _window.Topmost = false;
                }
                else
                {
                    UnregisterAppBar();
                    _window.Topmost = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to toggle pin mode", ex, "AppBar");
            }
        }

        /// <summary>
        /// Shows the window from hidden state
        /// </summary>
        public void ShowWindow()
        {
            if (!_isHidden) return;
            
            try
            {
                _isHidden = false;
                var screen = Forms.Screen.FromHandle(_windowHandle);
                var bounds = screen.Bounds;
                
                var source = PresentationSource.FromVisual(_window);
                double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? WindowConstants.DefaultDpiX;
                double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? WindowConstants.DefaultDpiY;

                double left = CurrentEdge == AppBarManager.ABEdge.ABE_LEFT 
                    ? bounds.Left / dpiX 
                    : (bounds.Right / dpiX) - _window.Width;
                double top = bounds.Top / dpiY;

                _window.Left = left;
                _window.Top = top;
                
                Logger.LogInfo("Window shown from hidden state", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to show window", ex, "AppBar");
            }
        }

        /// <summary>
        /// Hides the window (edge trigger mode)
        /// </summary>
        public void HideWindow()
        {
            if (_isPinned || _isHidden) return;
            
            try
            {
                _isHidden = true;
                var screen = Forms.Screen.FromHandle(_windowHandle);
                var bounds = screen.Bounds;
                
                var source = PresentationSource.FromVisual(_window);
                double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? WindowConstants.DefaultDpiX;
                double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? WindowConstants.DefaultDpiY;

                double left = CurrentEdge == AppBarManager.ABEdge.ABE_LEFT 
                    ? (bounds.Left / dpiX) - _window.Width + WindowConstants.VisibleStripWidth
                    : (bounds.Right / dpiX) - WindowConstants.VisibleStripWidth;
                double top = bounds.Top / dpiY;

                _window.Left = left;
                _window.Top = top;
                
                Logger.LogInfo("Window hidden (edge trigger mode)", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to hide window", ex, "AppBar");
            }
        }

        /// <summary>
        /// Docks the window to the specified edge
        /// </summary>
        public void DockToEdge(AppBarManager.ABEdge edge, System.Drawing.Rectangle? targetBounds = null)
        {
            try
            {
                if (_isPinned)
                {
                    _appBarManager?.Register((int)_window.Width, edge);
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
                    var source = PresentationSource.FromVisual(_window);
                    double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? WindowConstants.DefaultDpiX;
                    double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? WindowConstants.DefaultDpiY;

                    _window.Top = bounds.Top / dpiY;
                    _window.Height = bounds.Height / dpiY;
                    
                    if (edge == AppBarManager.ABEdge.ABE_LEFT)
                    {
                        _window.Left = bounds.Left / dpiX;
                    }
                    else
                    {
                        _window.Left = (bounds.Right / dpiX) - _window.Width;
                    }
                }
                
                Logger.LogInfo($"Docked to edge: {edge}", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to dock to edge", ex, "AppBar");
            }
        }

        /// <summary>
        /// Updates the AppBar width
        /// </summary>
        public void UpdateWidth(int width)
        {
            try
            {
                _appBarManager?.UpdateWidth(width);
                Logger.LogInfo($"AppBar width updated to: {width}", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to update AppBar width", ex, "AppBar");
            }
        }

        /// <summary>
        /// Previews the AppBar width change
        /// </summary>
        public void PreviewWidth(int width)
        {
            try
            {
                _appBarManager?.PreviewWidth(width);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to preview AppBar width", ex, "AppBar");
            }
        }

        /// <summary>
        /// Detects the edge based on mouse position and docks accordingly
        /// </summary>
        public void DetectEdgeAndDock()
        {
            try
            {
                var mousePos = Forms.Control.MousePosition;
                var screen = Forms.Screen.FromPoint(mousePos);
                var bounds = screen.Bounds;

                double threshold = bounds.Width * WindowConstants.EdgeDetectionThreshold;

                if (mousePos.X < bounds.Left + threshold)
                {
                    DockToEdge(AppBarManager.ABEdge.ABE_LEFT, bounds);
                }
                else if (mousePos.X > bounds.Right - threshold)
                {
                    DockToEdge(AppBarManager.ABEdge.ABE_RIGHT, bounds);
                }
                else
                {
                    DockToEdge(CurrentEdge, bounds);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to detect edge and dock", ex, "AppBar");
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                UnregisterAppBar();
                _appBarManager = null;
                Logger.LogInfo("AppBarController disposed", "AppBar");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to dispose AppBarController", ex, "AppBar");
            }
        }
    }
}
