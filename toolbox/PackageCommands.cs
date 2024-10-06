using System.Runtime.InteropServices;
using System.Text.Json;

namespace toolbox;

public static class PackageCommands
{
    private static string? _exeDir;
    private static string? _exePath;
    private static string? _appdataDir;
    private static string? _packageListPath;

    private static string? _name;
    private static string? _version;
    private static string? _url;
    private static string? _description;
    private static string? _sha256;
    private static bool _requirepath;
    private static bool _shortcut;
    private static List<string>? _osList;

    private static string _operatingSystem = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown";

    public static void Launcher(string[] args)
    {
        switch (_operatingSystem)
        {
            case "Windows":
                _exeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ravensoftware",
                    args[1].ToLower());
                _appdataDir = _exeDir;
                _packageListPath = Path.Combine(_appdataDir, "toolbox", "packages.json");
                break;
            case "macOS":
                //_exeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications");
                _exeDir = "/Applications";
                _appdataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "ravensoftware");
                _packageListPath = Path.Combine(_appdataDir, "toolbox", "packages.json");
                break;
        }

        string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());
        // Deserialize the JSON content into C# objects
        var packageList = JsonSerializer.Deserialize<PackageList>(json);

        var package =
            packageList?.Packages.FirstOrDefault(p => p.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase));

        if (package == null)
        {
            Console.WriteLine("Package not found");
            return;
        }

        _name = package?.Name;
        _version = package?.Version;
        _url = package?.Url;
        _description = package?.Description;
        _sha256 = package?.Sha256;
        _requirepath = package.RequirePath;
        _shortcut = package.Shortcut;
        _osList = package?.OsList;

        _exePath = Path.Combine(_exeDir, $"{_name.ToLower()}.zip");

        if (!_osList.Contains(_operatingSystem))
        {
            Console.WriteLine("This package isn't available for your OS");
            return;
        }

        switch (args[0])
        {
            case "install":
                Install(args[1]);
                break;
        }
    }

    public static void Install(string package)
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

        if (!Directory.Exists(_exeDir))
            Directory.CreateDirectory(_exeDir);

        Console.WriteLine($"Installing {_name}...");
        FileTools.DownloadFile(_url, _exePath);

        if (FileTools.GetChecksum(_exePath) != _sha256)
        {
            Console.WriteLine("Checksum mismatch");
            File.Delete(_exePath);
            return;
        }
        
        //todo: unzip

        if (_shortcut)
            switch (_operatingSystem)
            {
                case "Windows":
                    Console.WriteLine("Creating Shortcuts...");
                    Shortcut.Windows(_exePath, _name, _description);
                    break;
            }
    }

    public static void Infochecker(string package)
    {
        string addToPath = _requirepath ? "Yes" : "No";
        string isCli = _requirepath ? "Yes" : "No";

        Console.WriteLine($"Name: {_name}");
        Console.WriteLine($"Version: {_version}");
        Console.WriteLine($"URL: {_url}");
        Console.WriteLine($"Description: {_description}");
        Console.WriteLine($"Will be added to path: {addToPath}");
        Console.WriteLine($"Is a CLI app: {isCli}");
        Console.Write("Available for: ");
        foreach (string os in _osList)
        {
            if (os.Equals(_osList.Last()))
                Console.WriteLine($"{os}");
            else
                Console.Write($"{os}, ");
        }
    }
}