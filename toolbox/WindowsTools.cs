using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace toolbox;

public class WindowsTools
{
    public static void AddShortcut(string path, string name, string description)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        IShellLink link = (IShellLink)new ShellLink();
        string startMenuFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            name + ".lnk");
        string desktopFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".lnk");

        // Setup shortcut information
        link.SetDescription(description);
        link.SetPath(path);
        // Save it
        // ReSharper disable once SuspiciousTypeConversion.Global
        IPersistFile file = (IPersistFile)link;
        file.Save(startMenuFile, false);
        File.Copy(startMenuFile, desktopFile, true);
    }

    public static void RemoveShortcut(string name)
    {
        string startMenuFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            name + ".lnk");
        string desktopFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".lnk");

        if (File.Exists(startMenuFile))
            File.Delete(startMenuFile);

        if (File.Exists(desktopFile))
            File.Delete(desktopFile);
    }

    public static void AddPath(string executableDirectory)
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

    public static void RemovePath(string executableDirectory)
    {
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