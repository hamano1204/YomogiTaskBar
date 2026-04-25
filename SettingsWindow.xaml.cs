using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YomogiTaskBar.Models;
using YomogiTaskBar.Managers;

namespace YomogiTaskBar
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public AppSettings CurrentSettings { get; private set; }
        private System.Windows.Controls.Button? _activeButton;
        private bool _isInitializing = false;

        private bool _isRecording = false;
        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged();
                }
            }
        }

        public SettingsWindow(AppSettings settings)
        {
            _isInitializing = true;
            InitializeComponent();
            
            // Create a deep copy of settings so we can cancel changes
            CurrentSettings = new AppSettings
            {
                ThemeMode = settings.ThemeMode,
                LaunchOnStartup = settings.LaunchOnStartup,
                GlobalActivate = new ShortcutConfig { Key = settings.GlobalActivate.Key, Modifiers = settings.GlobalActivate.Modifiers },
                Minimize = new ShortcutConfig { Key = settings.Minimize.Key, Modifiers = settings.Minimize.Modifiers },
                ToggleMaximize = new ShortcutConfig { Key = settings.ToggleMaximize.Key, Modifiers = settings.ToggleMaximize.Modifiers },
                Close = new ShortcutConfig { Key = settings.Close.Key, Modifiers = settings.Close.Modifiers },
                NextMonitor = new ShortcutConfig { Key = settings.NextMonitor.Key, Modifiers = settings.NextMonitor.Modifiers },
                PrevMonitor = new ShortcutConfig { Key = settings.PrevMonitor.Key, Modifiers = settings.PrevMonitor.Modifiers }
            };

            UpdateUI();
            _isInitializing = false;
        }

        private void UpdateUI()
        {
            BtnGlobalActivate.Content = CurrentSettings.GlobalActivate.ToString();
            BtnMinimize.Content = CurrentSettings.Minimize.ToString();
            BtnToggleMaximize.Content = CurrentSettings.ToggleMaximize.ToString();
            BtnClose.Content = CurrentSettings.Close.ToString();
            BtnNextMonitor.Content = CurrentSettings.NextMonitor.ToString();
            BtnPrevMonitor.Content = CurrentSettings.PrevMonitor.ToString();
            LaunchOnStartupCheckBox.IsChecked = CurrentSettings.LaunchOnStartup;

            // Initialize Theme ComboBox
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag.ToString() == CurrentSettings.ThemeMode)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeButton != null)
            {
                ResetActiveButton();
            }

            _activeButton = (System.Windows.Controls.Button)sender;
            _activeButton.Content = "キー入力を待機中...";
            IsRecording = true;  // INotifyPropertyChanged 経由でXAMLに通知される
            
            this.PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void ResetActiveButton()
        {
            if (_activeButton == null) return;

            string? tag = _activeButton.Tag?.ToString();
            ShortcutConfig? config = tag switch
            {
                "GlobalActivate" => CurrentSettings.GlobalActivate,
                "Minimize" => CurrentSettings.Minimize,
                "ToggleMaximize" => CurrentSettings.ToggleMaximize,
                "Close" => CurrentSettings.Close,
                "NextMonitor" => CurrentSettings.NextMonitor,
                "PrevMonitor" => CurrentSettings.PrevMonitor,
                _ => null
            };
            
            _activeButton.Content = config?.ToString() ?? "未設定";
            _activeButton = null;
            IsRecording = false;  // INotifyPropertyChanged 経由でXAMLに通知される
            
            this.PreviewKeyDown -= Window_PreviewKeyDown;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_activeButton == null) return;

            // Handle system keys like Alt
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Ignore modifier-only presses
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // MARK HANDLED IMMEDIATELY to prevent window-level processing (like Esc closing the dialog)
            e.Handled = true;

            ModifierKeys modifiers = Keyboard.Modifiers;
            
            // Special handling for Win key if not captured by Keyboard.Modifiers
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            {
                modifiers |= ModifierKeys.Windows;
            }

            var config = new ShortcutConfig { Key = key, Modifiers = modifiers };
            
            string tag = _activeButton.Tag?.ToString() ?? string.Empty;
            switch (tag)
            {
                case "GlobalActivate": CurrentSettings.GlobalActivate = config; break;
                case "Minimize": CurrentSettings.Minimize = config; break;
                case "ToggleMaximize": CurrentSettings.ToggleMaximize = config; break;
                case "Close": CurrentSettings.Close = config; break;
                case "NextMonitor": CurrentSettings.NextMonitor = config; break;
                case "PrevMonitor": CurrentSettings.PrevMonitor = config; break;
            }

            _activeButton.Content = config.ToString();
            
            // Reset state using the helper
            var btn = _activeButton; // Keep reference to reset it
            _activeButton = null; // Clear first so ResetActiveButton (if called elsewhere) doesn't interfere, though we are doing it manually here
            
            btn.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "InputBackgroundBrush");
            btn.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "PrimaryTextBrush");
            this.PreviewKeyDown -= Window_PreviewKeyDown;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            StartupManager.Apply(CurrentSettings.LaunchOnStartup);
            SettingsManager.Save(CurrentSettings);
            this.DialogResult = true;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                CurrentSettings.ThemeMode = item.Tag.ToString() ?? "System";
                ThemeManager.ApplyTheme(CurrentSettings.ThemeMode);
            }
        }

        private void LaunchOnStartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            CurrentSettings.LaunchOnStartup = LaunchOnStartupCheckBox.IsChecked == true;
        }
    }
}
