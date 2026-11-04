using Avalonia;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 坐标轴所在的一条棱，包含世界坐标、屏幕坐标以及屏幕空间中的“向外”方向。
/// </summary>
internal readonly struct AxisEdge
{
    public AxisEdge(Point3D start, Point3D end, Point screenStart, Point screenEnd, Point outward)
    {
        Start = start;
        End = end;
        ScreenStart = screenStart;
        ScreenEnd = screenEnd;
        Outward = outward;
    }

    /// <summary>棱的世界坐标起点。</summary>
    public Point3D Start { get; }

    /// <summary>棱的世界坐标终点。</summary>
    public Point3D End { get; }

    /// <summary>棱的屏幕坐标起点。</summary>
    public Point ScreenStart { get; }

    /// <summary>棱的屏幕坐标终点。</summary>
    public Point ScreenEnd { get; }

    /// <summary>屏幕空间中垂直于棱、指向图形外侧的单位向量。</summary>
    public Point Outward { get; }

    /// <summary>屏幕长度。</summary>
    public double ScreenLength
    {
        get
        {
            var dx = ScreenEnd.X - ScreenStart.X;
            var dy = ScreenEnd.Y - ScreenStart.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

/// <summary>
/// 一次渲染所需的全部几何信息：取整后的轴范围、刻度、投影器、角点与轴棱。
/// 只有相机、尺寸、数据或相关属性变化时才会重建。
/// </summary>
internal sealed class PlotScene
{
    public required Size ViewSize { get; init; }

    public required AxisTicks XTicks { get; init; }

    public required AxisTicks YTicks { get; init; }

    public required AxisTicks ZTicks { get; init; }

    public required PlotProjection Projection { get; init; }

    /// <summary>8 个包围盒角点的世界坐标，索引位定义：bit0 = Z, bit1 = Y, bit2 = X。</summary>
    public required Point3D[] Corners { get; init; }

    /// <summary>8 个包围盒角点的屏幕坐标。</summary>
    public required Point[] ScreenCorners { get; init; }

    public required AxisEdge XEdge { get; init; }

    public required AxisEdge YEdge { get; init; }

    public required AxisEdge ZEdge { get; init; }

    /// <summary>远离相机的 X 墙面位置（用于绘制背景网格）。</summary>
    public required double FarX { get; init; }

    /// <summary>远离相机的 Y 墙面位置。</summary>
    public required double FarY { get; init; }

    /// <summary>远离相机的 Z 墙面位置。</summary>
    public required double FarZ { get; init; }

    /// <summary>包围盒中心（世界坐标）。</summary>
    public required Point3D Center { get; init; }

    /// <summary>包围盒对角线长度。</summary>
    public required double Diagonal { get; init; }
}

/// <summary>
/// 根据数据范围、相机参数和控件尺寸构建 <see cref="PlotScene"/>。
/// </summary>
internal static class PlotSceneBuilder
{
    public static PlotScene Build(
        Size size,
        Thickness padding,
        BoundingBox3D dataBounds,
        double xMinOverride,
        double xMaxOverride,
        double yMinOverride,
        double yMaxOverride,
        double zMinOverride,
        double zMaxOverride,
        int tickCount,
        double azimuth,
        double elevation,
        double zoom,
        double panX,
        double panY,
        bool perspective)
    {
        var (dataMinX, dataMaxX) = dataBounds.IsEmpty ? (0.0, 1.0) : (dataBounds.MinX, dataBounds.MaxX);
        var (dataMinY, dataMaxY) = dataBounds.IsEmpty ? (0.0, 1.0) : (dataBounds.MinY, dataBounds.MaxY);
        var (dataMinZ, dataMaxZ) = dataBounds.IsEmpty ? (0.0, 1.0) : (dataBounds.MinZ, dataBounds.MaxZ);

        var (xLo, xHi, xFixedLo, xFixedHi) = ResolveAxis(dataMinX, dataMaxX, xMinOverride, xMaxOverride);
        var (yLo, yHi, yFixedLo, yFixedHi) = ResolveAxis(dataMinY, dataMaxY, yMinOverride, yMaxOverride);
        var (zLo, zHi, zFixedLo, zFixedHi) = ResolveAxis(dataMinZ, dataMaxZ, zMinOverride, zMaxOverride);

        var xTicks = AxisTicks.Create(xLo, xHi, tickCount, xFixedLo, xFixedHi);
        var yTicks = AxisTicks.Create(yLo, yHi, tickCount, yFixedLo, yFixedHi);
        var zTicks = AxisTicks.Create(zLo, zHi, tickCount, zFixedLo, zFixedHi);

        var xMin = xTicks.Min;
        var xMax = xTicks.Max;
        var yMin = yTicks.Min;
        var yMax = yTicks.Max;
        var zMin = zTicks.Min;
        var zMax = zTicks.Max;

        var center = new Point3D((xMin + xMax) * 0.5, (yMin + yMax) * 0.5, (zMin + zMax) * 0.5);
        var diagonal = Math.Sqrt(
            (xMax - xMin) * (xMax - xMin) +
            (yMax - yMin) * (yMax - yMin) +
            (zMax - zMin) * (zMax - zMin));
        if (!(diagonal > 1e-9))
        {
            diagonal = 1;
        }

        // 相机基向量：toEye 由中心指向相机，right / up 构成屏幕坐标系。
        // 方位角语义与主流科学计算软件的 view(az, el) 一致：az = 0 时相机位于 -Y 方向，正值绕 Z 轴逆时针旋转。
        var azimuthRad = azimuth * Math.PI / 180.0;
        var elevationRad = Math.Clamp(elevation, -89.9, 89.9) * Math.PI / 180.0;
        var eyeX = Math.Cos(elevationRad) * Math.Sin(azimuthRad);
        var eyeY = -Math.Cos(elevationRad) * Math.Cos(azimuthRad);
        var eyeZ = Math.Sin(elevationRad);

        var horizontal = Math.Sqrt(eyeX * eyeX + eyeY * eyeY);
        if (horizontal < 1e-9)
        {
            horizontal = 1e-9;
        }

        var rightX = -eyeY / horizontal;
        var rightY = eyeX / horizontal;
        const double rightZ = 0.0;

        var upX = -rightY * eyeZ;
        var upY = rightX * eyeZ;
        var upZ = -rightX * eyeY + rightY * eyeX;

        var eyeDistance = perspective ? Math.Max(diagonal * 2.5, 1e-6) : 1.0;

        var corners = new Point3D[8];
        for (var i = 0; i < 8; i++)
        {
            corners[i] = new Point3D(
                (i & 4) != 0 ? xMax : xMin,
                (i & 2) != 0 ? yMax : yMin,
                (i & 1) != 0 ? zMax : zMin);
        }

        // 先用 scale = 1 / offset = 0 的投影器测量投影范围，再计算居中的缩放。
        var measure = new PlotProjection(
            rightX, rightY, rightZ,
            upX, upY, upZ,
            eyeX, eyeY, eyeZ,
            center.X, center.Y, center.Z,
            1, 0, 0,
            eyeDistance, perspective);

        var minVx = double.MaxValue;
        var maxVx = double.MinValue;
        var minVy = double.MaxValue;
        var maxVy = double.MinValue;

        foreach (var corner in corners)
        {
            if (!measure.TryProject(corner.X, corner.Y, corner.Z, out var vx, out var vy))
            {
                continue;
            }

            minVx = Math.Min(minVx, vx);
            maxVx = Math.Max(maxVx, vx);
            minVy = Math.Min(minVy, vy);
            maxVy = Math.Max(maxVy, vy);
        }

        if (minVx > maxVx || minVy > maxVy)
        {
            minVx = -1;
            maxVx = 1;
            minVy = -1;
            maxVy = 1;
        }

        var rangeX = Math.Max(maxVx - minVx, 1e-9);
        var rangeY = Math.Max(maxVy - minVy, 1e-9);

        var availableWidth = Math.Max(8, size.Width - padding.Left - padding.Right);
        var availableHeight = Math.Max(8, size.Height - padding.Top - padding.Bottom);

        var baseScale = Math.Min(availableWidth / rangeX, availableHeight / rangeY);
        var scale = baseScale * Math.Clamp(double.IsFinite(zoom) ? zoom : 1, 0.02, 100);

        var centerVx = (minVx + maxVx) * 0.5;
        var centerVy = (minVy + maxVy) * 0.5;

        var offsetX = padding.Left + availableWidth * 0.5 - scale * centerVx + (double.IsFinite(panX) ? panX : 0);
        var offsetY = padding.Top + availableHeight * 0.5 + scale * centerVy + (double.IsFinite(panY) ? panY : 0);

        var projection = new PlotProjection(
            rightX, rightY, rightZ,
            upX, upY, upZ,
            eyeX, eyeY, eyeZ,
            center.X, center.Y, center.Z,
            scale, offsetX, offsetY,
            eyeDistance, perspective);

        var screenCorners = new Point[8];
        for (var i = 0; i < 8; i++)
        {
            projection.TryProject(corners[i].X, corners[i].Y, corners[i].Z, out var sx, out var sy);
            screenCorners[i] = new Point(sx, sy);
        }

        var screenCenter = new Point(size.Width * 0.5, size.Height * 0.5);

        var xEdge = BuildEdge(corners, screenCorners, PickBottomEdge(screenCorners, (0, 4), (2, 6)), screenCenter);
        var yEdge = BuildEdge(corners, screenCorners, PickBottomEdge(screenCorners, (0, 2), (4, 6)), screenCenter);
        var zEdge = BuildEdge(corners, screenCorners, PickLeftVerticalEdge(screenCorners), screenCenter);

        return new PlotScene
        {
            ViewSize = size,
            XTicks = xTicks,
            YTicks = yTicks,
            ZTicks = zTicks,
            Projection = projection,
            Corners = corners,
            ScreenCorners = screenCorners,
            XEdge = xEdge,
            YEdge = yEdge,
            ZEdge = zEdge,
            FarX = eyeX >= 0 ? xMin : xMax,
            FarY = eyeY >= 0 ? yMin : yMax,
            FarZ = eyeZ >= 0 ? zMin : zMax,
            Center = center,
            Diagonal = diagonal,
        };
    }

    private static (double Min, double Max, bool FixedMin, bool FixedMax) ResolveAxis(
        double dataMin,
        double dataMax,
        double overrideMin,
        double overrideMax)
    {
        var fixedMin = double.IsFinite(overrideMin);
        var fixedMax = double.IsFinite(overrideMax);
        var min = fixedMin ? overrideMin : dataMin;
        var max = fixedMax ? overrideMax : dataMax;

        if (!double.IsFinite(min) || !double.IsFinite(max))
        {
            return (0, 1, false, false);
        }

        if (max <= min)
        {
            var span = Math.Max(Math.Abs(max), 1.0);
            if (fixedMin && fixedMax)
            {
                max = min + span;
            }
            else if (fixedMin)
            {
                max = min + span;
            }
            else if (fixedMax)
            {
                min = max - span;
            }
            else
            {
                min -= span * 0.5;
                max += span * 0.5;
            }
        }

        return (min, max, fixedMin, fixedMax);
    }

    /// <summary>
    /// 在底部平面上选择屏幕上最靠下的一条棱，作为刻度边。
    /// </summary>
    private static (int A, int B) PickBottomEdge(Point[] screenCorners, (int A, int B) first, (int A, int B) second)
    {
        var firstY = (screenCorners[first.A].Y + screenCorners[first.B].Y) * 0.5;
        var secondY = (screenCorners[second.A].Y + screenCorners[second.B].Y) * 0.5;
        return firstY >= secondY ? first : second;
    }

    /// <summary>
    /// 选择屏幕上最靠左的竖直棱作为 Z 轴。
    /// </summary>
    private static (int A, int B) PickLeftVerticalEdge(Point[] screenCorners)
    {
        Span<int> starts = [0, 2, 4, 6];
        var best = 0;
        var bestX = double.MaxValue;

        foreach (var start in starts)
        {
            var midX = (screenCorners[start].X + screenCorners[start + 1].X) * 0.5;
            if (midX < bestX)
            {
                bestX = midX;
                best = start;
            }
        }

        return (best, best + 1);
    }

    private static AxisEdge BuildEdge(Point3D[] corners, Point[] screenCorners, (int A, int B) indices, Point screenCenter)
    {
        var a = indices.A;
        var b = indices.B;
        var dx = screenCorners[b].X - screenCorners[a].X;
        var dy = screenCorners[b].Y - screenCorners[a].Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        Point outward;

        if (length < 1e-6)
        {
            outward = new Point(0, 1);
        }
        else
        {
            dx /= length;
            dy /= length;
            var n1 = new Point(-dy, dx);
            var n2 = new Point(dy, -dx);
            var mid = new Point(
                (screenCorners[a].X + screenCorners[b].X) * 0.5,
                (screenCorners[a].Y + screenCorners[b].Y) * 0.5);

            var d1 = DistanceSquared(mid + n1 * 24, screenCenter);
            var d2 = DistanceSquared(mid + n2 * 24, screenCenter);
            outward = d1 >= d2 ? n1 : n2;
        }

        return new AxisEdge(corners[a], corners[b], screenCorners[a], screenCorners[b], outward);
    }

    private static double DistanceSquared(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
