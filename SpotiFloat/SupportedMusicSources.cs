using System;

namespace SpotiFloat;

internal static class SupportedMusicSources
{
    internal static string[] GetProcessNames(string? appId)
    {
        if (!IsAllowed(appId)) return [];
        if (appId!.StartsWith("Spotify", StringComparison.OrdinalIgnoreCase)) return ["Spotify"];
        if (appId.StartsWith("Apple", StringComparison.OrdinalIgnoreCase)) return ["AppleMusic"];
        if (appId.StartsWith("Deezer", StringComparison.OrdinalIgnoreCase)) return ["Deezer"];
        if (appId.StartsWith("TIDAL", StringComparison.OrdinalIgnoreCase)) return ["TIDAL"];
        return ["Amazon Music", "AmazonMusic"];
    }

    internal static bool IsAllowed(string? appId)
    {
        if (string.IsNullOrEmpty(appId)) return false;

        // Match app identities, never track titles or browser names.
        return appId.Equals("Spotify", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("Spotify.exe", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("SpotifyAB.SpotifyMusic_", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("AppleMusic", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("AppleMusic.exe", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("AppleInc.AppleMusic_", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("AmazonMusic", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("AmazonMusic.exe", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("Amazon Music", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("Amazon Music.exe", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("AmazonMobileLLC.AmazonMusic_", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("AMZNMobileLLC.AmazonMusic_", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("Deezer", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("Deezer.exe", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("Deezer.62021768415AF_", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("TIDAL", StringComparison.OrdinalIgnoreCase)
            || appId.Equals("TIDAL.exe", StringComparison.OrdinalIgnoreCase)
            || appId.StartsWith("TIDALMusicAS.TIDAL_", StringComparison.OrdinalIgnoreCase);
    }
}
