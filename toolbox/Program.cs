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

        if (File.Exists(PackageListPath))
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
                        ExeDir = "/Applications";
                        break;
                }

                ExePath = Path.Combine(ExeDir, Name.ToLower() + "." + Extension);
            }
        }
        else
        {
            //todo: update
        }

        switch (args[0].ToLower())
        {
            case "install":
                Install(args[1]);
                break;
            case "sha256":
                Console.WriteLine(FileTools.GetChecksum(args[1]));
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
                    Shortcuts.Windows(ExePath, Name, Description);
                    break;
            }
    }

    public static void Infochecker(string package)
    {
        string addToPath = RequirePath ? "Yes" : "No";
        string isCli = !Shortcut ? "Yes" : "No";

        Console.WriteLine($"Name: {Name}");
        Console.WriteLine($"Version: {Version}");
        Console.WriteLine($"URL: {Url}");
        Console.WriteLine($"Description: {Description}");
        Console.WriteLine($"Will be added to path: {addToPath}");
        Console.WriteLine($"Is a CLI app: {isCli}");
        Console.Write("Available for: ");
        foreach (string os in OsList)
        {
            if (os.Equals(OsList.Last()))
                Console.WriteLine($"{os}");
            else
                Console.Write($"{os}, ");
        }
    }
}