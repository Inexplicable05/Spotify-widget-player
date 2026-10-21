using System;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace SpotiFloat;

internal static class TextContrast
{
    internal static double Ratio(Color first, Color second)
    {
        double firstLuminance = Luminance(first), secondLuminance = Luminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05)
            / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    internal static Color ReadableOn(Color background) =>
        Ratio(Colors.Black, background) >= Ratio(Colors.White, background) ? Colors.Black : Colors.White;

    internal static Color AccentOn(Color accent, Color background)
    {
        if (Ratio(accent, background) >= 4.5) return accent;
        var target = ReadableOn(background);
        for (int percent = 1; percent <= 100; percent++)
        {
            byte Blend(byte value, byte end) => (byte)Math.Round(value + (end - value) * percent / 100.0);
            var adjusted = Color.FromRgb(Blend(accent.R, target.R), Blend(accent.G, target.G), Blend(accent.B, target.B));
            if (Ratio(adjusted, background) >= 4.5) return adjusted;
        }
        return target;
    }

    private static double Luminance(Color colour)
    {
        double Linear(byte value)
        {
            double channel = value / 255.0;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(colour.R) + 0.7152 * Linear(colour.G) + 0.0722 * Linear(colour.B);
    }
}
