using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace SpotiFloat;

internal static class AlbumPalette
{
    internal static Color Extract(BitmapSource source)
    {
        var scaled = new TransformedBitmap(source,
            new ScaleTransform(Math.Min(1, 64.0 / source.PixelWidth), Math.Min(1, 64.0 / source.PixelHeight)));
        var bitmap = new FormatConvertedBitmap(scaled, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        var counts = new int[4096];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] < 128) continue;
            int key = (pixels[i + 2] >> 4) << 8 | (pixels[i + 1] >> 4) << 4 | pixels[i] >> 4;
            counts[key]++;
        }
        int winner = -1;
        double best = -1;
        for (int key = 0; key < counts.Length; key++)
        {
            if (counts[key] == 0) continue;
            int red = (key >> 8) * 16, green = ((key >> 4) & 15) * 16, blue = (key & 15) * 16;
            int spread = Math.Max(red, Math.Max(green, blue)) - Math.Min(red, Math.Min(green, blue));
            double score = counts[key] * (1 + spread / 255.0);
            if (score > best) { winner = key; best = score; }
        }
        // Return an actual sampled colour from the winning bucket, without saturation boosts.
        for (int i = 0; i < pixels.Length; i += 4)
        {
            int key = (pixels[i + 2] >> 4) << 8 | (pixels[i + 1] >> 4) << 4 | pixels[i] >> 4;
            if (pixels[i + 3] >= 128 && key == winner)
                return Color.FromRgb(pixels[i + 2], pixels[i + 1], pixels[i]);
        }
        return Colors.Gray;
    }

    private static readonly string[] OverrideKeys =
    [
        "AppBackground", "AppBorder", "PlayerOverlayColour", "ButtonBackground",
        "ButtonIconForeground", "SliderThumb", "PlayerVisualizerForeground"
    ];

    internal static void Apply(FrameworkElement player, int theme, AlbumColourMode mode, Color? seed)
    {
        foreach (var key in OverrideKeys) player.Resources.Remove(key);
        if (seed == null || mode < AlbumColourMode.Accents) return;
        void Brush(string key, Color colour) => player.Resources[key] = new SolidColorBrush(colour);
        if (mode == AlbumColourMode.Full)
        {
            var baseColour = theme == 1 ? Color.FromRgb(242, 242, 242) : Color.FromRgb(24, 24, 24);
            var surface = Mix(baseColour, seed.Value, 0.3);
            Brush("AppBackground", theme == 2 ? Color.FromArgb(64, seed.Value.R, seed.Value.G, seed.Value.B) : surface);
            Brush("AppBorder", Color.FromArgb(60, seed.Value.R, seed.Value.G, seed.Value.B));
            player.Resources["PlayerOverlayColour"] = Color.FromArgb(theme == 2 ? (byte)204 : (byte)230, surface.R, surface.G, surface.B);
            if (player is Controls.BarPlayerControl)
            {
                // An opaque local surface keeps Glass buttons independent of the wallpaper beneath them.
                var button = theme == 2
                    ? ButtonSurface(surface, Mix(Colors.Black, seed.Value, 64 / 255.0), Mix(Colors.White, seed.Value, 64 / 255.0))
                    : ButtonSurface(surface, surface);
                Brush("ButtonBackground", button);
            }
        }
        var background = BackgroundFor(player, theme);
        var accent = TextContrast.AccentOn(seed.Value, background);
        var iconBackground = mode == AlbumColourMode.Full && player is Controls.BarPlayerControl
            ? ((SolidColorBrush)player.Resources["ButtonBackground"]).Color : background;
        Brush("ButtonIconForeground", TextContrast.AccentOn(seed.Value, iconBackground));
        Brush("SliderThumb", accent);
        // Idle visualizers use the theme's contrast backing rather than the hover surface.
        Brush("PlayerVisualizerForeground", TextContrast.AccentOn(seed.Value, theme == 1 ? Colors.White : Colors.Black));
    }

    private static Color ButtonSurface(Color basis, params Color[] backgrounds)
    {
        var preferred = TextContrast.ReadableOn(basis);
        Color best = basis;
        double bestContrast = 0;
        foreach (var target in new[] { preferred, preferred == Colors.White ? Colors.Black : Colors.White })
        for (int percent = 0; percent <= 100; percent++)
        {
            var candidate = Mix(basis, target, percent / 100.0);
            double contrast = backgrounds.Min(background => TextContrast.Ratio(candidate, background));
            if (contrast > bestContrast) { best = candidate; bestContrast = contrast; }
            // 2:1 is a visual-separation target for these decorative circles, not a WCAG boundary requirement.
            if (contrast >= 2) return candidate;
        }
        return best;
    }

    internal static Color BackgroundFor(FrameworkElement player, int theme)
    {
        if (player.FindName("HoverGroup") != null && player.TryFindResource("PlayerOverlayColour") is Color overlay)
        {
            byte behind = TextContrast.ReadableOn(overlay) == Colors.White ? (byte)255 : (byte)0;
            return Mix(Color.FromRgb(behind, behind, behind), Color.FromRgb(overlay.R, overlay.G, overlay.B), overlay.A / 255.0);
        }
        if (theme == 2) return Colors.Black;
        return player.TryFindResource("AppBackground") is SolidColorBrush surface ? surface.Color : Colors.Black;
    }

    private static Color Mix(Color first, Color second, double amount) => Color.FromRgb(
        (byte)Math.Round(first.R + (second.R - first.R) * amount),
        (byte)Math.Round(first.G + (second.G - first.G) * amount),
        (byte)Math.Round(first.B + (second.B - first.B) * amount));
}
