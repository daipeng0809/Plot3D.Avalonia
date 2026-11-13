using System.Globalization;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 单个坐标轴的刻度信息：取整后的范围、步长、刻度值与文本标签。
/// 支持普通数值刻度与时间友好刻度（日期 / 时间 / 日期 + 时间）。
/// </summary>
internal sealed class AxisTicks
{
    private static readonly double[] NiceMantissas = [1.0, 2.0, 2.5, 5.0, 10.0];

    /// <summary>
    /// 时间轴候选步长（秒），覆盖 1 毫秒到 2 天。
    /// 大于 2 天的步长改由“周 / 月 / 年”日历对齐逻辑处理，避免刻度落在周六或非月初。
    /// </summary>
    private static readonly double[] TimeStepSeconds =
    [
        0.001, 0.002, 0.005,
        0.01, 0.02, 0.05,
        0.1, 0.2, 0.5,
        1, 2, 5, 10, 15, 30,
        60, 120, 300, 600, 900, 1200, 1800,
        3600, 7200, 10800, 21600, 43200,
        86400, 172800,
    ];

    /// <summary>月级候选步长（月）与对应的近似天数，用于挑选日历步长。</summary>
    private static readonly int[] CalendarMonthSteps = [1, 2, 3, 6, 12, 24, 36, 60, 120, 240];

    private static readonly double[] CalendarMonthApproxDays = [30, 61, 92, 183, 366, 731, 1096, 1827, 3653, 7310];

    /// <summary>月 / 年步长的对齐锚点：2000 年 1 月。</summary>
    private const int CalendarAnchorYear = 2000;

    private const double SecondsPerDay = 86400d;

    /// <summary>轴范围最小值（取整后）。</summary>
    public double Min { get; private init; }

    /// <summary>轴范围最大值（取整后）。</summary>
    public double Max { get; private init; }

    /// <summary>刻度步长。</summary>
    public double Step { get; private init; }

    /// <summary>标签需要的小数位数（时间格式下不使用）。</summary>
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
    /// <param name="format">标签格式。</param>
    /// <param name="milliseconds">时间格式是否输出毫秒（步长小于 1 秒时自动输出）。</param>
    /// <param name="labelDecimals">固定小数位数；<c>0</c> 表示只保留整数，<c>-1</c> 表示按步长自动。</param>
    public static AxisTicks Create(
        double dataMin,
        double dataMax,
        int targetCount,
        bool fixedMin,
        bool fixedMax,
        AxisValueFormat format = AxisValueFormat.Numeric,
        bool milliseconds = false,
        int labelDecimals = -1)
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
            // 时间格式按“天”语义使用固定半日 padding，避免按 OADate 量级扩展出上百年的跨度。
            var padding = format == AxisValueFormat.Numeric
                ? Math.Max(1.0, Math.Abs(dataMax)) * 0.5
                : 0.5;
            dataMin -= padding;
            dataMax += padding;
            fixedMin = false;
            fixedMax = false;
        }

        var count = Math.Clamp(targetCount, 2, 20);

        return format == AxisValueFormat.Numeric
            ? CreateNumeric(dataMin, dataMax, count, fixedMin, fixedMax, labelDecimals)
            : CreateTime(dataMin, dataMax, count, fixedMin, fixedMax, format, milliseconds);
    }

    private static AxisTicks CreateNumeric(double dataMin, double dataMax, int count, bool fixedMin, bool fixedMax, int labelDecimals = -1)
    {
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

        // 取整范围。
        var niceMin = fixedMin ? dataMin : Math.Floor(Math.Round(dataMin / step, 9)) * step;
        var niceMax = fixedMax ? dataMax : Math.Ceiling(Math.Round(dataMax / step, 9)) * step;

        if (niceMax - niceMin <= 0)
        {
            niceMax = niceMin + step;
        }

        // 计算标签精度：数值步长决定必要精度，用户指定的固定小数位优先。
        var exponent = (int)Math.Floor(Math.Log10(step));
        var stepMantissa = step / Math.Pow(10, exponent);
        var extraDigits = Math.Abs(stepMantissa - 2.5) < 1e-6 ? 1 : 0;
        var valueDecimals = Math.Clamp(-exponent + extraDigits, 0, 8);
        var decimals = labelDecimals >= 0 ? Math.Clamp(labelDecimals, 0, 8) : valueDecimals;

        // 生成刻度值（使用整数索引避免浮点累积误差）。
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
            value = Math.Round(value, valueDecimals);
            if (Math.Abs(value) < step * 1e-9)
            {
                value = 0; // 消除 -0 与浮点粉尘。
            }

            values[i] = value;
            labels[i] = FormatNumeric(value, decimals);
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

    private static AxisTicks CreateTime(
        double dataMinDays,
        double dataMaxDays,
        int count,
        bool fixedMin,
        bool fixedMax,
        AxisValueFormat format,
        bool milliseconds)
    {
        if (!AxisValueFormatter.IsValidOADate(dataMinDays) || !AxisValueFormatter.IsValidOADate(dataMaxDays))
        {
            // 数值超出 OADate 可表示范围，无法按日期显示，退回数值刻度。
            return CreateNumeric(dataMinDays, dataMaxDays, count, fixedMin, fixedMax);
        }

        var spanSeconds = (dataMaxDays - dataMinDays) * SecondsPerDay;
        var rawStepSeconds = spanSeconds / Math.Max(1, count - 1);

        if (rawStepSeconds > 14 * SecondsPerDay)
        {
            if (!TryGetMonthStep(rawStepSeconds, out var months))
            {
                // 超出 20 年：退回按天取整的数值步长，标签仍按日期格式输出。
                return CreateNumericWithTimeLabels(dataMinDays, dataMaxDays, count, fixedMin, fixedMax, format, milliseconds);
            }

            try
            {
                return CreateMonthTicks(dataMinDays, dataMaxDays, fixedMin, fixedMax, months, format, milliseconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return CreateNumericWithTimeLabels(dataMinDays, dataMaxDays, count, fixedMin, fixedMax, format, milliseconds);
            }
        }

        if (rawStepSeconds > 2 * SecondsPerDay)
        {
            // 3..14 天的跨度使用周对齐（周一为起点），避免 7 天步长落在周六。
            var stepDays = rawStepSeconds > 7 * SecondsPerDay ? 14 : 7;
            try
            {
                return CreateWeekTicks(dataMinDays, dataMaxDays, fixedMin, fixedMax, stepDays, format, milliseconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return CreateNumericWithTimeLabels(dataMinDays, dataMaxDays, count, fixedMin, fixedMax, format, milliseconds);
            }
        }

        var stepSeconds = TimeStepSeconds[^1];
        var found = false;
        foreach (var candidate in TimeStepSeconds)
        {
            if (rawStepSeconds <= candidate * (1 + 1e-9))
            {
                stepSeconds = candidate;
                found = true;
                break;
            }
        }

        if (!found || !double.IsFinite(stepSeconds) || stepSeconds <= 0)
        {
            return CreateNumericWithTimeLabels(dataMinDays, dataMaxDays, count, fixedMin, fixedMax, format, milliseconds);
        }

        var minSeconds = dataMinDays * SecondsPerDay;
        var maxSeconds = dataMaxDays * SecondsPerDay;

        // 对齐到整步长，使时间标签落在 0/15/30/60 秒、整分、整点等“整齐”位置。
        var niceMinSeconds = fixedMin ? minSeconds : Math.Floor(minSeconds / stepSeconds + 1e-9) * stepSeconds;
        var niceMaxSeconds = fixedMax ? maxSeconds : Math.Ceiling(maxSeconds / stepSeconds - 1e-9) * stepSeconds;

        if (niceMaxSeconds - niceMinSeconds <= 0)
        {
            niceMaxSeconds = niceMinSeconds + stepSeconds;
        }

        var slots = (int)Math.Floor((niceMaxSeconds - niceMinSeconds) / stepSeconds + 1e-9);
        if (slots < 1)
        {
            slots = 1;
        }

        // 防御极端时间跨度导致点数爆炸，最多 100 个刻度。
        slots = Math.Min(slots, 100);

        var labelFormat = ResolveLabelFormat(format, stepSeconds);
        var showMilliseconds = milliseconds || stepSeconds < 1;
        var values = new double[slots + 1];
        var labels = new string[slots + 1];

        for (var i = 0; i <= slots; i++)
        {
            var seconds = niceMinSeconds + i * stepSeconds;
            var value = seconds / SecondsPerDay;
            values[i] = value;
            labels[i] = AxisValueFormatter.Format(value, labelFormat, showMilliseconds);
        }

        return new AxisTicks
        {
            Min = niceMinSeconds / SecondsPerDay,
            Max = niceMaxSeconds / SecondsPerDay,
            Step = stepSeconds / SecondsPerDay,
            Decimals = 0,
            Values = values,
            Labels = labels,
        };
    }

    /// <summary>
    /// 按周对齐的刻度：起点对齐到周一 00:00，避免 7 / 14 天步长落在周六。
    /// </summary>
    private static AxisTicks CreateWeekTicks(
        double dataMinDays,
        double dataMaxDays,
        bool fixedMin,
        bool fixedMax,
        int stepDays,
        AxisValueFormat format,
        bool milliseconds)
    {
        var minDate = DateTime.FromOADate(dataMinDays);
        var maxDate = DateTime.FromOADate(dataMaxDays);

        var start = fixedMin ? minDate : StartOfWeek(minDate);
        var end = fixedMax
            ? maxDate
            : start.AddDays(Math.Ceiling((maxDate - start).TotalDays / stepDays) * stepDays);

        if (end <= start)
        {
            end = start.AddDays(stepDays);
        }

        var nominalSeconds = stepDays * SecondsPerDay;
        var labelFormat = ResolveLabelFormat(format, nominalSeconds);
        var showMilliseconds = milliseconds || nominalSeconds < 1;
        var values = new List<double>();
        var labels = new List<string>();

        for (var date = start; date <= end && values.Count < 100; date = date.AddDays(stepDays))
        {
            var value = date.ToOADate();
            values.Add(value);
            labels.Add(AxisValueFormatter.Format(value, labelFormat, showMilliseconds));
        }

        return new AxisTicks
        {
            Min = start.ToOADate(),
            Max = Math.Max(end.ToOADate(), values.Count > 0 ? values[^1] : start.ToOADate()),
            Step = stepDays,
            Decimals = 0,
            Values = values.ToArray(),
            Labels = labels.ToArray(),
        };
    }

    /// <summary>
    /// 按月 / 年对齐的刻度：起点对齐到月初（多个月步长锚定 2000 年 1 月），
    /// 12 的倍数即年步长，刻度落在每年 1 月 1 日。
    /// </summary>
    private static AxisTicks CreateMonthTicks(
        double dataMinDays,
        double dataMaxDays,
        bool fixedMin,
        bool fixedMax,
        int stepMonths,
        AxisValueFormat format,
        bool milliseconds)
    {
        var minDate = DateTime.FromOADate(dataMinDays);
        var maxDate = DateTime.FromOADate(dataMaxDays);

        var start = fixedMin ? minDate : AlignMonthBack(minDate, stepMonths);
        var end = fixedMax ? maxDate : AlignMonthForward(maxDate, stepMonths);
        if (end <= start)
        {
            end = start.AddMonths(stepMonths);
        }

        var nominalSeconds = stepMonths * 30d * SecondsPerDay;
        var labelFormat = ResolveLabelFormat(format, nominalSeconds);
        var showMilliseconds = milliseconds || nominalSeconds < 1;
        var values = new List<double>();
        var labels = new List<string>();

        for (var date = start; date <= end && values.Count < 100; date = date.AddMonths(stepMonths))
        {
            var value = date.ToOADate();
            values.Add(value);
            labels.Add(AxisValueFormatter.Format(value, labelFormat, showMilliseconds));
        }

        return new AxisTicks
        {
            Min = start.ToOADate(),
            Max = Math.Max(end.ToOADate(), values.Count > 0 ? values[^1] : start.ToOADate()),
            Step = stepMonths * 30.436875,
            Decimals = 0,
            Values = values.ToArray(),
            Labels = labels.ToArray(),
        };
    }

    private static AxisTicks CreateNumericWithTimeLabels(
        double dataMinDays,
        double dataMaxDays,
        int count,
        bool fixedMin,
        bool fixedMax,
        AxisValueFormat format,
        bool milliseconds)
    {
        var numeric = CreateNumeric(dataMinDays, dataMaxDays, count, fixedMin, fixedMax);
        var labels = new string[numeric.Values.Length];
        for (var i = 0; i < labels.Length; i++)
        {
            labels[i] = AxisValueFormatter.Format(numeric.Values[i], format, milliseconds);
        }

        return new AxisTicks
        {
            Min = numeric.Min,
            Max = numeric.Max,
            Step = numeric.Step,
            Decimals = numeric.Decimals,
            Values = numeric.Values,
            Labels = labels,
        };
    }

    /// <summary>调整标签格式，避免纯日期 / 纯时间在步长尺度下产生重复标签。</summary>
    private static AxisValueFormat ResolveLabelFormat(AxisValueFormat format, double stepSeconds) => format switch
    {
        // 纯日期标签在不足一天的步长下会重复，回退为日期 + 时间。
        AxisValueFormat.Date when stepSeconds < SecondsPerDay => AxisValueFormat.DateTime,
        // 纯时间标签在跨越一天以上时会重复，回退为日期 + 时间。
        AxisValueFormat.Time when stepSeconds >= SecondsPerDay => AxisValueFormat.DateTime,
        _ => format,
    };

    private static bool TryGetMonthStep(double rawStepSeconds, out int months)
    {
        for (var i = 0; i < CalendarMonthSteps.Length; i++)
        {
            if (rawStepSeconds <= CalendarMonthApproxDays[i] * SecondsPerDay)
            {
                months = CalendarMonthSteps[i];
                return true;
            }
        }

        months = 0;
        return false;
    }

    /// <summary>周对齐：返回该日期所在周的周一 00:00。</summary>
    private static DateTime StartOfWeek(DateTime date)
    {
        var day = date.Date;
        var offset = ((int)day.DayOfWeek + 6) % 7; // Monday = 0 ... Sunday = 6
        return day.AddDays(-offset);
    }

    /// <summary>把日期向前对齐到月份步长的刻度上（锚点 2000-01）。</summary>
    private static DateTime AlignMonthBack(DateTime date, int stepMonths)
    {
        var months = (date.Year - CalendarAnchorYear) * 12 + (date.Month - 1);
        var aligned = FloorDiv(months, stepMonths) * stepMonths;
        return new DateTime(CalendarAnchorYear, 1, 1).AddMonths(aligned);
    }

    /// <summary>返回第一个大于等于 <paramref name="date"/> 的月份步长刻度。</summary>
    private static DateTime AlignMonthForward(DateTime date, int stepMonths)
    {
        var aligned = AlignMonthBack(date, stepMonths);
        while (aligned < date)
        {
            aligned = aligned.AddMonths(stepMonths);
        }

        return aligned;
    }

    private static int FloorDiv(int value, int divisor)
    {
        var quotient = value / divisor;
        if (value % divisor != 0 && (value < 0) != (divisor < 0))
        {
            quotient--;
        }

        return quotient;
    }

    private static string FormatNumeric(double value, int decimals)
    {
        var abs = Math.Abs(value);
        if (abs >= 1e6 || (abs > 0 && abs < 1e-4))
        {
            return value.ToString("0.###E+0", CultureInfo.InvariantCulture);
        }

        return value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
