﻿using System.Net;
using System.Text.Json;

namespace toolbox;

abstract class Program
{
    static string _packageListPath = "bad";

    static void Main(string[] args)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            _packageListPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _packageListPath = string.Concat(_packageListPath + Path.DirectorySeparatorChar + "ravensoftware" +
                                            Path.DirectorySeparatorChar + "toolbox" + Path.DirectorySeparatorChar +
                                            "packages.json");
        }

        if (!File.Exists(_packageListPath) && args[0] != "update")
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

        // Read the file content
        string json = File.ReadAllText(_packageListPath);

        // Deserialize the JSON content into C# objects
        var packageList = JsonSerializer.Deserialize<PackageList>(json);

        var package = packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));
        
        if (package == null)
        {
            Console.WriteLine($"Package {appName} not found in the package list.");
            return;
        }
        
        Console.WriteLine($"Name: {package.Name}");
        Console.WriteLine($"Version: {package.Version}");
        Console.WriteLine($"URL: {package.Url}");
        Console.WriteLine($"Description: {package.Description}");
        Console.WriteLine("Okay to install? Y/n");
        
        string? response = Console.ReadLine();
        response = response?.ToLower();
        
        if (response != "y" && response != "yes" && response != "")
        {
            Console.WriteLine("Cancelling...");
            return;
        }
        
        // Get the app data path, and check if the folder exists. If it doesn't, create it.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appDataPath = string.Concat(appDataPath + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + appName);
        }

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        Console.WriteLine($"Installing {appName}...");

        DownloadFile(package.Url, appDataPath + Path.DirectorySeparatorChar + appName + ".exe");
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