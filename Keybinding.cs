namespace Keyhooker_V2;

public class Keybinding
{
    public int Id { get; set; }
    public string Keys { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;
}
