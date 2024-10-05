using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace toolbox
{
    abstract class Program
    {
        private static string? _packageListPath;
        private static string? _appdata;
        private const string Version = "1.0.2";

        static void Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _packageListPath = string.Concat(_appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                                 Path.DirectorySeparatorChar + "toolbox" + Path.DirectorySeparatorChar +
                                                 "packages.json");
            }

            else
            {
                throw new InvalidOperationException("Wrong OS");
            }

            UpdateCheck();

            try
            {
                switch (args[0])
                {
                    case "install":
                        Install(args[1]);
                        break;

                    case "update":
                        Update();
                        break;

                    case "remove":
                        Remove(args[1]);
                        break;

                    case "upgrade":
                        Upgrade(args[1]);
                        break;

                    case "list":
                        List();
                        break;

                    case "sha256":
                        Console.WriteLine($"Checksum of {args[1]}: \n{GetChecksum(args[1])}");
                        break;

                    default:
                        Console.WriteLine("Invalid command.");
                        Console.WriteLine("Usage: toolbox [install|update|remove|upgrade|list|sha256] <appname>");
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Usage: toolbox [install|update|remove|upgrade|list|sha256] <appname>");
            }
        }

        static void Install(string appName)
        {
            string executableDirectory = string.Concat(_appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + appName.ToLower());

            string executablePath = executableDirectory + Path.DirectorySeparatorChar + appName.ToLower() + ".exe";

            // Read the file content
            string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                Console.WriteLine($"Package {appName} not found in the package list.");
                return;
            }

            // Check if the package is available for the current OS
            if (!package.OsList.Contains(Environment.OSVersion.Platform.ToString()))
            {
                Console.WriteLine($"Package {package.Name} is not available for your OS.");
                return;
            }

            InfoChecker(package.Name);
            Console.WriteLine("Okay to install? Y/n");

            string? response = Console.ReadLine();
            response = response?.ToLower();

            if (response != "y" && response != "yes" && response != "")
            {
                Console.WriteLine("Cancelling...");
                return;
            }

            if (!Directory.Exists(executableDirectory))
                Directory.CreateDirectory(executableDirectory);

            Console.WriteLine($"Installing {package.Name}...");
            DownloadFile(package.Url, executablePath);

            // Check the checksum
            if (GetChecksum(executablePath) != package.Sha256)
            {
                Console.WriteLine("Checksums do not match. Exiting...");
                File.Delete(executablePath);
                return;
            }

            // Add to PATH
            if (package.RequirePath)
            {
                string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                // Check if the new directory is already in the PATH
                if (currentPath != null && !currentPath.Contains(executableDirectory))
                {
                    // Append the new directory to the PATH
                    string updatedPath = currentPath + ";" + executableDirectory;

                    // Set the updated PATH environment variable
                    Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);

                    Console.WriteLine($"Added {executableDirectory} to the user PATH.");
                }
            }

            // Create a shortcut
            Console.WriteLine("Creating shortcut...");
            if (package.Shortcut)
                ShortcutMaker(executablePath, package.Name, package.Description);

            Console.WriteLine($"{package.Name} has been installed to {executableDirectory}");
        }

        static void Remove(string appName)
        {
            string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            string executableDirectory = string.Concat(_appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + package.Name.ToLower());

            string startMenuFile = string.Concat(_appdata + Path.DirectorySeparatorChar + "Microsoft" +
                                                 Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                                 "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                                 Path.DirectorySeparatorChar + package.Name + ".lnk");

            string desktopFile = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)) +
                                 Path.DirectorySeparatorChar + package.Name + ".lnk";

            if (package == null)
            {
                Console.WriteLine($"Package {package.Name} not found in the package list.");
                return;
            }

            if (appName == "toolbox-cli")
            {
                Console.WriteLine("Cannot remove Toolbox, please delete the toolbox folder manually.");
                return;
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

            // Remove from PATH
            if (package.RequirePath)
            {
                // Get the current user PATH environment variable
                string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                // Check if the directory exists in the PATH
                if (currentPath != null && currentPath.Contains(executableDirectory))
                {
                    // Remove the directory from the PATH
                    string updatedPath = currentPath.Replace(executableDirectory, "").Replace(";;", ";");

                    // Remove any trailing or leading semicolons
                    updatedPath = updatedPath.Trim(';');

                    // Set the updated PATH environment variable
                    Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);

                    Console.WriteLine($"Removed {executableDirectory} from the user PATH.");
                }
            }

            // Remove the shortcut
            if (File.Exists(startMenuFile))
            {
                Console.WriteLine("Removing shortcut...");
                File.Delete(startMenuFile);

                if (File.Exists(desktopFile))
                    File.Delete(desktopFile);
            }

            Console.WriteLine($"{appName} has been removed.");
        }

        static void Upgrade(string appName)
        {
            string executableDirectory = string.Concat(_appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + appName.ToLower());

            string executablePath = executableDirectory + Path.DirectorySeparatorChar + appName.ToLower() + ".exe";

            // Read the file content
            string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                Console.WriteLine($"Package {appName} not found in the package list.");
                return;
            }

            InfoChecker(package.Name);
            Console.WriteLine("Okay to upgrade? Y/n");

            string? response = Console.ReadLine();
            response = response?.ToLower();

            if (response != "y" && response != "yes" && response != "")
            {
                Console.WriteLine("Cancelling...");
                return;
            }

            if (!Directory.Exists(executableDirectory))
                Directory.CreateDirectory(executableDirectory);

            Console.WriteLine($"Upgrading {package.Name}...");
            DownloadFile(package.Url, executablePath);

            // Check the checksum
            if (GetChecksum(executablePath) != package.Sha256)
            {
                Console.WriteLine("Checksums do not match. Exiting...");
                File.Delete(executablePath);
                return;
            }

            // Add to PATH
            if (package.RequirePath)
            {
                string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                // Check if the new directory is already in the PATH
                if (currentPath != null && !currentPath.Contains(executableDirectory))
                {
                    // Append the new directory to the PATH
                    string updatedPath = currentPath + ";" + executableDirectory;

                    // Set the updated PATH environment variable
                    Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);

                    Console.WriteLine($"Added {executableDirectory} to the user PATH.");
                }
            }

            Console.WriteLine($"{package.Name} has been upgraded.");
        }


        static void Update()
        {
            string updateUrl =
                "https://raw.githubusercontent.com/ravendevteam/toolbox/refs/heads/main/toolbox/packages.json";

            string toolboxDir = string.Concat(_appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                              Path.DirectorySeparatorChar + "toolbox");
            string packagePath = toolboxDir + Path.DirectorySeparatorChar + "packages.json";
            string lastUpdatePath = String.Concat(toolboxDir + Path.DirectorySeparatorChar + "lastupdate");

            if (!Directory.Exists(toolboxDir))
                Directory.CreateDirectory(toolboxDir);

            if (File.Exists(packagePath))
            {
                // Deserialize the JSON content into C# objects, if empty use the default URL
                try
                {
                    string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());
                    var packageList = JsonSerializer.Deserialize<PackageList>(json);
                    string pattern = @"^(https?|ftp)://[\w.-]+(\.[\w.-]+)+[\w\-.,@?^=%&:/~+#]*$";
                    Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    if (regex.IsMatch(packageList!.UpdateUrl))
                        updateUrl = packageList.UpdateUrl;
                }
                catch (JsonException)
                {
                    Console.WriteLine("Error reading the package list. Using the default URL.");
                }
            }

            // Download the packages list.
            Console.WriteLine("Updating package list...");
            DownloadFile(updateUrl, packagePath);

            // Update the lastupdate file
            using (StreamWriter outputFile = new StreamWriter(lastUpdatePath))
            {
                outputFile.WriteLine(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            }

            Console.WriteLine("\nPackage list has been updated.");

            // Read the file content
            string jsonToolbox = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageListToolbox = JsonSerializer.Deserialize<PackageList>(jsonToolbox);

            var toolbox =
                packageListToolbox?.Packages.FirstOrDefault(p =>
                    p.Name.Equals("toolbox", StringComparison.OrdinalIgnoreCase));

            // Check if the toolbox needs to be upgraded
            if (toolbox?.Version != Version)
            {
                Console.WriteLine("Toolbox is outdated. Please run 'toolbox upgrade toolbox' as soon as possible.");
            }
        }

        static void List()
        {
            string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            if (packageList == null)
            {
                Console.WriteLine("No packages found.");
                return;
            }

            foreach (var package in packageList.Packages)
            {
                InfoChecker(package.Name);
                Console.WriteLine("\n");
            }
        }

        static void UpdateCheck()
        {
            long timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string lastUpdatePath = String.Concat(_appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                                  Path.DirectorySeparatorChar + "toolbox" +
                                                  Path.DirectorySeparatorChar + "lastupdate");

            if (!File.Exists(lastUpdatePath) || !File.Exists(_packageListPath) ||
                !File.ReadAllText(_packageListPath).Contains("updateurl"))
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
                }
            }
        }

        static void InfoChecker(string appName)
        {
            string json = File.ReadAllText(_packageListPath ?? throw new InvalidOperationException());

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return;
            }

            string addToPath = package.RequirePath ? "Yes" : "No";
            string isCli = !package.Shortcut ? "Yes" : "No";
            List<string> osList = new List<string>();

            foreach (string os in package.OsList)
            {
                switch (os)
                {
                    case "Win32NT":
                        osList.Add("Windows");
                        break;
                    case "Unix":
                        osList.Add("Linux/Unix");
                        break;
                    case "MacOSX":
                        osList.Add("macOS");
                        break;
                    default:
                        osList.Add(os);
                        break;
                }
            }

            Console.WriteLine($"Name: {package.Name}");
            Console.WriteLine($"Version: {package.Version}");
            Console.WriteLine($"URL: {package.Url}");
            Console.WriteLine($"Description: {package.Description}");
            Console.WriteLine($"Will be added to path: {addToPath}");
            Console.WriteLine($"Is a CLI app: {isCli}");
            Console.Write("Available for: ");
            foreach (string os in osList)
            {
                if (os.Equals(osList.Last()))
                    Console.WriteLine($"{os}");
                else
                    Console.Write($"{os}, ");
            }
        }

        static void DownloadFile(string url, string fileName)
        {
            try
            {
#pragma warning disable SYSLIB0014
                using var client = new WebClient();

                // Subscribe to the DownloadProgressChanged event
                client.DownloadProgressChanged += (_, e) =>
                {
                    // Update the progress bar
                    Console.Write(
                        $"\rDownloading: [{new string('#', e.ProgressPercentage / 2)}{new string(' ', 50 - e.ProgressPercentage / 2)}] {e.ProgressPercentage}%");
                };

                // Subscribe to the DownloadFileCompleted event
                client.DownloadFileCompleted += (_, _) => { Console.WriteLine("\nDownload completed!"); };

                // Start the download asynchronously
                client.DownloadFileAsync(new Uri(url), fileName);
#pragma warning restore SYSLIB0014

                // Keep the application running until the download is complete
                while (client.IsBusy)
                {
                    Thread.Sleep(100);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error downloading the file.");
                Console.WriteLine("Please close all apps that are using the file.");
            }
            catch (WebException)
            {
                Console.WriteLine("Error downloading the file.");
                Console.WriteLine("Please check your internet connection.");
            }
        }

        private static string GetChecksum(string file)
        {
            if (!File.Exists(file))
                return "File does not exist";

#pragma warning disable SYSLIB0021
            // ReSharper disable once ConvertToUsingDeclaration
            using (FileStream stream = File.OpenRead(file))
            {
                SHA256Managed sha = new SHA256Managed();
#pragma warning restore SYSLIB0021
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty).ToLower();
            }
        }

        static void ShortcutMaker(string path, string name, string description)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            IShellLink link = (IShellLink)new ShellLink();
            string startMenuFile = string.Concat(_appdata + Path.DirectorySeparatorChar + "Microsoft" +
                                                 Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                                 "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                                 Path.DirectorySeparatorChar + name + ".lnk");

            string desktopFile = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)) +
                                 Path.DirectorySeparatorChar + name + ".lnk";

            // Setup shortcut information
            link.SetDescription(description);
            link.SetPath(path);

            // Save it
            // ReSharper disable once SuspiciousTypeConversion.Global
            IPersistFile file = (IPersistFile)link;
            file.Save(startMenuFile, false);
            File.Copy(startMenuFile, desktopFile, true);
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd,
            int fFlags);

        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
            out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}