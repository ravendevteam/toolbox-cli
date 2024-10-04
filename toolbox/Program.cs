using System.Net;

namespace toolbox;

abstract class Program
{
    static void Main(string[] args)
    {
        string jsonFilePath = "bad";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            jsonFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            jsonFilePath = string.Concat(jsonFilePath + Path.DirectorySeparatorChar + "ravensoftware" +
                                         Path.DirectorySeparatorChar + "toolbox" + Path.DirectorySeparatorChar +
                                         "packages.json");
        }
        
        if(!File.Exists(jsonFilePath) && args[0] != "update")
        {
            Console.WriteLine("The packages.json file does not exist. Please run the update command.");
                return;
        }

        if (args[0] == "install")
            Install(args[1]);

        else if (args[0] == "update")
            Update();

        else
        {
            Console.WriteLine("No arguments were passed.");
        }
    }

    static void Install(string appName)
    {
        string appDataPath = "Not Initialized";

        // Get the app data path, and check if the folder exists. If it doesn't, create it.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + appName);
        }

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        Console.WriteLine($"Installing {appName}...");
        string url = "https://github.com/ravendevteam/scratchpad/releases/download/v1.1.0/scratchpad.exe";

        DownloadFile(url, appDataPath + Path.DirectorySeparatorChar + appName + ".exe");
        Console.WriteLine($"{appName} has been installed to {appDataPath}");
    }

    static void Update()
    {
        string appDataPath = "Not Initialized";
        string updateUrl =
            "https://raw.githubusercontent.com/ravendevteam/toolbox/refs/heads/main/toolbox/packages.json";

        // Get the app data path, and check if the folder exists. If it doesn't, create it.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + "toolbox");
        }

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // Download the packages list.
        Console.WriteLine("Updating package list...");
        DownloadFile(updateUrl, appDataPath + Path.DirectorySeparatorChar + "packages.json");
        Console.WriteLine("Package list has been updated.");
    }

    static void DownloadFile(string url, string fileName)
    {
#pragma warning disable SYSLIB0014
        // Perhaps use httpclient instead? This works so let's keep it for now.
        using var client = new WebClient();
#pragma warning restore SYSLIB0014
        client.DownloadFile(url, fileName);
    }
}