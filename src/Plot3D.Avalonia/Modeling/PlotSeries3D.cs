using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 所有可绘制三维图形的基类。内置 <see cref="LineSeries3D"/> 与 <see cref="ScatterSeries3D"/>；
/// 通过继承该类并重写 <see cref="Render"/> 可以扩展新的图形类型（曲面、柱状、矢量场等）。
/// </summary>
public abstract class PlotSeries3D : AvaloniaObject
{
    /// <summary>图例 / 系列名称。</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PlotSeries3D, string?>(nameof(Title));

    /// <summary>是否参与绘制。</summary>
    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<PlotSeries3D, bool>(nameof(IsVisible), true);

    /// <summary>取色方式。</summary>
    public static readonly StyledProperty<SeriesColorMode> ColorModeProperty =
        AvaloniaProperty.Register<PlotSeries3D, SeriesColorMode>(nameof(ColorMode));

    /// <summary>系列颜色，在 <see cref="SeriesColorMode.Fixed"/> 下始终生效，在 <see cref="SeriesColorMode.Auto"/> 下显式赋值后生效。</summary>
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<PlotSeries3D, Color>(nameof(Color), Colors.SteelBlue);

    /// <summary>透明度（0..1）。</summary>
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<PlotSeries3D, double>(nameof(Opacity), 1.0);

    /// <summary>线宽（设备无关像素）。</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<PlotSeries3D, double>(nameof(StrokeThickness), 1.5);

    /// <summary>线型。</summary>
    public static readonly StyledProperty<LineStyle> LineStyleProperty =
        AvaloniaProperty.Register<PlotSeries3D, LineStyle>(nameof(LineStyle));

    /// <summary>标记点尺寸（设备无关像素），0 表示不绘制标记点。</summary>
    public static readonly StyledProperty<double> MarkerSizeProperty =
        AvaloniaProperty.Register<PlotSeries3D, double>(nameof(MarkerSize), 0);

    /// <summary>标记点形状。</summary>
    public static readonly StyledProperty<MarkerShape> MarkerShapeProperty =
        AvaloniaProperty.Register<PlotSeries3D, MarkerShape>(nameof(MarkerShape));

    private Color? _randomColor;

    /// <summary>当系列数据发生变化时触发，用于通知控件重绘并重新计算坐标范围。</summary>
    public event EventHandler? DataChanged;

    /// <summary>图例 / 系列名称。</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>是否参与绘制。</summary>
    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    /// <summary>取色方式。</summary>
    public SeriesColorMode ColorMode
    {
        get => GetValue(ColorModeProperty);
        set => SetValue(ColorModeProperty, value);
    }

    /// <summary>系列颜色。</summary>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>透明度（0..1）。</summary>
    public double Opacity
    {
        get => GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    /// <summary>线宽（设备无关像素）。</summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>线型。</summary>
    public LineStyle LineStyle
    {
        get => GetValue(LineStyleProperty);
        set => SetValue(LineStyleProperty, value);
    }

    /// <summary>标记点尺寸，0 表示不绘制标记点。</summary>
    public double MarkerSize
    {
        get => GetValue(MarkerSizeProperty);
        set => SetValue(MarkerSizeProperty, value);
    }

    /// <summary>标记点形状。</summary>
    public MarkerShape MarkerShape
    {
        get => GetValue(MarkerShapeProperty);
        set => SetValue(MarkerShapeProperty, value);
    }

    /// <summary>当前系列的数据包围盒，用于自动计算坐标轴范围。</summary>
    public abstract BoundingBox3D GetBounds();

    /// <summary>把系列绘制到指定上下文。</summary>
    protected internal abstract void Render(SeriesRenderContext context);

    /// <summary>通知控件系列数据已变化。</summary>
    protected void RaiseDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>在随机取色模式下获取（并缓存）随机颜色。</summary>
    internal Color ResolveRandomColor() => _randomColor ??= ColorGenerator.CreateRandom(Random.Shared);

    /// <summary>颜色模式变化时使随机颜色失效。</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorModeProperty && ColorMode != SeriesColorMode.Random)
        {
            _randomColor = null;
        }
    }
}
