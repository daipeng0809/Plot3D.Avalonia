using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 把大量标记点合并进单个 <see cref="StreamGeometry"/> 一次性填充，避免逐点绘制调用的开销。
/// </summary>
internal static class MarkerRenderer
{
    public static void Draw(SeriesRenderContext context, ReadOnlySpan<Point3D> points, double size, MarkerShape shape)
    {
        if (points.Length == 0 || !(size > 0) || !double.IsFinite(size))
        {
            return;
        }

        var projection = context.Projection;
        var geometry = new StreamGeometry();
        var any = false;

        using (var g = geometry.Open())
        {
            foreach (var point in points)
            {
                if (!projection.TryProject(point.X, point.Y, point.Z, out var x, out var y))
                {
                    continue;
                }

                AddMarker(g, x, y, size, shape);
                any = true;
            }
        }

        if (any)
        {
            context.DrawingContext.DrawGeometry(context.CreateBrush(), null, geometry);
        }
    }

    /// <summary>
    /// 绘制单个标记点，可指定独立颜色与描边（用于“最新点高亮”“悬停高亮”“选中高亮”等强调效果）。
    /// </summary>
    public static void DrawSingle(
        SeriesRenderContext context,
        in Point3D point,
        double size,
        MarkerShape shape,
        Color color,
        double strokeThickness = 0)
    {
        if (!(size > 0) || !double.IsFinite(size))
        {
            return;
        }

        if (!context.Projection.TryProject(point.X, point.Y, point.Z, out var x, out var y))
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            AddMarker(g, x, y, size, shape);
        }

        Pen? pen = strokeThickness > 0
            ? new Pen(new ImmutableSolidColorBrush(Color.FromArgb(220, 255, 255, 255)), strokeThickness)
            : null;

        context.DrawingContext.DrawGeometry(context.CreateBrush(color), pen, geometry);
    }

    private static void AddMarker(StreamGeometryContext g, double cx, double cy, double size, MarkerShape shape)
    {
        var radius = size * 0.5;

        // 很小的标记用方形即可，几何数据量最小且视觉差异不可见。
        if (size <= 2.5)
        {
            g.BeginFigure(new Point(cx - radius, cy - radius), true);
            g.LineTo(new Point(cx + radius, cy - radius));
            g.LineTo(new Point(cx + radius, cy + radius));
            g.LineTo(new Point(cx - radius, cy + radius));
            g.EndFigure(true);
            return;
        }

        switch (shape)
        {
            case MarkerShape.Square:
                g.BeginFigure(new Point(cx - radius, cy - radius), true);
                g.LineTo(new Point(cx + radius, cy - radius));
                g.LineTo(new Point(cx + radius, cy + radius));
                g.LineTo(new Point(cx - radius, cy + radius));
                g.EndFigure(true);
                break;

            case MarkerShape.Diamond:
                g.BeginFigure(new Point(cx, cy - radius), true);
                g.LineTo(new Point(cx + radius, cy));
                g.LineTo(new Point(cx, cy + radius));
                g.LineTo(new Point(cx - radius, cy));
                g.EndFigure(true);
                break;

            case MarkerShape.Triangle:
                var halfBase = radius * 0.8660254037844386; // sqrt(3)/2
                g.BeginFigure(new Point(cx, cy - radius), true);
                g.LineTo(new Point(cx + halfBase, cy + radius * 0.5));
                g.LineTo(new Point(cx - halfBase, cy + radius * 0.5));
                g.EndFigure(true);
                break;

            default:
                var sides = size < 6 ? 8 : size < 12 ? 12 : 16;
                for (var i = 0; i < sides; i++)
                {
                    var angle = Math.PI * 2 * i / sides;
                    var px = cx + radius * Math.Cos(angle);
                    var py = cy + radius * Math.Sin(angle);
                    if (i == 0)
                    {
                        g.BeginFigure(new Point(px, py), true);
                    }
                    else
                    {
                        g.LineTo(new Point(px, py));
                    }
                }

                g.EndFigure(true);
                break;
        }
    }
}
