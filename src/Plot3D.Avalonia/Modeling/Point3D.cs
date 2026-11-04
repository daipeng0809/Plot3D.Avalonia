using System.Globalization;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维空间中的一个点（双精度）。
/// </summary>
public readonly struct Point3D : IEquatable<Point3D>
{
    /// <summary>初始化一个新的 <see cref="Point3D"/>。</summary>
    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>X 分量。</summary>
    public double X { get; }

    /// <summary>Y 分量。</summary>
    public double Y { get; }

    /// <summary>Z 分量。</summary>
    public double Z { get; }

    /// <summary>原点 (0,0,0)。</summary>
    public static Point3D Origin => default;

    /// <summary>三个分量是否都是有限数值（非 NaN / 无穷大）。</summary>
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);

    /// <summary>向量长度。</summary>
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>按分量相加。</summary>
    public static Point3D operator +(Point3D a, Point3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>按分量相减。</summary>
    public static Point3D operator -(Point3D a, Point3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>取反。</summary>
    public static Point3D operator -(Point3D a) => new(-a.X, -a.Y, -a.Z);

    /// <summary>按标量缩放。</summary>
    public static Point3D operator *(Point3D a, double scale) => new(a.X * scale, a.Y * scale, a.Z * scale);

    /// <summary>按标量缩放。</summary>
    public static Point3D operator *(double scale, Point3D a) => a * scale;

    /// <summary>按标量除法。</summary>
    public static Point3D operator /(Point3D a, double scale) => new(a.X / scale, a.Y / scale, a.Z / scale);

    /// <summary>点积。</summary>
    public static double Dot(Point3D a, Point3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>叉积。</summary>
    public static Point3D Cross(Point3D a, Point3D b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>两点之间的距离。</summary>
    public static double Distance(Point3D a, Point3D b) => (a - b).Length;

    /// <summary>线性插值。</summary>
    public static Point3D Lerp(Point3D a, Point3D b, double t) => new(
        a.X + (b.X - a.X) * t,
        a.Y + (b.Y - a.Y) * t,
        a.Z + (b.Z - a.Z) * t);

    /// <inheritdoc />
    public bool Equals(Point3D other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>判断两个点是否相等。</summary>
    public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

    /// <summary>判断两个点是否不相等。</summary>
    public static bool operator !=(Point3D left, Point3D right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({X:0.###}, {Y:0.###}, {Z:0.###})");
}
