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
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments were passed.");
            return;
        }

        if (args[0] == "install")
            Install(args[1]);
    }

    static void Install(string appName)
    {
        string appDataPath = "Not Initialized";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + appName);
        }

        Console.WriteLine($"Installing {appName}...");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        Console.WriteLine(appDataPath);
        string url = "https://github.com/ravendevteam/scratchpad/releases/download/v1.1.0/scratchpad.exe";

        DownloadFile(url, appDataPath + Path.DirectorySeparatorChar + appName + ".exe");
        Console.WriteLine($"{appName} has been installed to {appDataPath}");
    }

    static void Update()
    {
        Console.WriteLine("Updating toolbox...");
        //DownloadFile();
    }

    static void DownloadFile(string url, string fileName)
    {
        using (var client = new WebClient())
        {
            client.DownloadFile(url, fileName);
        }
    }
}