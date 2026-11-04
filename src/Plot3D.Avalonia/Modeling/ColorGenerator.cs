using Avalonia.Media;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 颜色辅助方法：HSL 转换与观感良好的随机颜色生成。
/// </summary>
internal static class ColorGenerator
{
    /// <summary>
    /// 生成一个在浅色 / 深色背景下都能看清的随机颜色。
    /// </summary>
    public static Color CreateRandom(Random random)
    {
        var hue = random.NextDouble();
        var saturation = 0.55 + random.NextDouble() * 0.30;
        var lightness = 0.42 + random.NextDouble() * 0.18;
        return FromHsl(hue, saturation, lightness);
    }

    /// <summary>HSL（0..1）转 RGB。</summary>
    public static Color FromHsl(double hue, double saturation, double lightness)
    {
        hue -= Math.Floor(hue);
        saturation = Math.Clamp(saturation, 0, 1);
        lightness = Math.Clamp(lightness, 0, 1);

        if (saturation <= 0)
        {
            var gray = (byte)Math.Round(lightness * 255);
            return Color.FromRgb(gray, gray, gray);
        }

        var q = lightness < 0.5
            ? lightness * (1 + saturation)
            : lightness + saturation - lightness * saturation;
        var p = 2 * lightness - q;

        return Color.FromRgb(
            ToByte(HueToRgb(p, q, hue + 1.0 / 3.0)),
            ToByte(HueToRgb(p, q, hue)),
            ToByte(HueToRgb(p, q, hue - 1.0 / 3.0)));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }
        else if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6.0)
        {
            return p + (q - p) * 6 * t;
        }

        if (t < 1.0 / 2.0)
        {
            return q;
        }

        if (t < 2.0 / 3.0)
        {
            return p + (q - p) * (2.0 / 3.0 - t) * 6;
        }

        return p;
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);

    /// <summary>按指定透明度缩放颜色的 alpha 通道。</summary>
    public static Color WithOpacity(Color color, double opacity)
    {
        if (opacity >= 1)
        {
            return color;
        }

        var alpha = (byte)Math.Clamp(Math.Round(color.A * Math.Clamp(opacity, 0, 1)), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
