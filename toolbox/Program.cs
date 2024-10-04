using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace toolbox;

class Program
{
    static void Main(string[] args)
    {
        if (args[0] == "install")
            Install(args[1]);
        
        else if (args[0] == "update")
            Update();

        else
        {
            Console.WriteLine("No arguments were passed.");
            return;
        }
    }

    static void Install(string appName)
    {
        string appDataPath = "Not Initialized";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + "ravensoftware"+ Path.DirectorySeparatorChar + appName);
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
            "https://cdn.discordapp.com/attachments/1184970674524139571/1291818096855617588/idk.json?ex=67017adb&is=6700295b&hm=2f947935b16111f78e00baacf8d8fb9eaaf5db09413e97faef1f702310c9d740&";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + "ravensoftware"+ Path.DirectorySeparatorChar + "toolbox");
        }
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        Console.WriteLine("Updating packages list...");
        DownloadFile(updateUrl, appDataPath + Path.DirectorySeparatorChar + "packages.json");
    }

    static void DownloadFile(string url, string fileName)
    {
        using (var client = new WebClient())
        {
            client.DownloadFile(url, fileName);
        }
    }
}