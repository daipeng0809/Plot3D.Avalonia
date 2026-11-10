using Avalonia;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 三维到二维的投影器（正交或透视）。由 <see cref="Controls.Plot3DView"/> 在每次布局变化时创建，
/// 并传递给每个 <see cref="PlotSeries3D"/> 用于绘制。
/// </summary>
public readonly struct PlotProjection : IEquatable<PlotProjection>
{
    /// <summary>射线与包围盒求交时允许的浮点误差。</summary>
    private const double SlabEpsilon = 1e-9;

    private readonly double _rightX, _rightY, _rightZ;
    private readonly double _upX, _upY, _upZ;
    private readonly double _eyeX, _eyeY, _eyeZ;
    private readonly double _centerX, _centerY, _centerZ;
    private readonly double _scale;
    private readonly double _offsetX;
    private readonly double _offsetY;
    private readonly double _eyeDistance;
    private readonly bool _perspective;

    internal PlotProjection(
        double rightX, double rightY, double rightZ,
        double upX, double upY, double upZ,
        double eyeX, double eyeY, double eyeZ,
        double centerX, double centerY, double centerZ,
        double scale, double offsetX, double offsetY,
        double eyeDistance, bool perspective)
    {
        _rightX = rightX;
        _rightY = rightY;
        _rightZ = rightZ;
        _upX = upX;
        _upY = upY;
        _upZ = upZ;
        _eyeX = eyeX;
        _eyeY = eyeY;
        _eyeZ = eyeZ;
        _centerX = centerX;
        _centerY = centerY;
        _centerZ = centerZ;
        _scale = scale;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _eyeDistance = eyeDistance;
        _perspective = perspective;
    }

    /// <summary>是否为透视投影。</summary>
    public bool IsPerspective => _perspective;

    /// <summary>
    /// 把三维坐标投影为控件内的二维坐标（单位为设备无关像素）。
    /// 当点位于相机后方（透视投影）或坐标无效时返回 <c>false</c>。
    /// </summary>
    public bool TryProject(double x, double y, double z, out double screenX, out double screenY)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
        {
            screenX = double.NaN;
            screenY = double.NaN;
            return false;
        }

        var dx = x - _centerX;
        var dy = y - _centerY;
        var dz = z - _centerZ;

        var vx = dx * _rightX + dy * _rightY + dz * _rightZ;
        var vy = dx * _upX + dy * _upY + dz * _upZ;

        if (_perspective)
        {
            var vz = dx * _eyeX + dy * _eyeY + dz * _eyeZ;
            var depth = _eyeDistance - vz;
            if (depth < 1e-9)
            {
                screenX = double.NaN;
                screenY = double.NaN;
                return false;
            }

            var factor = _eyeDistance / depth;
            vx *= factor;
            vy *= factor;
        }

        screenX = _offsetX + _scale * vx;
        screenY = _offsetY - _scale * vy;
        return double.IsFinite(screenX) && double.IsFinite(screenY);
    }

    /// <summary>把三维点投影为控件内的二维点。</summary>
    public bool TryProject(Point3D point, out Point screen)
    {
        if (TryProject(point.X, point.Y, point.Z, out var x, out var y))
        {
            screen = new Point(x, y);
            return true;
        }

        screen = default;
        return false;
    }

    /// <summary>把三维点投影为控件内的二维点，失败时返回 NaN 点。</summary>
    public Point Project(Point3D point) =>
        TryProject(point.X, point.Y, point.Z, out var x, out var y) ? new Point(x, y) : new Point(double.NaN, double.NaN);

    /// <summary>
    /// 把屏幕坐标反算为三维坐标：结果位于“过包围盒中心且垂直于视线方向”的平面上。
    /// 该平面不保证与坐标框相交，十字准线请优先使用 <see cref="TryIntersectBounds"/>。
    /// </summary>
    /// <param name="screenX">屏幕 X 坐标（设备无关像素）。</param>
    /// <param name="screenY">屏幕 Y 坐标。</param>
    /// <param name="point">反算得到的三维坐标。</param>
    public bool TryUnproject(double screenX, double screenY, out Point3D point) =>
        TryGetViewPlanePoint(screenX, screenY, out point);

    /// <summary>把屏幕点反算为三维坐标，失败时返回 false。</summary>
    public bool TryUnproject(Point screenPoint, out Point3D point) =>
        TryUnproject(screenPoint.X, screenPoint.Y, out point);

    /// <summary>
    /// 由屏幕位置构造视线射线，并与轴对齐包围盒求交，返回最近的交点。
    /// 交点始终位于射线上，因此投影后与输入屏幕位置重合，不会出现逐轴裁剪导致的偏移。
    /// </summary>
    /// <param name="screenX">屏幕 X 坐标。</param>
    /// <param name="screenY">屏幕 Y 坐标。</param>
    /// <param name="minX">包围盒最小 X。</param>
    /// <param name="minY">包围盒最小 Y。</param>
    /// <param name="minZ">包围盒最小 Z。</param>
    /// <param name="maxX">包围盒最大 X。</param>
    /// <param name="maxY">包围盒最大 Y。</param>
    /// <param name="maxZ">包围盒最大 Z。</param>
    /// <param name="point">交点坐标。</param>
    /// <param name="offset">交点相对视图平面上对应点的空间偏移距离（用于判断射线是否几乎擦过包围盒）。</param>
    /// <returns>射线与包围盒相交时返回 true。</returns>
    public bool TryIntersectBounds(
        double screenX,
        double screenY,
        double minX,
        double minY,
        double minZ,
        double maxX,
        double maxY,
        double maxZ,
        out Point3D point,
        out double offset)
    {
        point = default;
        offset = double.PositiveInfinity;

        if (!TryGetViewPlanePoint(screenX, screenY, out var planePoint))
        {
            return false;
        }

        double originX, originY, originZ;
        double directionX, directionY, directionZ;

        if (_perspective)
        {
            // 透视：射线从相机位置射向视图平面上的对应点。
            originX = _centerX + _eyeX * _eyeDistance;
            originY = _centerY + _eyeY * _eyeDistance;
            originZ = _centerZ + _eyeZ * _eyeDistance;
            directionX = planePoint.X - originX;
            directionY = planePoint.Y - originY;
            directionZ = planePoint.Z - originZ;
        }
        else
        {
            // 正交：射线从视图平面上的点沿视线方向射入。
            originX = planePoint.X;
            originY = planePoint.Y;
            originZ = planePoint.Z;
            directionX = -_eyeX;
            directionY = -_eyeY;
            directionZ = -_eyeZ;
        }

        var tEnter = double.NegativeInfinity;
        var tExit = double.PositiveInfinity;

        if (!ClipSlab(originX, directionX, minX, maxX, ref tEnter, ref tExit) ||
            !ClipSlab(originY, directionY, minY, maxY, ref tEnter, ref tExit) ||
            !ClipSlab(originZ, directionZ, minZ, maxZ, ref tEnter, ref tExit) ||
            tEnter > tExit + SlabEpsilon ||
            (_perspective && tExit < -SlabEpsilon))
        {
            return false;
        }

        // 正交投影下视图平面上的点可能位于包围盒某一侧，
        // 此时取光标线上离该点最近的包围盒交点（允许 t 为负），保证准线仍严格落在光标射线上。
        var t = tEnter > SlabEpsilon
            ? tEnter
            : tExit < -SlabEpsilon
                ? tExit
                : 0;

        point = new Point3D(
            originX + directionX * t,
            originY + directionY * t,
            originZ + directionZ * t);

        var dx = point.X - planePoint.X;
        var dy = point.Y - planePoint.Y;
        var dz = point.Z - planePoint.Z;
        offset = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        return point.IsFinite && double.IsFinite(offset);
    }

    /// <inheritdoc />
    public bool Equals(PlotProjection other) =>
        _rightX.Equals(other._rightX) &&
        _rightY.Equals(other._rightY) &&
        _rightZ.Equals(other._rightZ) &&
        _upX.Equals(other._upX) &&
        _upY.Equals(other._upY) &&
        _upZ.Equals(other._upZ) &&
        _eyeX.Equals(other._eyeX) &&
        _eyeY.Equals(other._eyeY) &&
        _eyeZ.Equals(other._eyeZ) &&
        _centerX.Equals(other._centerX) &&
        _centerY.Equals(other._centerY) &&
        _centerZ.Equals(other._centerZ) &&
        _scale.Equals(other._scale) &&
        _offsetX.Equals(other._offsetX) &&
        _offsetY.Equals(other._offsetY) &&
        _eyeDistance.Equals(other._eyeDistance) &&
        _perspective == other._perspective;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PlotProjection other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_rightX);
        hash.Add(_rightY);
        hash.Add(_rightZ);
        hash.Add(_upX);
        hash.Add(_upY);
        hash.Add(_upZ);
        hash.Add(_eyeX);
        hash.Add(_eyeY);
        hash.Add(_eyeZ);
        hash.Add(_centerX);
        hash.Add(_centerY);
        hash.Add(_centerZ);
        hash.Add(_scale);
        hash.Add(_offsetX);
        hash.Add(_offsetY);
        hash.Add(_eyeDistance);
        hash.Add(_perspective);
        return hash.ToHashCode();
    }

    /// <summary>判断两个投影器是否完全一致。</summary>
    public static bool operator ==(PlotProjection left, PlotProjection right) => left.Equals(right);

    /// <summary>判断两个投影器是否不一致。</summary>
    public static bool operator !=(PlotProjection left, PlotProjection right) => !left.Equals(right);

    private bool TryGetViewPlanePoint(double screenX, double screenY, out Point3D point)
    {
        if (!double.IsFinite(screenX) || !double.IsFinite(screenY) || !(_scale > 1e-12))
        {
            point = default;
            return false;
        }

        var vx = (screenX - _offsetX) / _scale;
        var vy = (_offsetY - screenY) / _scale;

        // 视图平面（vz = 0）在透视投影下对应 depth = eyeDistance，缩放因子恰为 1，
        // 因此两种投影使用同一个反算公式。
        point = new Point3D(
            _centerX + _rightX * vx + _upX * vy,
            _centerY + _rightY * vx + _upY * vy,
            _centerZ + _rightZ * vx + _upZ * vy);

        return point.IsFinite;
    }

    private static bool ClipSlab(
        double origin,
        double direction,
        double min,
        double max,
        ref double tEnter,
        ref double tExit)
    {
        const double epsilon = 1e-12;

        if (Math.Abs(direction) < epsilon)
        {
            return origin >= min && origin <= max;
        }

        var inverse = 1.0 / direction;
        var t0 = (min - origin) * inverse;
        var t1 = (max - origin) * inverse;
        if (t0 > t1)
        {
            (t0, t1) = (t1, t0);
        }

        if (t0 > tEnter)
        {
            tEnter = t0;
        }

        if (t1 < tExit)
        {
            tExit = t1;
        }

        return tEnter <= tExit + SlabEpsilon;
    }
}
