using System.Net;
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
        
        else if (args[0] == "remove")
            Remove(args[1]);
        
        else if (args[0] == "upgrade")
            Upgrade(args[1]);

        else
        {
            Console.WriteLine("No arguments were passed.");
        }
    }

    static void Install(string appName)
    {
        string executableDirectory = "Not Initialized";
        string executablePath = "Not Initialized";
        string shortcutPath = "Not Initialized";

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
            executableDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            executableDirectory = string.Concat(executableDirectory + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + appName);
            
            executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";
            
            shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            shortcutPath = string.Concat(shortcutPath + Path.DirectorySeparatorChar + "Microsoft" +
                                         Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                         "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                         Path.DirectorySeparatorChar + appName + ".lnk");
        }

        if (!Directory.Exists(executableDirectory))
            Directory.CreateDirectory(executableDirectory);

        Console.WriteLine($"Installing {appName}...");
        DownloadFile(package.Url, executablePath);
        
        // Create a shortcut
        Console.WriteLine("Creating shortcut...");
        
        Console.WriteLine($"{appName} has been installed to {executableDirectory}");
    }
    
    static void Remove(string appName)
    {
        string executableDirectory = "Not Initialized";
        string executablePath = "Not Initialized";
        string shortcutPath = "Not Initialized";
        
        string json = File.ReadAllText(_packageListPath);

        // Deserialize the JSON content into C# objects
        var packageList = JsonSerializer.Deserialize<PackageList>(json);

        var package = packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));
        
        if (package == null)
        {
            Console.WriteLine($"Package {appName} not found in the package list.");
            return;
        }

        // Get the app data path, and check if the folder exists. If it doesn't, create it.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            executableDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            executableDirectory = string.Concat(executableDirectory + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + appName);
            
            executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";
            
            shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            shortcutPath = string.Concat(shortcutPath + Path.DirectorySeparatorChar + "Microsoft" +
                                         Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                         "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                         Path.DirectorySeparatorChar + appName + ".lnk");
        }

        if (!Directory.Exists(executableDirectory))
        {
            Console.WriteLine($"{appName} is not installed.");
            return;
        }
        
        Console.WriteLine($"Name: {package.Name}");
        Console.WriteLine("Okay to remove? Y/n");
        
        string? response = Console.ReadLine();
        response = response?.ToLower();
        
        if (response != "y" && response != "yes" && response != "")
        {
            Console.WriteLine("Cancelling...");
            return;
        }

        Console.WriteLine($"Removing {appName}...");
        Directory.Delete(executableDirectory, true);
        
        // Remove the shortcut
        Console.WriteLine("Removing shortcut...");
        
        Console.WriteLine($"{appName} has been removed.");
    }
    
    static void Upgrade(string appName)
    {
        string executableDirectory = "Not Initialized";
        string executablePath = "Not Initialized";

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
        Console.WriteLine("Okay to upgrade? Y/n");
        
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
            executableDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            executableDirectory = string.Concat(executableDirectory + Path.DirectorySeparatorChar + "ravensoftware" +
                                        Path.DirectorySeparatorChar + appName);
            
            executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";
        }

        if (!Directory.Exists(executableDirectory))
            Directory.CreateDirectory(executableDirectory);

        Console.WriteLine($"Upgrading {appName}...");
        DownloadFile(package.Url, executablePath);
        
        Console.WriteLine($"{appName} has been upgraded.");
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
        using var client = new WebClient();

        // Subscribe to the DownloadProgressChanged event
        client.DownloadProgressChanged += (sender, e) =>
        {
            // Update the progress bar
            Console.Write($"\rDownloading: [{new string('#', e.ProgressPercentage / 2)}{new string(' ', 50 - e.ProgressPercentage / 2)}] {e.ProgressPercentage}%");
        };

        // Subscribe to the DownloadFileCompleted event
        client.DownloadFileCompleted += (sender, e) =>
        {
            Console.WriteLine("\nDownload completed!");
        };

        // Start the download asynchronously
        client.DownloadFileAsync(new Uri(url), fileName);
#pragma warning restore SYSLIB0014

        // Keep the application running until the download is complete
        while (client.IsBusy)
        {
            Thread.Sleep(100);
        }
    }
}