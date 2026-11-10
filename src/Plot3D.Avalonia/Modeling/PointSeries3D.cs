using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 由离散三维点构成的系列基类（折线 / 散点），统一维护点坐标、逐点业务标签（Tag）
/// 以及“最新点高亮”样式。
/// </summary>
/// <remarks>
/// <see cref="Points"/> 与 <see cref="Tags"/> 按索引一一对应；直接修改 <see cref="Points"/> 后
/// 需要调用 <see cref="InvalidateData"/> 通知控件刷新。
/// </remarks>
public abstract class PointSeries3D : PlotSeries3D
{
    /// <summary>是否用不同颜色 / 尺寸突出显示最新（索引最大）的数据点，便于识别曲线方向。</summary>
    public static readonly StyledProperty<bool> HighlightLatestPointProperty =
        AvaloniaProperty.Register<PointSeries3D, bool>(nameof(HighlightLatestPoint));

    /// <summary>最新点颜色。</summary>
    public static readonly StyledProperty<Color> LatestPointColorProperty =
        AvaloniaProperty.Register<PointSeries3D, Color>(nameof(LatestPointColor), Color.Parse("#FF3B30"));

    /// <summary>最新点标记尺寸；0 表示自动（普通标记尺寸的 1.8 倍，且不小于 7）。</summary>
    public static readonly StyledProperty<double> LatestPointSizeProperty =
        AvaloniaProperty.Register<PointSeries3D, double>(nameof(LatestPointSize));

    /// <summary>最新点标记形状。</summary>
    public static readonly StyledProperty<MarkerShape> LatestPointShapeProperty =
        AvaloniaProperty.Register<PointSeries3D, MarkerShape>(nameof(LatestPointShape));

    private readonly List<Point3D> _points = new();
    private readonly List<object?> _tags = new();

    /// <summary>当前投影下的屏幕坐标缓存，避免每次鼠标移动重复投影全部点。</summary>
    private Point[] _screenPoints = [];

    private PlotProjection _cachedProjection;
    private bool _screenCacheValid;

    /// <summary>
    /// 底层点列表。允许直接读写以换取最高性能；批量修改后需要调用 <see cref="InvalidateData"/> 通知控件刷新。
    /// </summary>
    public List<Point3D> Points => _points;

    /// <summary>
    /// 与 <see cref="Points"/> 一一对应的业务标签，用于悬停提示等交互；缺失或为 null 时使用坐标文本。
    /// </summary>
    public List<object?> Tags => _tags;

    /// <summary>当前点数。</summary>
    public int Count => _points.Count;

    /// <summary>获取指定索引的点。</summary>
    public Point3D this[int index] => _points[index];

    /// <summary>是否突出显示最新点。</summary>
    public bool HighlightLatestPoint
    {
        get => GetValue(HighlightLatestPointProperty);
        set => SetValue(HighlightLatestPointProperty, value);
    }

    /// <summary>最新点颜色。</summary>
    public Color LatestPointColor
    {
        get => GetValue(LatestPointColorProperty);
        set => SetValue(LatestPointColorProperty, value);
    }

    /// <summary>最新点标记尺寸；0 表示自动。</summary>
    public double LatestPointSize
    {
        get => GetValue(LatestPointSizeProperty);
        set => SetValue(LatestPointSizeProperty, value);
    }

    /// <summary>最新点标记形状。</summary>
    public MarkerShape LatestPointShape
    {
        get => GetValue(LatestPointShapeProperty);
        set => SetValue(LatestPointShapeProperty, value);
    }

    /// <summary>替换全部点数据并清空标签（自动通知刷新）。</summary>
    public void SetPoints(IEnumerable<Point3D> points)
    {
        SetPoints(points, null);
    }

    /// <summary>
    /// 替换全部点数据与逐点标签（自动通知刷新）。标签数量不足时自动补 null。
    /// </summary>
    public void SetPoints(IEnumerable<Point3D> points, IEnumerable<object?>? tags)
    {
        ArgumentNullException.ThrowIfNull(points);

        _points.Clear();
        _tags.Clear();

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

        if (tags is not null)
        {
            if (tags is object?[] tagArray)
            {
                _tags.AddRange(tagArray);
            }
            else if (tags is List<object?> tagList)
            {
                _tags.AddRange(tagList);
            }
            else
            {
                foreach (var tag in tags)
                {
                    _tags.Add(tag);
                }
            }

            if (_tags.Count > _points.Count)
            {
                _tags.RemoveRange(_points.Count, _tags.Count - _points.Count);
            }
        }

        while (_tags.Count < _points.Count)
        {
            _tags.Add(null);
        }

        InvalidateScreenCache();
        RaiseDataChanged();
    }

    /// <summary>替换全部点数据（自动通知刷新）。</summary>
    public void SetPoints(ReadOnlySpan<Point3D> points)
    {
        _points.Clear();
        _tags.Clear();
        foreach (var point in points)
        {
            _points.Add(point);
            _tags.Add(null);
        }

        InvalidateScreenCache();
        RaiseDataChanged();
    }

    /// <summary>追加一个点（自动通知刷新）。</summary>
    public void Add(Point3D point) => Add(point, null);

    /// <summary>追加一个点及对应的业务标签（自动通知刷新）。</summary>
    public void Add(Point3D point, object? tag)
    {
        _points.Add(point);
        _tags.Add(tag);
        InvalidateScreenCache();
        RaiseDataChanged();
    }

    /// <summary>追加多个点（自动通知刷新）。</summary>
    public void AddRange(IEnumerable<Point3D> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        foreach (var point in points)
        {
            _points.Add(point);
            _tags.Add(null);
        }

        InvalidateScreenCache();
        RaiseDataChanged();
    }

    /// <summary>追加多个点及对应的业务标签（自动通知刷新）。</summary>
    public void AddRange(IEnumerable<Point3D> points, IEnumerable<object?>? tags)
    {
        ArgumentNullException.ThrowIfNull(points);

        using var pointEnumerator = points.GetEnumerator();
        using var tagEnumerator = tags?.GetEnumerator();
        while (pointEnumerator.MoveNext())
        {
            _points.Add(pointEnumerator.Current);
            _tags.Add(tagEnumerator is not null && tagEnumerator.MoveNext() ? tagEnumerator.Current : null);
        }

        InvalidateScreenCache();
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
        _tags.Clear();
        InvalidateScreenCache();
        RaiseDataChanged();
    }

    /// <summary>删除指定索引的点（自动通知刷新），成功返回 true。</summary>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _points.Count)
        {
            return false;
        }

        _points.RemoveAt(index);
        if (index < _tags.Count)
        {
            _tags.RemoveAt(index);
        }

        InvalidateScreenCache();
        RaiseDataChanged();
        return true;
    }

    /// <summary>获取指定索引的业务标签；越界或不存在时返回 null。</summary>
    public object? GetTag(int index) => index >= 0 && index < _tags.Count ? _tags[index] : null;

    /// <summary>设置指定索引的业务标签；索引越界时会被忽略。</summary>
    public void SetTag(int index, object? tag)
    {
        if (index < 0 || index >= _points.Count)
        {
            return;
        }

        while (_tags.Count <= index)
        {
            _tags.Add(null);
        }

        _tags[index] = tag;
    }

    /// <summary>在直接修改 <see cref="Points"/> / <see cref="Tags"/> 后调用，通知控件重新计算范围并刷新。</summary>
    public void InvalidateData()
    {
        InvalidateScreenCache();
        RaiseDataChanged();
    }

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

    /// <summary>
    /// 在屏幕空间中查找距离指定位置最近的数据点（跳过 NaN / 相机后方的点）。
    /// 屏幕坐标按当前投影缓存，鼠标移动时只做距离比较；投影变化或数据变化后自动重建。
    /// </summary>
    /// <param name="projection">当前三维投影器。</param>
    /// <param name="screenPoint">屏幕坐标（设备无关像素）。</param>
    /// <param name="index">最近点的索引，未找到时为 -1。</param>
    /// <param name="distance">最近点的屏幕距离，未找到时为 <see cref="double.PositiveInfinity"/>。</param>
    public bool TryFindNearestPoint(
        in PlotProjection projection,
        Point screenPoint,
        out int index,
        out double distance)
    {
        EnsureScreenCache(projection);

        index = -1;
        distance = double.PositiveInfinity;

        var screenPoints = _screenPoints;
        var bestSquared = double.PositiveInfinity;

        for (var i = 0; i < screenPoints.Length; i++)
        {
            var screen = screenPoints[i];
            if (!double.IsFinite(screen.X) || !double.IsFinite(screen.Y))
            {
                continue;
            }

            var dx = screen.X - screenPoint.X;
            var dy = screen.Y - screenPoint.Y;
            var squared = dx * dx + dy * dy;
            if (squared < bestSquared)
            {
                bestSquared = squared;
                index = i;
            }
        }

        if (index < 0)
        {
            return false;
        }

        distance = Math.Sqrt(bestSquared);
        return true;
    }

    /// <summary>数据变化后使屏幕坐标缓存失效。</summary>
    private void InvalidateScreenCache() => _screenCacheValid = false;

    /// <summary>确保当前投影下的屏幕坐标缓存可用。</summary>
    private void EnsureScreenCache(in PlotProjection projection)
    {
        var count = _points.Count;

        if (_screenCacheValid && _cachedProjection.Equals(projection) && _screenPoints.Length == count)
        {
            return;
        }

        if (_screenPoints.Length != count)
        {
            _screenPoints = new Point[count];
        }

        var span = CollectionsMarshal.AsSpan(_points);
        for (var i = 0; i < span.Length; i++)
        {
            _screenPoints[i] = projection.TryProject(span[i], out var screen)
                ? screen
                : new Point(double.NaN, double.NaN);
        }

        _cachedProjection = projection;
        _screenCacheValid = true;
    }

    /// <summary>用最新点样式绘制最后一个有效点（供派生类在 <c>Render</c> 末尾调用）。</summary>
    protected void RenderLatestPoint(SeriesRenderContext context)
    {
        if (!HighlightLatestPoint)
        {
            return;
        }

        var span = CollectionsMarshal.AsSpan(_points);
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (!span[i].IsFinite)
            {
                continue;
            }

            var size = LatestPointSize > 0 && double.IsFinite(LatestPointSize)
                ? LatestPointSize
                : Math.Max(MarkerSize * 1.8, 7);

            MarkerRenderer.DrawSingle(context, span[i], size, LatestPointShape, LatestPointColor);
            return;
        }
    }
}
