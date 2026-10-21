using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SpotiFloat;

internal static class InstallerSupport
{
    internal static string? FindUninstaller()
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                // Inno Setup uses AppId (default: AppName) followed by _is1.
                using var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SpotiFloat_is1");
                if (key?.GetValue("InstallLocation") is string location
                    && key.GetValue("UninstallString") is string command
                    && ResolveUninstaller(AppContext.BaseDirectory, location, command) is { } path) return path;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                Debug.WriteLine($"Uninstaller lookup failed: {ex}");
            }
        }
        return null;
    }

    internal static string? ResolveUninstaller(string appDirectory, string installDirectory, string command)
    {
        try
        {
            string executable = command.Trim();
            if (executable.StartsWith('"'))
            {
                int closingQuote = executable.IndexOf('"', 1);
                if (closingQuote < 0) return null;
                executable = executable[1..closingQuote];
            }
            string installed = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installDirectory));
            string current = Path.TrimEndingDirectorySeparator(Path.GetFullPath(appDirectory));
            if (!string.Equals(installed, current, StringComparison.OrdinalIgnoreCase)
                || !Path.IsPathFullyQualified(executable)) return null;
            string path = Path.GetFullPath(executable);
            if (!string.Equals(Path.GetDirectoryName(path), installed, StringComparison.OrdinalIgnoreCase)
                || !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path), @"^unins\d+\.exe$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                || !File.Exists(path)) return null;
            return path;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Invalid uninstaller registration: {ex}");
            return null;
        }
    }
}
