using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 传递给 <see cref="PlotSeries3D"/> 的渲染上下文，封装了绘图表面、投影器以及解析后的系列颜色。
/// </summary>
/// <remarks>
/// 该类型主要供自定义系列（继承 <see cref="PlotSeries3D"/>）使用，普通调用方无需直接创建。
/// </remarks>
public sealed class SeriesRenderContext
{
    internal SeriesRenderContext(
        DrawingContext drawingContext,
        in PlotProjection projection,
        Color color,
        double opacity,
        Rect viewBounds)
    {
        DrawingContext = drawingContext;
        Projection = projection;
        Color = color;
        Opacity = double.IsFinite(opacity) ? Math.Clamp(opacity, 0, 1) : 1;
        ViewBounds = viewBounds;
    }

    /// <summary>当前绘图表面。</summary>
    public DrawingContext DrawingContext { get; }

    /// <summary>当前三维到二维的投影器。</summary>
    public PlotProjection Projection { get; }

    /// <summary>已解析的系列颜色（考虑取色模式与调色板）。</summary>
    public Color Color { get; }

    /// <summary>系列透明度（0..1）。</summary>
    public double Opacity { get; }

    /// <summary>控件的完整绘制区域。</summary>
    public Rect ViewBounds { get; }

    /// <summary>把三维点投影为二维点，失败时返回 false。</summary>
    public bool TryProject(Point3D point, out Point screen) => Projection.TryProject(point, out screen);

    /// <summary>使用系列颜色创建纯色画刷。</summary>
    public IBrush CreateBrush() => CreateBrush(Color);

    /// <summary>使用指定颜色（叠加系列透明度）创建纯色画刷。</summary>
    public IBrush CreateBrush(Color color) =>
        new ImmutableSolidColorBrush(ColorGenerator.WithOpacity(color, Opacity));

    /// <summary>使用系列颜色创建画笔。</summary>
    public Pen CreatePen(double thickness, LineStyle lineStyle) => CreatePen(Color, thickness, lineStyle);

    /// <summary>使用指定颜色创建画笔。</summary>
    public Pen CreatePen(Color color, double thickness, LineStyle lineStyle)
    {
        var brush = new ImmutableSolidColorBrush(ColorGenerator.WithOpacity(color, Opacity));
        IDashStyle? dash = lineStyle switch
        {
            LineStyle.Dash => DashStyle.Dash,
            LineStyle.Dot => DashStyle.Dot,
            LineStyle.DashDot => DashStyle.DashDot,
            _ => null,
        };

        var safeThickness = double.IsFinite(thickness) ? Math.Max(0.1, thickness) : 1;
        return new Pen(brush, safeThickness, dash, PenLineCap.Round, PenLineJoin.Round);
    }
}
