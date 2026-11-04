using System.Globalization;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 单个坐标轴的刻度信息：取整后的范围、步长、刻度值与文本标签。
/// </summary>
internal sealed class AxisTicks
{
    private static readonly double[] NiceMantissas = [1.0, 2.0, 2.5, 5.0, 10.0];

    /// <summary>轴范围最小值（取整后）。</summary>
    public double Min { get; private init; }

    /// <summary>轴范围最大值（取整后）。</summary>
    public double Max { get; private init; }

    /// <summary>刻度步长。</summary>
    public double Step { get; private init; }

    /// <summary>标签需要的小数位数。</summary>
    public int Decimals { get; private init; }

    /// <summary>刻度数值。</summary>
    public double[] Values { get; private init; } = [];

    /// <summary>与 <see cref="Values"/> 对应的文本标签。</summary>
    public string[] Labels { get; private init; } = [];

    /// <summary>范围长度。</summary>
    public double Length => Max - Min;

    /// <summary>
    /// 计算刻度。
    /// </summary>
    /// <param name="dataMin">数据最小值。</param>
    /// <param name="dataMax">数据最大值。</param>
    /// <param name="targetCount">期望的刻度数量（含两端）。</param>
    /// <param name="fixedMin">最小值是否由用户指定（指定后不做取整扩展）。</param>
    /// <param name="fixedMax">最大值是否由用户指定。</param>
    public static AxisTicks Create(double dataMin, double dataMax, int targetCount, bool fixedMin, bool fixedMax)
    {
        // 1. 规范化输入范围。
        if (!double.IsFinite(dataMin) || !double.IsFinite(dataMax))
        {
            dataMin = 0;
            dataMax = 1;
            fixedMin = false;
            fixedMax = false;
        }

        if (dataMax < dataMin)
        {
            (dataMin, dataMax) = (dataMax, dataMin);
        }

        if (dataMax - dataMin <= 0)
        {
            // 所有点重合：围绕该值扩展出一个可见范围。
            var padding = Math.Max(1.0, Math.Abs(dataMax)) * 0.5;
            dataMin -= padding;
            dataMax += padding;
            fixedMin = false;
            fixedMax = false;
        }

        var count = Math.Clamp(targetCount, 2, 20);
        var rawStep = (dataMax - dataMin) / (count - 1);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        var normalized = rawStep / magnitude;

        var mantissa = NiceMantissas[^1];
        foreach (var candidate in NiceMantissas)
        {
            if (normalized <= candidate * (1 + 1e-9))
            {
                mantissa = candidate;
                break;
            }
        }

        var step = mantissa * magnitude;
        if (!double.IsFinite(step) || step <= 0)
        {
            step = 1;
        }

        // 2. 取整范围。
        var niceMin = fixedMin ? dataMin : Math.Floor(Math.Round(dataMin / step, 9)) * step;
        var niceMax = fixedMax ? dataMax : Math.Ceiling(Math.Round(dataMax / step, 9)) * step;

        if (niceMax - niceMin <= 0)
        {
            niceMax = niceMin + step;
        }

        // 3. 计算标签精度。
        var exponent = (int)Math.Floor(Math.Log10(step));
        var stepMantissa = step / Math.Pow(10, exponent);
        var extraDigits = Math.Abs(stepMantissa - 2.5) < 1e-6 ? 1 : 0;
        var decimals = Math.Clamp(-exponent + extraDigits, 0, 8);

        // 4. 生成刻度值（使用整数索引避免浮点累积误差）。
        var slots = (int)Math.Floor((niceMax - niceMin) / step + 1e-9);
        if (slots < 1)
        {
            slots = 1;
        }

        var values = new double[slots + 1];
        var labels = new string[slots + 1];
        for (var i = 0; i <= slots; i++)
        {
            var value = niceMin + i * step;
            value = Math.Round(value, decimals);
            if (Math.Abs(value) < step * 1e-9)
            {
                value = 0; // 消除 -0 与浮点粉尘。
            }

            values[i] = value;
            labels[i] = Format(value, decimals);
        }

        return new AxisTicks
        {
            Min = niceMin,
            Max = Math.Max(niceMax, niceMin + slots * step),
            Step = step,
            Decimals = decimals,
            Values = values,
            Labels = labels,
        };
    }

    private static string Format(double value, int decimals)
    {
        var abs = Math.Abs(value);
        if (abs >= 1e6 || (abs > 0 && abs < 1e-4))
        {
            return value.ToString("0.###E+0", CultureInfo.InvariantCulture);
        }

        return value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
