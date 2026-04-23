using System;
using System.Windows.Media;

namespace SideBarTaskSwitcher.ViewModels
{
    public class WindowItemViewModel
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public ImageSource? IconSource { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsSeparator { get; set; }
        public int MonitorIndex { get; set; }
    }
}
