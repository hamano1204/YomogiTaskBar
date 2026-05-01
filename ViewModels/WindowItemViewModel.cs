using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace YomogiTaskBar.ViewModels
{
    public class WindowItemViewModel : INotifyPropertyChanged
    {
        private IntPtr _handle;
        private string _title = string.Empty;
        private int _processId;
        private ImageSource? _iconSource;
        private bool _isMinimized;
        private bool _isSeparator;
        private bool _isDesktopSeparator;
        private int _monitorIndex;
        private bool _isActive;
        private Guid _desktopId;
        private string _desktopName = string.Empty;

        public IntPtr Handle
        {
            get => _handle;
            set { if (_handle != value) { _handle = value; OnPropertyChanged(); } }
        }

        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        public int ProcessId
        {
            get => _processId;
            set { if (_processId != value) { _processId = value; OnPropertyChanged(); } }
        }

        public ImageSource? IconSource
        {
            get => _iconSource;
            set { if (_iconSource != value) { _iconSource = value; OnPropertyChanged(); } }
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set { if (_isMinimized != value) { _isMinimized = value; OnPropertyChanged(); } }
        }

        public bool IsSeparator
        {
            get => _isSeparator;
            set { if (_isSeparator != value) { _isSeparator = value; OnPropertyChanged(); } }
        }

        public bool IsDesktopSeparator
        {
            get => _isDesktopSeparator;
            set { if (_isDesktopSeparator != value) { _isDesktopSeparator = value; OnPropertyChanged(); } }
        }

        public int MonitorIndex
        {
            get => _monitorIndex;
            set { if (_monitorIndex != value) { _monitorIndex = value; OnPropertyChanged(); } }
        }

        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        public Guid DesktopId
        {
            get => _desktopId;
            set { if (_desktopId != value) { _desktopId = value; OnPropertyChanged(); } }
        }

        public string DesktopName
        {
            get => _desktopName;
            set { if (_desktopName != value) { _desktopName = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
