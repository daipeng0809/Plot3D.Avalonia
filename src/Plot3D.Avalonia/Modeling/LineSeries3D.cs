using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维折线 / 曲线系列。支持任意数量的点、NaN 断线、多种线型与可选的顶点标记。
/// </summary>
public class LineSeries3D : PlotSeries3D
{
    private readonly List<Point3D> _points = new();

    /// <summary>创建一个空的三维折线系列。</summary>
    public LineSeries3D()
    {
    }

    /// <summary>使用指定的点创建一个三维折线系列。</summary>
    public LineSeries3D(params Point3D[] points)
    {
        if (points is { Length: > 0 })
        {
            _points.AddRange(points);
        }
    }

    /// <summary>
    /// 直接访问底层点列表。批量修改后需要调用 <see cref="InvalidateData"/> 通知控件刷新。
    /// </summary>
    public List<Point3D> Points => _points;

    /// <summary>当前点数。</summary>
    public int Count => _points.Count;

    /// <summary>替换全部点数据（自动通知刷新）。</summary>
    public void SetPoints(IEnumerable<Point3D> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _points.Clear();
        if (points is Point3D[] array)
        {
            _points.AddRange(array);
        }
        else if (points is List<Point3D> list)
        {
            _points.AddRange(list);
        }
        else
        {
            foreach (var point in points)
            {
                _points.Add(point);
            }
        }

        RaiseDataChanged();
    }

    /// <summary>替换全部点数据（自动通知刷新）。</summary>
    public void SetPoints(ReadOnlySpan<Point3D> points)
    {
        _points.Clear();
        foreach (var point in points)
        {
            _points.Add(point);
        }

        RaiseDataChanged();
    }

    /// <summary>追加一个点（自动通知刷新）。</summary>
    public void Add(Point3D point)
    {
        _points.Add(point);
        RaiseDataChanged();
    }

    /// <summary>追加多个点（自动通知刷新）。</summary>
    public void AddRange(IEnumerable<Point3D> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _points.AddRange(points);
        RaiseDataChanged();
    }

    /// <summary>清空所有点（自动通知刷新）。</summary>
    public void Clear()
    {
        if (_points.Count == 0)
        {
            return;
        }

        _points.Clear();
        RaiseDataChanged();
    }

    /// <summary>在直接修改 <see cref="Points"/> 后调用，通知控件重新计算范围并刷新。</summary>
    public void InvalidateData() => RaiseDataChanged();

    /// <inheritdoc />
    public override BoundingBox3D GetBounds()
    {
        var bounds = BoundingBox3D.Empty;
        var span = CollectionsMarshal.AsSpan(_points);
        for (var i = 0; i < span.Length; i++)
        {
            bounds = bounds.Union(span[i]);
        }

        return bounds;
    }

    /// <inheritdoc />
    protected internal override void Render(SeriesRenderContext context)
    {
        var points = CollectionsMarshal.AsSpan(_points);
        if (points.Length == 0)
        {
            return;
        }

        var projection = context.Projection;
        var markerSize = MarkerSize;

        if (points.Length == 1)
        {
            MarkerRenderer.Draw(context, points, Math.Max(markerSize, 5), MarkerShape);
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
    }
}
