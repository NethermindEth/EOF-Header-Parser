
using System.Net;

namespace TypeExtensions;
public static class FileExtensions
{
    public static string[] FetchFile(string filePath) {
        bool? isUri = null;
        if(Uri.IsWellFormedUriString(filePath, UriKind.Absolute)) {
            isUri = true;
        }
        if(File.Exists(filePath)) {
            isUri = false;
        }
        if(isUri == null) {
            Console.WriteLine($"Invalid input. Input must be a valid URI or a valid file path {filePath}.");
            return Array.Empty<string>();
        }

        if(isUri.Value) {
            var client = new WebClient();
            return client.DownloadString(filePath).Split('\n');
        } else {
            return File.ReadAllLines(filePath);
        }
    }

}