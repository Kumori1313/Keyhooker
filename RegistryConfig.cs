using Microsoft.Win32;

namespace Keyhooker_V2;

public static class RegistryConfig
{
    private const string AppKey = @"Software\KeyhookerV2";
    private const string BindingsKey = AppKey + @"\Bindings";
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyhookerV2";

    public static List<Keybinding> LoadBindings()
    {
        var bindings = new List<Keybinding>();
        using var key = Registry.CurrentUser.OpenSubKey(BindingsKey);
        if (key == null) return bindings;

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var sub = key.OpenSubKey(subKeyName);
            if (sub == null) continue;

            if (int.TryParse(subKeyName, out int id))
            {
                bindings.Add(new Keybinding
                {
                    Id = id,
                    Keys = sub.GetValue("Keys") as string ?? string.Empty,
                    Command = sub.GetValue("Command") as string ?? string.Empty,
                    Args = sub.GetValue("Args") as string ?? string.Empty
                });
            }
        }

        return bindings.OrderBy(b => b.Id).ToList();
    }

    public static void SaveBindings(List<Keybinding> bindings)
    {
        Registry.CurrentUser.DeleteSubKeyTree(BindingsKey, false);

        using var key = Registry.CurrentUser.CreateSubKey(BindingsKey);
        for (int i = 0; i < bindings.Count; i++)
        {
            using var sub = key.CreateSubKey(i.ToString());
            sub.SetValue("Keys", bindings[i].Keys);
            sub.SetValue("Command", bindings[i].Command);
            sub.SetValue("Args", bindings[i].Args);
        }
    }

    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey);
        return key?.GetValue(AppName) != null;
    }

    public static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? "";
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
