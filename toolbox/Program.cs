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
        private static string? appdata;

        static void Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _packageListPath = string.Concat(appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                                 Path.DirectorySeparatorChar + "toolbox" + Path.DirectorySeparatorChar +
                                                 "packages.json");
            }

            else
            {
                throw new InvalidOperationException("Wrong OS");
            }

            UpdateCheck();

            if (args[0] == "install")
                Install(args[1]);

            else if (args[0] == "update")
                Update();

            else if (args[0] == "remove")
                Remove(args[1]);

            else if (args[0] == "upgrade")
                Upgrade(args[1]);

            else if (args[0] == "sha256")
                Console.WriteLine(GetChecksum(args[1]));

            else
            {
                Console.WriteLine("No arguments were passed.");
            }
        }

        static void Install(string appName)
        {
            string executableDirectory = string.Concat(appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + appName);

            string executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";


            // Read the file content
            string json = File.ReadAllText(_packageListPath);

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

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

            if (!Directory.Exists(executableDirectory))
                Directory.CreateDirectory(executableDirectory);

            Console.WriteLine($"Installing {appName}...");
            DownloadFile(package.Url, executablePath);

            // Check the checksum
            if (GetChecksum(executablePath) != package.Sha256)
            {
                Console.WriteLine("Checksums do not match. Exiting...");
                File.Delete(executablePath);
                return;
            }

            // Create a shortcut
            Console.WriteLine("Creating shortcut...");
            ShortcutMaker(executablePath, package.Name, package.Description);

            Console.WriteLine($"{appName} has been installed to {executableDirectory}");
        }

        static void Remove(string appName)
        {
            string executableDirectory = string.Concat(appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + appName);

            string executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";

            string shortcutPath = string.Concat(appdata + Path.DirectorySeparatorChar + "Microsoft" +
                                                Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                                "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                                Path.DirectorySeparatorChar + appName + ".lnk");

            string json = File.ReadAllText(_packageListPath);

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                Console.WriteLine($"Package {appName} not found in the package list.");
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

            // Remove the shortcut
            Console.WriteLine("Removing shortcut...");
            File.Delete(shortcutPath);

            Console.WriteLine($"{appName} has been removed.");
        }

        static void Upgrade(string appName)
        {
            string executableDirectory = string.Concat(appdata + Path.DirectorySeparatorChar +
                                                       "ravensoftware" +
                                                       Path.DirectorySeparatorChar + appName);

            string executablePath = executableDirectory + Path.DirectorySeparatorChar + appName + ".exe";

            // Read the file content
            string json = File.ReadAllText(_packageListPath);

            // Deserialize the JSON content into C# objects
            var packageList = JsonSerializer.Deserialize<PackageList>(json);

            var package =
                packageList?.Packages.FirstOrDefault(p => p.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

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

            if (!Directory.Exists(executableDirectory))
                Directory.CreateDirectory(executableDirectory);

            Console.WriteLine($"Upgrading {appName}...");
            DownloadFile(package.Url, executablePath);

            // Check the checksum
            if (GetChecksum(executablePath) != package.Sha256)
            {
                Console.WriteLine("Checksums do not match. Exiting...");
                File.Delete(executablePath);
                return;
            }

            Console.WriteLine($"{appName} has been upgraded.");
        }


        static void Update()
        {
            string updateUrl =
                "https://raw.githubusercontent.com/ravendevteam/toolbox/refs/heads/main/toolbox/packages.json";

            string toolboxDir = string.Concat(appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                               Path.DirectorySeparatorChar + "toolbox");
            string packagePath = toolboxDir + Path.DirectorySeparatorChar + "packages.json";
            string lastUpdatePath = String.Concat(toolboxDir + Path.DirectorySeparatorChar + "lastupdate");


            if (!Directory.Exists(toolboxDir))
                Directory.CreateDirectory(toolboxDir);

            if (File.Exists(packagePath))
            {
                // Read the file content
                string json = File.ReadAllText(_packageListPath);

                // Deserialize the JSON content into C# objects
                var packageList = JsonSerializer.Deserialize<PackageList>(json);
                
                string pattern = @"^(https?|ftp)://[\w.-]+(\.[\w.-]+)+[\w\-.,@?^=%&:/~+#]*$";
                Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                
                if(regex.IsMatch(packageList.UpdateUrl))
                    updateUrl = packageList.UpdateUrl;
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
        }

        static void UpdateCheck()
        {
            long timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string lastUpdatePath = String.Concat(appdata + Path.DirectorySeparatorChar + "ravensoftware" +
                                                  Path.DirectorySeparatorChar + "toolbox" +
                                                  Path.DirectorySeparatorChar + "lastupdate");


            if (!File.Exists(lastUpdatePath))
            {
                Console.WriteLine("Automatically updating Raven Toolbox (file doesn't exist)");
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
                    Console.WriteLine("Automatically updating Raven Toolbox (time is more than 86400)");
                    Update();
                }
            }
        }

        static void DownloadFile(string url, string fileName)
        {
#pragma warning disable SYSLIB0014
            using var client = new WebClient();

            // Subscribe to the DownloadProgressChanged event
            client.DownloadProgressChanged += (sender, e) =>
            {
                // Update the progress bar
                Console.Write(
                    $"\rDownloading: [{new string('#', e.ProgressPercentage / 2)}{new string(' ', 50 - e.ProgressPercentage / 2)}] {e.ProgressPercentage}%");
            };

            // Subscribe to the DownloadFileCompleted event
            client.DownloadFileCompleted += (sender, e) => { Console.WriteLine("\nDownload completed!"); };

            // Start the download asynchronously
            client.DownloadFileAsync(new Uri(url), fileName);
#pragma warning restore SYSLIB0014

            // Keep the application running until the download is complete
            while (client.IsBusy)
            {
                Thread.Sleep(100);
            }
        }

        private static string GetChecksum(string file)
        {
            if (!File.Exists(file))
                return "File does not exist";

            using (FileStream stream = File.OpenRead(file))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty).ToLower();
            }
        }

        static void ShortcutMaker(string path, string name, string description)
        {
            IShellLink link = (IShellLink)new ShellLink();
            string shortCutDir = string.Concat(appdata + Path.DirectorySeparatorChar + "Microsoft" +
                                               Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar +
                                               "Start Menu" + Path.DirectorySeparatorChar + "Programs" +
                                               Path.DirectorySeparatorChar + name + ".lnk");

            // setup shortcut information
            link.SetDescription(description);
            link.SetPath(path);

            // save it
            IPersistFile file = (IPersistFile)link;
            file.Save(shortCutDir, false);
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