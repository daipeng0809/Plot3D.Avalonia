using System.Globalization;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 单个坐标轴上的数值范围。
/// </summary>
public readonly struct AxisRange : IEquatable<AxisRange>
{
    /// <summary>使用最小值和最大值初始化范围；若 <paramref name="min"/> 大于 <paramref name="max"/> 会自动交换。</summary>
    public AxisRange(double min, double max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        Min = min;
        Max = max;
    }

    /// <summary>范围最小值。</summary>
    public double Min { get; }

    /// <summary>范围最大值。</summary>
    public double Max { get; }

    /// <summary>范围长度。</summary>
    public double Length => Max - Min;

    /// <summary>范围是否有效（有限数值且最大值大于最小值）。</summary>
    public bool IsValid => double.IsFinite(Min) && double.IsFinite(Max) && Max > Min;

    /// <summary>表示“未设置 / 自动计算”的空范围。</summary>
    public static AxisRange Empty => new(double.NaN, double.NaN);

    /// <summary>把数值限制到当前范围内。</summary>
    public double Clamp(double value) => value < Min ? Min : value > Max ? Max : value;

    /// <inheritdoc />
    public bool Equals(AxisRange other) => Min.Equals(other.Min) && Max.Equals(other.Max);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AxisRange other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Min, Max);

    /// <summary>判断两个范围是否相等。</summary>
    public static bool operator ==(AxisRange left, AxisRange right) => left.Equals(right);

    /// <summary>判断两个范围是否不相等。</summary>
    public static bool operator !=(AxisRange left, AxisRange right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid
            ? string.Create(CultureInfo.InvariantCulture, $"[{Min:0.###}, {Max:0.###}]")
            : "Auto";
}
