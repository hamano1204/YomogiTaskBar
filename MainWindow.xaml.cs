using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SideBarTaskSwitcher.Managers;
using SideBarTaskSwitcher.ViewModels;

namespace SideBarTaskSwitcher
{
    public partial class MainWindow : Window
    {
        private AppBarManager _appBarManager;
        private WindowManager _windowManager;
        private ObservableCollection<WindowItemViewModel> _windows;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _windowManager = new WindowManager();
            _windows = new ObservableCollection<WindowItemViewModel>();
            WindowsList.ItemsSource = _windows;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _appBarManager = new AppBarManager(this);
            _appBarManager.Register((int)this.Width);

            RefreshWindowList();

            // Set up 2-second timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (s, ev) => RefreshWindowList();
            _timer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _appBarManager?.Unregister();
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
    }
}