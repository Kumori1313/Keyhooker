using System.Runtime.InteropServices;

namespace Keyhooker_V2;

public class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly HotkeyWindow _window;
    private readonly Dictionary<int, Keybinding> _bindings = new();
    private int _nextId = 1;

    public event Action<Keybinding>? HotkeyPressed;

    public HotkeyManager()
    {
        _window = new HotkeyWindow();
        _window.HotkeyActivated += OnHotkeyActivated;
    }

    public bool Register(Keybinding binding)
    {
        if (!TryParseKeys(binding.Keys, out uint modifiers, out uint vk))
            return false;

        int id = _nextId++;
        if (RegisterHotKey(_window.Handle, id, modifiers | MOD_NOREPEAT, vk))
        {
            _bindings[id] = binding;
            return true;
        }
        return false;
    }

    public void UnregisterAll()
    {
        foreach (var id in _bindings.Keys)
            UnregisterHotKey(_window.Handle, id);

        _bindings.Clear();
        _nextId = 1;
    }

    private void OnHotkeyActivated(int id)
    {
        if (_bindings.TryGetValue(id, out var binding))
            HotkeyPressed?.Invoke(binding);
    }

    public static bool TryParseKeys(string keys, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        var parts = keys.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl" or "control":
                    modifiers |= MOD_CONTROL; break;
                case "alt":
                    modifiers |= MOD_ALT; break;
                case "shift":
                    modifiers |= MOD_SHIFT; break;
                case "win" or "windows":
                    modifiers |= MOD_WIN; break;
                default:
                    return false;
            }
        }

        var keyName = parts[^1].ToUpperInvariant();

        // Single alphanumeric character — VK codes match ASCII for A-Z and 0-9
        if (keyName.Length == 1)
        {
            char c = keyName[0];
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                vk = (uint)c;
                return true;
            }
        }

        // Named keys
        vk = keyName switch
        {
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            "SPACE" => 0x20,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27,
            "NUMPAD0" => 0x60, "NUMPAD1" => 0x61, "NUMPAD2" => 0x62,
            "NUMPAD3" => 0x63, "NUMPAD4" => 0x64, "NUMPAD5" => 0x65,
            "NUMPAD6" => 0x66, "NUMPAD7" => 0x67, "NUMPAD8" => 0x68,
            "NUMPAD9" => 0x69,
            "PRINTSCREEN" or "PRTSC" => 0x2C,
            "SCROLLLOCK" => 0x91,
            "PAUSE" => 0x13,
            _ => 0
        };

        return vk != 0;
    }

    public void Dispose()
    {
        UnregisterAll();
        _window.DestroyHandle();
    }

    private class HotkeyWindow : NativeWindow
    {
        public event Action<int>? HotkeyActivated;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
                HotkeyActivated?.Invoke((int)m.WParam);

            base.WndProc(ref m);
        }
    }
}
