using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Diagnostics;

namespace SpotiFloat
{
    public enum PlayerLayout { Bar = 0, Square = 1, Disk = 2 }

    public class AppSettings
    {
        public bool IsLocked { get; set; } = false;
        public WidgetZOrder ZOrder { get; set; } = WidgetZOrder.Topmost;
        public bool HideOnFullscreen { get; set; } = false;
        public double Scale { get; set; } = 1.0;
        public int ThemeIndex { get; set; } = 0; // 0: Dark, 1: Light, 2: Glass, 3: Dynamic
        public bool RunOnStartup { get; set; } = false;
        public bool EnableDynamicTitleColour { get; set; } = true;
        public PlayerLayout Layout { get; set; } = PlayerLayout.Bar;
    }

    public static class SettingsManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotiFloat");
        private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SpotiFloat";

        public static AppSettings LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadSettings error: {ex}");
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);

                UpdateStartupRegistry(settings.RunOnStartup);
            }
            catch (Exception ex)
{
    Debug.WriteLine($"SaveSettings error: {ex}");
}
        }

        private static void UpdateStartupRegistry(bool runOnStartup)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key != null)
                {
                    if (runOnStartup)
                    {
                        // Path to the current running executable
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
{
    Debug.WriteLine($"UpdateStartupRegistry error: {ex}");
}
        }

        public static void WipeSettingsAndUninstall()
        {
            try
            {
                // Remove Registry Key
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    key?.DeleteValue(AppName, false);
                }

                // Delete AppData Folder
                if (Directory.Exists(AppDataFolder))
                {
                    Directory.Delete(AppDataFolder, true);
                }
            }
            catch (Exception ex)
{
    Debug.WriteLine($"WipeSettingsAndUninstall error: {ex}");
}
        }
    }
}
