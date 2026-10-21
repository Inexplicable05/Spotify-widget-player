using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Diagnostics;

namespace SpotiFloat
{
    public enum PlayerLayout { Bar = 0, Square = 1, Disk = 2 }
    public enum AlbumColourMode { Off = 0, Title = 1, Accents = 2, Full = 3 }

    public class AppSettings
    {
        public bool IsLocked { get; set; } = false;
        public WidgetZOrder ZOrder { get; set; } = WidgetZOrder.Topmost;
        public bool HideOnFullscreen { get; set; } = false;
        public double Scale { get; set; } = 1.0;
        public int ThemeIndex { get; set; } = 0; // 0: Dark, 1: Light, 2: Glass
        public bool RunOnStartup { get; set; } = false;
        public AlbumColourMode AlbumColouring { get; set; } = AlbumColourMode.Off;
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
                    return DeserializeSettings(json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadSettings error: {ex}");
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        internal static AppSettings DeserializeSettings(string json)
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return settings;
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.AlbumColouring), out _))
            {
                settings.AlbumColouring = settings.ThemeIndex == 3 ? AlbumColourMode.Full
                    : document.RootElement.TryGetProperty("EnableDynamicTitleColour", out var legacyTitle)
                        && legacyTitle.ValueKind == JsonValueKind.True ? AlbumColourMode.Title : AlbumColourMode.Off;
            }
            if (!Enum.IsDefined(settings.AlbumColouring)) settings.AlbumColouring = AlbumColourMode.Off;
            settings.ThemeIndex = Math.Clamp(settings.ThemeIndex, 0, 2);
            // The former Dynamic theme used Dark as its base.
            if (document.RootElement.TryGetProperty(nameof(AppSettings.ThemeIndex), out var oldTheme)
                && oldTheme.TryGetInt32(out int theme) && theme == 3) settings.ThemeIndex = 0;
            return settings;
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

        public static bool ClearSavedData()
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
                return true;
            }
            catch (Exception ex)
{
    Debug.WriteLine($"ClearSavedData error: {ex}");
    return false;
}
        }
    }
}
