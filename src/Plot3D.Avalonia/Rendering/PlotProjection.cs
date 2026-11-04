using Avalonia;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 三维到二维的投影器（正交或透视）。由 <see cref="Controls.Plot3DView"/> 在每次布局变化时创建，
/// 并传递给每个 <see cref="PlotSeries3D"/> 用于绘制。
/// </summary>
public readonly struct PlotProjection
{
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
}
