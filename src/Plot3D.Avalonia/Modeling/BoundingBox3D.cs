namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维轴对齐包围盒，用于自动计算坐标轴范围。
/// </summary>
public readonly struct BoundingBox3D : IEquatable<BoundingBox3D>
{
    private readonly double _minX;
    private readonly double _minY;
    private readonly double _minZ;
    private readonly double _maxX;
    private readonly double _maxY;
    private readonly double _maxZ;

    /// <summary>使用指定的最小 / 最大角点初始化包围盒。</summary>
    public BoundingBox3D(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        _minX = Math.Min(minX, maxX);
        _minY = Math.Min(minY, maxY);
        _minZ = Math.Min(minZ, maxZ);
        _maxX = Math.Max(minX, maxX);
        _maxY = Math.Max(minY, maxY);
        _maxZ = Math.Max(minZ, maxZ);
    }

    private BoundingBox3D(bool empty)
    {
        _ = empty;
        _minX = _minY = _minZ = double.PositiveInfinity;
        _maxX = _maxY = _maxZ = double.NegativeInfinity;
    }

    /// <summary>空包围盒（不包含任何点）。</summary>
    public static BoundingBox3D Empty => new(true);

    /// <summary>是否为空（没有任何点被加入）。</summary>
    public bool IsEmpty => !(_minX <= _maxX && _minY <= _maxY && _minZ <= _maxZ);

    /// <summary>最小 X。</summary>
    public double MinX => _minX;

    /// <summary>最小 Y。</summary>
    public double MinY => _minY;

    /// <summary>最小 Z。</summary>
    public double MinZ => _minZ;

    /// <summary>最大 X。</summary>
    public double MaxX => _maxX;

    /// <summary>最大 Y。</summary>
    public double MaxY => _maxY;

    /// <summary>最大 Z。</summary>
    public double MaxZ => _maxZ;

    /// <summary>中心点。</summary>
    public Point3D Center => IsEmpty
        ? Point3D.Origin
        : new Point3D((_minX + _maxX) * 0.5, (_minY + _maxY) * 0.5, (_minZ + _maxZ) * 0.5);

    /// <summary>X 方向长度。</summary>
    public double SizeX => IsEmpty ? 0 : _maxX - _minX;

    /// <summary>Y 方向长度。</summary>
    public double SizeY => IsEmpty ? 0 : _maxY - _minY;

    /// <summary>Z 方向长度。</summary>
    public double SizeZ => IsEmpty ? 0 : _maxZ - _minZ;

    /// <summary>对角线长度。</summary>
    public double DiagonalLength
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }

            var dx = _maxX - _minX;
            var dy = _maxY - _minY;
            var dz = _maxZ - _minZ;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>并入一个点，返回新的包围盒。</summary>
    public BoundingBox3D Union(double x, double y, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
        {
            return this;
        }

        return new BoundingBox3D(
            Math.Min(_minX, x), Math.Min(_minY, y), Math.Min(_minZ, z),
            Math.Max(_maxX, x), Math.Max(_maxY, y), Math.Max(_maxZ, z));
    }

    /// <summary>并入一个点，返回新的包围盒。</summary>
    public BoundingBox3D Union(Point3D point) => Union(point.X, point.Y, point.Z);

    /// <summary>并入另一个包围盒，返回新的包围盒。</summary>
    public BoundingBox3D Union(BoundingBox3D other)
    {
        if (other.IsEmpty)
        {
            return this;
        }

        if (IsEmpty)
        {
            return other;
        }

        return new BoundingBox3D(
            Math.Min(_minX, other._minX), Math.Min(_minY, other._minY), Math.Min(_minZ, other._minZ),
            Math.Max(_maxX, other._maxX), Math.Max(_maxY, other._maxY), Math.Max(_maxZ, other._maxZ));
    }

    /// <summary>判断点是否位于包围盒内（包含边界）。</summary>
    public bool Contains(double x, double y, double z) =>
        !IsEmpty && x >= _minX && x <= _maxX && y >= _minY && y <= _maxY && z >= _minZ && z <= _maxZ;

    /// <inheritdoc />
    public bool Equals(BoundingBox3D other) =>
        _minX.Equals(other._minX) && _minY.Equals(other._minY) && _minZ.Equals(other._minZ) &&
        _maxX.Equals(other._maxX) && _maxY.Equals(other._maxY) && _maxZ.Equals(other._maxZ);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BoundingBox3D other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_minX, _minY, _minZ, _maxX, _maxY, _maxZ);

    /// <summary>判断两个包围盒是否相等。</summary>
    public static bool operator ==(BoundingBox3D left, BoundingBox3D right) => left.Equals(right);

    /// <summary>判断两个包围盒是否不相等。</summary>
    public static bool operator !=(BoundingBox3D left, BoundingBox3D right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsEmpty
        ? "Empty"
        : $"X[{_minX:0.###},{_maxX:0.###}] Y[{_minY:0.###},{_maxY:0.###}] Z[{_minZ:0.###},{_maxZ:0.###}]";
}
