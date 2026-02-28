using System.Diagnostics;

namespace Keyhooker_V2;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ToolStripMenuItem _autoStartItem;
    private ConfigForm? _configForm;

    public TrayApplicationContext()
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        _autoStartItem = new ToolStripMenuItem("Run on Startup")
        {
            Checked = RegistryConfig.IsAutoStartEnabled(),
            CheckOnClick = true
        };
        _autoStartItem.Click += (_, _) =>
            RegistryConfig.SetAutoStart(_autoStartItem.Checked);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Keyhooker V2",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Configure...", null, (_, _) => ShowConfig()),
                    _autoStartItem,
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication())
                }
            }
        };

        _trayIcon.DoubleClick += (_, _) => ShowConfig();

        LoadAndRegisterHotkeys();
    }

    private void LoadAndRegisterHotkeys()
    {
        _hotkeyManager.UnregisterAll();
        var bindings = RegistryConfig.LoadBindings();
        int failed = 0;

        foreach (var binding in bindings)
        {
            if (!_hotkeyManager.Register(binding))
                failed++;
        }

        if (failed > 0)
        {
            _trayIcon.ShowBalloonTip(3000, "Keyhooker V2",
                $"{failed} hotkey(s) failed to register (may conflict with other apps).",
                ToolTipIcon.Warning);
        }
    }

    private void OnHotkeyPressed(Keybinding binding)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = binding.Command,
                Arguments = binding.Args,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(3000, "Keyhooker V2",
                $"Failed to launch: {binding.Command}\n{ex.Message}",
                ToolTipIcon.Error);
        }
    }

    private void ShowConfig()
    {
        if (_configForm != null && !_configForm.IsDisposed)
        {
            _configForm.Activate();
            return;
        }

        _configForm = new ConfigForm();
        if (_configForm.ShowDialog() == DialogResult.OK)
            LoadAndRegisterHotkeys();

        _configForm.Dispose();
        _configForm = null;
    }

    private void ExitApplication()
    {
        _hotkeyManager.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(60, 120, 216));
        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        g.DrawString("K", font, brush, 1, 0);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
