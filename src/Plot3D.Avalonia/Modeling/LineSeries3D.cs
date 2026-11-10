using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维折线 / 曲线系列。支持任意数量的点、NaN 断线、多种线型与可选的顶点标记。
/// </summary>
public class LineSeries3D : PointSeries3D
{
    /// <summary>创建一个空的三维折线系列。</summary>
    public LineSeries3D()
    {
    }

    /// <summary>使用指定的点创建一个三维折线系列。</summary>
    public LineSeries3D(params Point3D[] points)
    {
        if (points is { Length: > 0 })
        {
            SetPoints(points);
        }
    }

    /// <inheritdoc />
    protected internal override void Render(SeriesRenderContext context)
    {
        var points = CollectionsMarshal.AsSpan(Points);
        if (points.Length == 0)
        {
            return;
        }

        var projection = context.Projection;
        var markerSize = MarkerSize;

        if (points.Length == 1)
        {
            MarkerRenderer.Draw(context, points, Math.Max(markerSize, 5), MarkerShape);
            RenderLatestPoint(context);
            return;
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            var figureOpen = false;
            var lastX = 0.0;
            var lastY = 0.0;

            foreach (var point in points)
            {
                if (!projection.TryProject(point.X, point.Y, point.Z, out var sx, out var sy))
                {
                    // 无效点（NaN / 相机后方）中断当前折线。
                    if (figureOpen)
                    {
                        g.EndFigure(false);
                        figureOpen = false;
                    }

                    continue;
                }

                if (figureOpen && Math.Abs(sx - lastX) < 0.05 && Math.Abs(sy - lastY) < 0.05)
                {
                    // 屏幕距离过近的点对视觉无贡献，跳过以减少几何数据量。
                    continue;
                }

                if (!figureOpen)
                {
                    g.BeginFigure(new Point(sx, sy), false);
                    figureOpen = true;
                }
                else
                {
                    g.LineTo(new Point(sx, sy));
                }

                lastX = sx;
                lastY = sy;
            }

            if (figureOpen)
            {
                g.EndFigure(false);
            }
        }

        context.DrawingContext.DrawGeometry(null, context.CreatePen(StrokeThickness, LineStyle), geometry);

        if (markerSize > 0)
        {
            MarkerRenderer.Draw(context, points, markerSize, MarkerShape);
        }

        RenderLatestPoint(context);
    }
}
