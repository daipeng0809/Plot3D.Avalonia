namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 一次数据点命中结果：所在系列、点在系列中的索引、三维坐标以及添加点时同步写入的业务标签。
/// </summary>
public readonly struct PlotPointInfo : IEquatable<PlotPointInfo>
{
    /// <summary>使用系列、索引、坐标与标签初始化命中结果。</summary>
    public PlotPointInfo(PlotSeries3D series, int pointIndex, Point3D position, object? tag)
    {
        Series = series;
        PointIndex = pointIndex;
        Position = position;
        Tag = tag;
    }

    /// <summary>数据点所在的系列。</summary>
    public PlotSeries3D Series { get; }

    /// <summary>数据点在系列中的索引。</summary>
    public int PointIndex { get; }

    /// <summary>数据点的三维坐标。</summary>
    public Point3D Position { get; }

    /// <summary>添加该点时同步写入的业务标签（可为 null）。</summary>
    public object? Tag { get; }

    /// <inheritdoc />
    public bool Equals(PlotPointInfo other) =>
        ReferenceEquals(Series, other.Series) &&
        PointIndex == other.PointIndex &&
        Position == other.Position &&
        Equals(Tag, other.Tag);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PlotPointInfo other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Series, PointIndex, Position);

    /// <summary>判断两个命中结果是否相等。</summary>
    public static bool operator ==(PlotPointInfo left, PlotPointInfo right) => left.Equals(right);

    /// <summary>判断两个命中结果是否不相等。</summary>
    public static bool operator !=(PlotPointInfo left, PlotPointInfo right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"{Series.Title ?? "Series"}[{PointIndex}] {Position}";
}

/// <summary>
/// 与数据点相关的事件参数（悬停、选中、取消选中、右键菜单等）。
/// </summary>
public sealed class PlotPointEventArgs : EventArgs
{
    /// <summary>使用命中的数据点初始化参数。</summary>
    public PlotPointEventArgs(PlotPointInfo point)
    {
        Point = point;
    }

    /// <summary>命中的数据点。</summary>
    public PlotPointInfo Point { get; }
}

/// <summary>
/// 构建右键菜单时的事件参数，允许宿主按当前点击位置动态增删菜单项。
/// </summary>
public sealed class PlotContextMenuEventArgs : EventArgs
{
    /// <summary>使用当前菜单项与命中的数据点初始化参数。</summary>
    public PlotContextMenuEventArgs(IList<global::Avalonia.Controls.Control> items, PlotPointInfo? hitPoint)
    {
        Items = items;
        HitPoint = hitPoint;
    }

    /// <summary>菜单项集合（已包含内置项，可增删或替换）。</summary>
    public IList<global::Avalonia.Controls.Control> Items { get; }

    /// <summary>右键点击时命中的数据点；未命中时为 null。</summary>
    public PlotPointInfo? HitPoint { get; }
}
