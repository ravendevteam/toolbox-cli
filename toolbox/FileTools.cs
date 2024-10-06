using System.Net;
using System.Security.Cryptography;

namespace toolbox;

public static class FileTools
{
    public static void DownloadFile(string url, string fileName)
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
    
    public static string GetChecksum(string file)
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
}