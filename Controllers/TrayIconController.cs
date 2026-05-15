using System;
using System.Reflection;
using System.Windows;
using Forms = System.Windows.Forms;
using YomogiTaskBar.Utilities;

namespace YomogiTaskBar.Controllers
{
    public class TrayIconController : IDisposable
    {
        private Forms.NotifyIcon? _notifyIcon;
        private readonly Window _mainWindow;

        public TrayIconController(Window mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public void Setup()
        {
            try
            {
                _notifyIcon = new Forms.NotifyIcon();
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                _notifyIcon.Visible = true;
                _notifyIcon.Text = "YomogiTaskBar";

                var contextMenuStrip = new Forms.ContextMenuStrip();
                var closeMenuItem = new Forms.ToolStripMenuItem("閉じる");
                closeMenuItem.Click += (s, args) => _mainWindow.Close();
                contextMenuStrip.Items.Add(closeMenuItem);

                _notifyIcon.ContextMenuStrip = contextMenuStrip;
                
                Logger.LogInfo("NotifyIcon setup completed", "TrayIconController");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to setup NotifyIcon", ex, "TrayIconController");
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
                Logger.LogInfo("NotifyIcon disposed", "TrayIconController");
            }
        }
    }
}
