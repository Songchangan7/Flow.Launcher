using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.BrowserTab.Commands;

internal static class ChromeSessionUrlLoader
{
    private static readonly Regex UrlRegex = new(@"https?://[^\x00-\x20""'<>]{5,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static List<string> LoadRecentUrls()
    {
        try
        {
            var sessionDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "User Data",
                "Default",
                "Sessions");

            if (!Directory.Exists(sessionDir))
            {
                return [];
            }

            var tabFiles = new DirectoryInfo(sessionDir)
                .GetFiles("Tabs_*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            foreach (var tabFile in tabFiles)
            {
                var urls = TryReadUrls(tabFile.FullName);
                if (urls.Count > 0)
                {
                    return urls;
                }
            }
        }
        catch (Exception e)
        {
            Main.Context.API.LogException(nameof(ChromeSessionUrlLoader), "Failed to load Chrome session URLs.", e);
        }

        return [];
    }

    private static List<string> TryReadUrls(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            var text = Encoding.ASCII.GetString(bytes);

            return UrlRegex.Matches(text)
                .Select(match => match.Value)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
