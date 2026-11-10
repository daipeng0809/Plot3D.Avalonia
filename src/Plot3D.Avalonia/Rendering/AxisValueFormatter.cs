using System.Globalization;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 把坐标轴数值格式化为标签文本，支持普通数值与 OLE Automation 日期（单位：天）。
/// </summary>
internal static class AxisValueFormatter
{
    /// <summary>OADate 的有效范围（与 <see cref="DateTime.FromOADate"/> 一致）。</summary>
    private const double MinOADate = -657435d;
    private const double MaxOADate = 2958465.99999999d;

    /// <summary>判断数值是否是可转换为 <see cref="DateTime"/> 的有效 OADate。</summary>
    public static bool IsValidOADate(double value) =>
        double.IsFinite(value) && value >= MinOADate && value <= MaxOADate;

    /// <summary>把数值格式化为文本。</summary>
    /// <param name="value">数值（时间格式时为 OADate）。</param>
    /// <param name="format">格式类型。</param>
    /// <param name="milliseconds">时间格式是否输出毫秒。</param>
    public static string Format(double value, AxisValueFormat format, bool milliseconds)
    {
        if (format == AxisValueFormat.Numeric || !IsValidOADate(value))
        {
            return FormatNumber(value);
        }

        var timestamp = DateTime.FromOADate(value);
        return format switch
        {
            AxisValueFormat.Date => timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AxisValueFormat.Time => timestamp.ToString(milliseconds ? "HH:mm:ss.fff" : "HH:mm:ss", CultureInfo.InvariantCulture),
            _ => timestamp.ToString(milliseconds ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        };
    }

    /// <summary>普通数值格式（极大 / 极小值切换为科学计数法）。</summary>
    private static string FormatNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        var abs = Math.Abs(value);
        if (abs >= 1e6 || (abs > 0 && abs < 1e-4))
        {
            return value.ToString("0.###E+0", CultureInfo.InvariantCulture);
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
