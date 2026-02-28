namespace Keyhooker_V2;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "KeyhookerV2_SingleInstance", out bool isNew);
        if (!isNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
