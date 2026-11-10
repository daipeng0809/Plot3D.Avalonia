using Avalonia;
using Plot3D.Avalonia.Modeling;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Controls;

/// <summary>
/// <see cref="Plot3DView"/> 的外观参数：透明背景、坐标轴数值格式与刻度 / 标题排版微调。
/// </summary>
public partial class Plot3DView
{
    #region 属性注册

    /// <summary>是否使用完全透明的背景（不填充任何背景色），便于叠加到其它控件上。</summary>
    public static readonly StyledProperty<bool> IsBackgroundTransparentProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsBackgroundTransparent));

    /// <summary>X 轴数值格式（数值 / 日期 / 时间 / 日期 + 时间）。</summary>
    public static readonly StyledProperty<AxisValueFormat> XAxisValueFormatProperty =
        AvaloniaProperty.Register<Plot3DView, AxisValueFormat>(nameof(XAxisValueFormat));

    /// <summary>Y 轴数值格式。</summary>
    public static readonly StyledProperty<AxisValueFormat> YAxisValueFormatProperty =
        AvaloniaProperty.Register<Plot3DView, AxisValueFormat>(nameof(YAxisValueFormat));

    /// <summary>Z 轴数值格式。</summary>
    public static readonly StyledProperty<AxisValueFormat> ZAxisValueFormatProperty =
        AvaloniaProperty.Register<Plot3DView, AxisValueFormat>(nameof(ZAxisValueFormat));

    /// <summary>时间格式是否输出毫秒（步长小于 1 秒时始终输出毫秒）。</summary>
    public static readonly StyledProperty<bool> AxisTimeMillisecondsProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(AxisTimeMilliseconds));

    /// <summary>刻度线长度（像素）。</summary>
    public static readonly StyledProperty<double> TickLengthProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(TickLength), 4.0);

    /// <summary>刻度文本与刻度线之间的距离（像素，可为负值）。</summary>
    public static readonly StyledProperty<double> TickLabelGapProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(TickLabelGap), 3.0);

    /// <summary>刻度文本的整体偏移（像素，沿屏幕坐标方向）。</summary>
    public static readonly StyledProperty<Vector> TickLabelOffsetProperty =
        AvaloniaProperty.Register<Plot3DView, Vector>(nameof(TickLabelOffset));

    /// <summary>刻度文本字号；NaN 表示使用 <see cref="FontSize"/>。</summary>
    public static readonly StyledProperty<double> TickLabelFontSizeProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(TickLabelFontSize), double.NaN);

    /// <summary>轴标题与刻度文本之间的额外间距（像素）。</summary>
    public static readonly StyledProperty<double> AxisTitleGapProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(AxisTitleGap));

    /// <summary>轴标题的整体偏移（像素，沿屏幕坐标方向）。</summary>
    public static readonly StyledProperty<Vector> AxisTitleOffsetProperty =
        AvaloniaProperty.Register<Plot3DView, Vector>(nameof(AxisTitleOffset));

    /// <summary>轴标题字号；NaN 表示使用 <see cref="FontSize"/>。</summary>
    public static readonly StyledProperty<double> AxisTitleFontSizeProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(AxisTitleFontSize), double.NaN);

    #endregion

    /// <summary>是否使用完全透明的背景。</summary>
    public bool IsBackgroundTransparent
    {
        get => GetValue(IsBackgroundTransparentProperty);
        set => SetValue(IsBackgroundTransparentProperty, value);
    }

    /// <summary>X 轴数值格式。</summary>
    public AxisValueFormat XAxisValueFormat
    {
        get => GetValue(XAxisValueFormatProperty);
        set => SetValue(XAxisValueFormatProperty, value);
    }

    /// <summary>Y 轴数值格式。</summary>
    public AxisValueFormat YAxisValueFormat
    {
        get => GetValue(YAxisValueFormatProperty);
        set => SetValue(YAxisValueFormatProperty, value);
    }

    /// <summary>Z 轴数值格式。</summary>
    public AxisValueFormat ZAxisValueFormat
    {
        get => GetValue(ZAxisValueFormatProperty);
        set => SetValue(ZAxisValueFormatProperty, value);
    }

    /// <summary>时间格式是否输出毫秒。</summary>
    public bool AxisTimeMilliseconds
    {
        get => GetValue(AxisTimeMillisecondsProperty);
        set => SetValue(AxisTimeMillisecondsProperty, value);
    }

    /// <summary>刻度线长度（像素）。</summary>
    public double TickLength
    {
        get => GetValue(TickLengthProperty);
        set => SetValue(TickLengthProperty, value);
    }

    /// <summary>刻度文本与刻度线之间的距离（像素）。</summary>
    public double TickLabelGap
    {
        get => GetValue(TickLabelGapProperty);
        set => SetValue(TickLabelGapProperty, value);
    }

    /// <summary>刻度文本的整体偏移（像素）。</summary>
    public Vector TickLabelOffset
    {
        get => GetValue(TickLabelOffsetProperty);
        set => SetValue(TickLabelOffsetProperty, value);
    }

    /// <summary>刻度文本字号；NaN 表示使用 <see cref="FontSize"/>。</summary>
    public double TickLabelFontSize
    {
        get => GetValue(TickLabelFontSizeProperty);
        set => SetValue(TickLabelFontSizeProperty, value);
    }

    /// <summary>轴标题与刻度文本之间的额外间距（像素）。</summary>
    public double AxisTitleGap
    {
        get => GetValue(AxisTitleGapProperty);
        set => SetValue(AxisTitleGapProperty, value);
    }

    /// <summary>轴标题的整体偏移（像素）。</summary>
    public Vector AxisTitleOffset
    {
        get => GetValue(AxisTitleOffsetProperty);
        set => SetValue(AxisTitleOffsetProperty, value);
    }

    /// <summary>轴标题字号；NaN 表示使用 <see cref="FontSize"/>。</summary>
    public double AxisTitleFontSize
    {
        get => GetValue(AxisTitleFontSizeProperty);
        set => SetValue(AxisTitleFontSizeProperty, value);
    }

    /// <summary>解析后的刻度文本字号（未设置时回退到 <see cref="FontSize"/>）。</summary>
    internal double ResolveTickLabelFontSize() => ResolveFontSize(TickLabelFontSize);

    /// <summary>解析后的轴标题字号（未设置时回退到 <see cref="FontSize"/>）。</summary>
    internal double ResolveAxisTitleFontSize() => ResolveFontSize(AxisTitleFontSize);

    private double ResolveFontSize(double value)
    {
        if (double.IsFinite(value) && value > 0.1)
        {
            return Math.Clamp(value, 4, 96);
        }

        var fallback = double.IsFinite(FontSize) ? FontSize : 12;
        return Math.Clamp(fallback, 4, 96);
    }

    /// <summary>按 X 轴格式把数值转换为标签文本（时间格式需传入 OADate）。</summary>
    public string FormatXAxisValue(double value) =>
        AxisValueFormatter.Format(value, XAxisValueFormat, AxisTimeMilliseconds);

    /// <summary>按 Y 轴格式把数值转换为标签文本。</summary>
    public string FormatYAxisValue(double value) =>
        AxisValueFormatter.Format(value, YAxisValueFormat, AxisTimeMilliseconds);

    /// <summary>按 Z 轴格式把数值转换为标签文本。</summary>
    public string FormatZAxisValue(double value) =>
        AxisValueFormatter.Format(value, ZAxisValueFormat, AxisTimeMilliseconds);
}
