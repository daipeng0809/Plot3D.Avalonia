using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维散点系列。所有标记点合并到同一个几何体中一次性填充，可高效绘制数万个点。
/// </summary>
public class ScatterSeries3D : PlotSeries3D
{
    private readonly List<Point3D> _points = new();

    static ScatterSeries3D()
    {
        MarkerSizeProperty.OverrideDefaultValue<ScatterSeries3D>(6);
    }

    /// <summary>创建一个空的三维散点系列。</summary>
    public ScatterSeries3D()
    {
    }

    /// <summary>使用指定的点创建一个三维散点系列。</summary>
    public ScatterSeries3D(params Point3D[] points)
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
        _points.AddRange(points);
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
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_points);
        for (var i = 0; i < span.Length; i++)
        {
            bounds = bounds.Union(span[i]);
        }

        return bounds;
    }

    /// <inheritdoc />
    protected internal override void Render(SeriesRenderContext context)
    {
        var points = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_points);
        MarkerRenderer.Draw(context, points, MarkerSize, MarkerShape);
    }
}
