using System;

namespace YomogiTaskBar.Models
{
    /// <summary>
    /// Window-related constants used throughout the application
    /// </summary>
    public static class WindowConstants
    {
        /// <summary>
        /// Minimum window width in pixels
        /// </summary>
        public const int MinWindowWidth = 100;
        
        /// <summary>
        /// Maximum window width in pixels
        /// </summary>
        public const int MaxWindowWidth = 800;
        
        /// <summary>
        /// Default window width in pixels
        /// </summary>
        public const double DefaultWindowWidth = 300;
        
        /// <summary>
        /// Visible strip width for edge trigger mode in logical units
        /// </summary>
        public const double VisibleStripWidth = 2.0;
        
        /// <summary>
        /// Auto-hide delay in milliseconds
        /// </summary>
        public const int AutoHideDelayMs = 500;
        
        /// <summary>
        /// Window refresh timer interval in seconds
        /// </summary>
        public const int RefreshIntervalSeconds = 1;
        
        /// <summary>
        /// Desktop operation delay in milliseconds
        /// </summary>
        public const int DesktopOperationDelayMs = 500;
        
        /// <summary>
        /// Edge detection threshold as percentage of screen width
        /// </summary>
        public const double EdgeDetectionThreshold = 0.1;
        
        /// <summary>
        /// DPI fallback values
        /// </summary>
        public const double DefaultDpiX = 1.0;
        public const double DefaultDpiY = 1.0;
    }
}
