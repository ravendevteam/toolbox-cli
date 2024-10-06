using System.Text.Json;

namespace toolbox;

class Toolbox
{
    public const string Version = "2.0.0";

    static void Main(string[] args)
    {
        if (args.Length >= 2)
            PackageCommands.Launcher(args);
    }
}