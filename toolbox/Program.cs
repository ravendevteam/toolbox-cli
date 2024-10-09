using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace toolbox;

class Toolbox
{
    public const string ToolboxVersion = "2.0.0";

    public static List<string> PackageCommands { get; } = new List<string>
    {
        "install",
        "upgrade",
        "remove"
    };

    public static string? AppdataDir;
    public static string? PackageListPath;

    public static string OperatingSystem = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown";

    public static string? UpdateUrl;

    public static Dictionary<string, string> Packages;

    public static string? Name;
    public static string? Version;
    public static string? Url;
    public static string? Description;
    public static string? Sha256;
    public static bool RequirePath;
    public static bool Shortcut;
    public static List<string> OsList;

    public static string? ExeDir;
    public static string? ExePath;
    public static string? Extension;

    static void Main(string[] args)
    {
        switch (OperatingSystem)
        {
            case "Windows":
                AppdataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ravensoftware");
                break;
            case "macOS":
                AppdataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "ravensoftware");
                break;
            default:
                throw new Exception("Unknown Operating System");
        }

        PackageListPath = Path.Combine(AppdataDir, "toolbox", "packages.json");

        UpdateCheck(args[0]);

        try
        {
            string json = File.ReadAllText(PackageListPath ?? throw new InvalidOperationException());
            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            string pattern = @"^(https?|ftp)://[\w.-]+(\.[\w.-]+)+[\w\-.,@?^=%&:/~+#]*$";
            Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (regex.IsMatch(packageList!.UpdateUrl))
                UpdateUrl = packageList.UpdateUrl;


            if (PackageCommands.Contains(args[0].ToLower()))
            {
                var package =
                    packageList?.Packages.FirstOrDefault(
                        p => p.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase));

                Name = package?.Name;
                Version = package?.Version;
                Url = package?.Url[OperatingSystem];
                Description = package?.Description;
                Sha256 = package?.Sha256[OperatingSystem];
                RequirePath = package.RequirePath;
                Shortcut = package.Shortcut;
                OsList = package?.OsList;

                Extension = Url.Substring(Url.LastIndexOf('.') + 1);

                switch (OperatingSystem)
                {
                    case "Windows":
                        ExeDir = Path.Combine(AppdataDir, Name.ToLower());
                        break;

                    case "macOS":
                        ExeDir = RequirePath ? "/usr/local/bin" : "/Applications";
                        break;
                }

                ExePath = Path.Combine(ExeDir, Name.ToLower() + "." + Extension);
            }
        }
        catch (Exception e)
        {
            Update();
            Console.WriteLine(
                "Toolbox crashed unexpectedly and tried fixing itself. If the issue persists, please report it.");
            return;
        }


        switch (args[0].ToLower())
        {
            case "install":
                Install(args[1]);
                break;
            case "sha256":
                Console.WriteLine(FileTools.GetChecksum(args[1]));
                break;
            case "update":
                Update();
                break;
            case "upgrade":
                Upgrade(args[1]);
                break;
            case "remove":
                Remove(args[1]);
                break;
            case "list":
                List();
                break;
        }
    }

    static void Install(string package)
    {
        Infochecker(package);

        Console.WriteLine("Okay to install? Y/n");

        string? response = Console.ReadLine();
        response = response?.ToLower();

        if (response != "y" && response != "yes" && response != String.Empty)
        {
            Console.WriteLine("Cancelling...");
            return;
        }

        if (!Directory.Exists(ExeDir))
            Directory.CreateDirectory(ExeDir);

        Console.WriteLine($"Installing {Name}...");
        FileTools.DownloadFile(Url, ExePath);

        if (FileTools.GetChecksum(ExePath) != Sha256)
        {
            Console.WriteLine("Checksum mismatch");
            Directory.Delete(ExeDir, true);
            return;
        }

        if (Extension == "zip")
        {
            switch (OperatingSystem)
            {
                case "macOS":
                    ZipFile.ExtractToDirectory(ExePath, ExeDir, true);
                    File.Delete(ExePath);
                    break;
            }
        }

        if (Shortcut)
            switch (OperatingSystem)
            {
                case "Windows":
                    Console.WriteLine("Creating Shortcuts...");
                    WindowsTools.AddShortcut(ExePath, Name, Description);
                    break;
            }

        if (RequirePath)
            switch (OperatingSystem)
            {
                case "Windows":
                    WindowsTools.AddPath(ExeDir);
                    break;
            }

        Console.WriteLine("Installation complete");
    }

    static void Upgrade(string package)
    {
        Infochecker(package);

        Console.WriteLine("Okay to upgrade? Y/n");

        string? response = Console.ReadLine();
        response = response?.ToLower();

        if (response != "y" && response != "yes" && response != String.Empty)
        {
            Console.WriteLine("Cancelling...");
            return;
        }

        if (!Directory.Exists(ExeDir))
            Directory.CreateDirectory(ExeDir);

        Console.WriteLine($"Upgrading {Name}...");
        FileTools.DownloadFile(Url, ExePath);

        if (FileTools.GetChecksum(ExePath) != Sha256)
        {
            Console.WriteLine("Checksum mismatch");
            Directory.Delete(ExeDir, true);
            return;
        }

        if (Extension == "zip")
        {
            switch (OperatingSystem)
            {
                case "macOS":
                    ZipFile.ExtractToDirectory(ExePath, ExeDir, true);
                    File.Delete(ExePath);
                    break;
            }
        }

        if (RequirePath)
            switch (OperatingSystem)
            {
                case "Windows":
                    WindowsTools.AddPath(ExeDir);
                    break;
            }

        Console.WriteLine("Upgrade complete");
    }

    static void Remove(string Package)
    {
        Infochecker(Package);

        Console.WriteLine("Okay to remove? Y/n");

        string? response = Console.ReadLine();
        response = response?.ToLower();

        if (response != "y" && response != "yes" && response != String.Empty)
        {
            Console.WriteLine("Cancelling...");
            return;
        }

        Console.WriteLine($"Removing {Name}...");

        if (Directory.Exists(ExeDir))
            Directory.Delete(ExeDir, true);

        if (File.Exists(ExePath))
            File.Delete(ExePath);

        if (Shortcut)
            switch (OperatingSystem)
            {
                case "Windows":
                    Console.WriteLine("Removing Shortcuts...");
                    WindowsTools.RemoveShortcut(Name);
                    break;
            }

        if (RequirePath)
            switch (OperatingSystem)
            {
                case "Windows":
                    WindowsTools.RemovePath(ExeDir);
                    break;
            }

        Console.WriteLine("Removal complete");
    }

    static void Update()
    {
        string lastUpdatePath = Path.Combine(AppdataDir, "toolbox", "lastupdate");
        if (UpdateUrl == null || !UpdateUrl.Contains("packages.json"))
        {
            UpdateUrl =
                "https://raw.githubusercontent.com/ravendevteam/toolbox/refs/heads/crossplatform/toolbox/packages.json";
            Console.WriteLine("Using default update URL");
        }

        FileTools.DownloadFile(UpdateUrl, PackageListPath);

        using (StreamWriter outputFile = new StreamWriter(lastUpdatePath))
        {
            outputFile.WriteLine(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }

        Console.WriteLine("\nUpdate complete");
    }

    static void UpdateCheck(string args0)
    {
        long timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string lastUpdatePath = Path.Combine(AppdataDir, "toolbox", "lastupdate");

        if (!File.Exists(lastUpdatePath) || !File.Exists(PackageListPath) ||
            !File.ReadAllText(PackageListPath).Contains("updateurl"))
        {
            Console.WriteLine("Automatically updating Raven Toolbox");
            Update();
        }

        else
        {
            long lastUpdate;
            using (StreamReader reader = new StreamReader(lastUpdatePath))
            {
                string lastUpdateString = reader.ReadToEnd();
                lastUpdate = long.Parse(lastUpdateString);
            }

            long timeDif = timeNow - lastUpdate;

            if (timeDif > 86400)
            {
                Console.WriteLine("Automatically updating Raven Toolbox");
                Update();
                if (args0.ToLower() == "update")
                    return;
            }
        }
    }

    static void List()
    {
        string json = File.ReadAllText(PackageListPath ?? throw new InvalidOperationException());

        // Deserialize the JSON content into C# objects
        var packageList = JsonSerializer.Deserialize<PackageList>(json);

        if (packageList == null)
        {
            Console.WriteLine("No packages found.");
            return;
        }

        foreach (var package in packageList.Packages)
        {
            Infochecker(package.Name);
            Console.WriteLine("\n");
        }
    }

    static void Infochecker(string packageName)
    {
        string json = File.ReadAllText(PackageListPath ?? throw new InvalidOperationException());
        // Deserialize the JSON content into C# objects
        var packageList = JsonSerializer.Deserialize<PackageList>(json);

        var package =
            packageList?.Packages.FirstOrDefault(
                p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

        string addToPath = package!.RequirePath ? "Yes" : "No";
        string isCli = package.Shortcut ? "Yes" : "No";

        Console.WriteLine($"Name: {package.Name}");
        Console.WriteLine($"Version: {package.Version}");
        Console.WriteLine($"URL: {package.Url}");
        Console.WriteLine($"Description: {package.Description}");
        Console.WriteLine($"Will be added to path: {addToPath}");
        Console.WriteLine($"Is a CLI app: {isCli}");
        Console.Write("Available for: ");
        foreach (string os in package.OsList)
        {
            if (os.Equals(package.OsList.Last()))
                Console.WriteLine($"{os}");
            else
                Console.Write($"{os}, ");
        }
    }
}