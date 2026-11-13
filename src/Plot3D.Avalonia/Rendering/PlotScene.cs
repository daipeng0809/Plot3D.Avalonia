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

    /// <summary>远离相机的 X 墙面四个角点索引（配合 <see cref="ScreenCorners" /> 填充面板）。</summary>
    public required (int A, int B, int C, int D) XFace { get; init; }

    /// <summary>远离相机的 Y 墙面四个角点索引。</summary>
    public required (int A, int B, int C, int D) YFace { get; init; }

    /// <summary>远离相机的 Z 墙面四个角点索引。</summary>
    public required (int A, int B, int C, int D) ZFace { get; init; }

    /// <summary>包围盒中心（世界坐标）。</summary>
    public required Point3D Center { get; init; }

    /// <summary>包围盒对角线长度（真实数据单位）。</summary>
    public required double Diagonal { get; init; }

    /// <summary>归一化空间中的包围盒对角线长度（等长轴锁定时为正方体的对角线）。</summary>
    public required double VisualDiagonal { get; init; }
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
        double xLeft,
        double xRight,
        double yLeft,
        double yRight,
        double zBottom,
        double zTop,
        int tickCount,
        int xTickDecimals,
        int yTickDecimals,
        int zTickDecimals,
        AxisValueFormat xFormat,
        AxisValueFormat yFormat,
        AxisValueFormat zFormat,
        bool axisTimeMilliseconds,
        double azimuth,
        double elevation,
        double zoom,
        double panX,
        double panY,
        bool perspective,
        bool aspectLocked)
    {
        var hasData = !dataBounds.IsEmpty;
        var (dataMinX, dataMaxX) = hasData ? (dataBounds.MinX, dataBounds.MaxX) : (0.0, 1.0);
        var (dataMinY, dataMaxY) = hasData ? (dataBounds.MinY, dataBounds.MaxY) : (0.0, 1.0);
        var (dataMinZ, dataMaxZ) = hasData ? (dataBounds.MinZ, dataBounds.MaxZ) : (0.0, 1.0);

        // “左 / 右（下 / 上）”是用户视角下的轴两端：X / Z 默认左（下）端为较小值；
        // Y 轴在默认视角下自右下方向左上方延伸，因此左端默认显示较大值。
        var xRange = ResolveAxis(xLeft, xRight, dataMinX, dataMaxX, hasData, GetMinimumSpan(xFormat), defaultLeftIsLow: true);
        var yRange = ResolveAxis(yLeft, yRight, dataMinY, dataMaxY, hasData, GetMinimumSpan(yFormat), defaultLeftIsLow: false);
        var zRange = ResolveAxis(zBottom, zTop, dataMinZ, dataMaxZ, hasData, GetMinimumSpan(zFormat), defaultLeftIsLow: true);

        var xTicks = AxisTicks.Create(xRange.Lo, xRange.Hi, tickCount, xRange.FixedLo, xRange.FixedHi, xFormat, axisTimeMilliseconds, xTickDecimals);
        var yTicks = AxisTicks.Create(yRange.Lo, yRange.Hi, tickCount, yRange.FixedLo, yRange.FixedHi, yFormat, axisTimeMilliseconds, yTickDecimals);
        var zTicks = AxisTicks.Create(zRange.Lo, zRange.Hi, tickCount, zRange.FixedLo, zRange.FixedHi, zFormat, axisTimeMilliseconds, zTickDecimals);

        var xMin = xTicks.Min;
        var xMax = xTicks.Max;
        var yMin = yTicks.Min;
        var yMax = yTicks.Max;
        var zMin = zTicks.Min;
        var zMax = zTicks.Max;

        // 反向轴的刻度仍按数值递升生成，但投影时沿该轴取负，使两端数值与屏幕左右 / 上下对应。
        var xReversed = xRange.Reversed;
        var yReversed = yRange.Reversed;
        var zReversed = zRange.Reversed;

        var center = new Point3D((xMin + xMax) * 0.5, (yMin + yMax) * 0.5, (zMin + zMax) * 0.5);
        var spanX = xMax - xMin;
        var spanY = yMax - yMin;
        var spanZ = zMax - zMin;

        // 等长轴锁定：把三个轴分别归一到相同长度（1），坐标框因此显示为正方体，
        // 各轴刻度仍按真实数值排列；关闭时系数为 ±1，保持真实比例。
        // 负系数表示该轴反向：数值方向与屏幕方向相反，刻度自左向右（或自下向上）递减。
        var axisScaleX = (xReversed ? -1.0 : 1.0) * (aspectLocked ? GetNormalizeScale(spanX) : 1.0);
        var axisScaleY = (yReversed ? -1.0 : 1.0) * (aspectLocked ? GetNormalizeScale(spanY) : 1.0);
        var axisScaleZ = (zReversed ? -1.0 : 1.0) * (aspectLocked ? GetNormalizeScale(spanZ) : 1.0);

        var diagonal = Math.Sqrt(
            spanX * spanX +
            spanY * spanY +
            spanZ * spanZ);
        if (!(diagonal > 1e-9))
        {
            diagonal = 1;
        }

        var visualSpanX = spanX * axisScaleX;
        var visualSpanY = spanY * axisScaleY;
        var visualSpanZ = spanZ * axisScaleZ;

        var visualDiagonal = Math.Sqrt(
            visualSpanX * visualSpanX +
            visualSpanY * visualSpanY +
            visualSpanZ * visualSpanZ);
        if (!(visualDiagonal > 1e-9))
        {
            visualDiagonal = 1;
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

        var eyeDistance = perspective ? Math.Max(visualDiagonal * 2.5, 1e-6) : 1.0;

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
            axisScaleX, axisScaleY, axisScaleZ,
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

        // centerVy 是 measure 投影输出的屏幕 Y 中心（屏幕 Y 与世界 Y 方向相反），
        // 因此这里必须减去，否则透视投影下包围盒会整体偏移。
        var offsetY = padding.Top + availableHeight * 0.5 - scale * centerVy + (double.IsFinite(panY) ? panY : 0);

        var projection = new PlotProjection(
            rightX, rightY, rightZ,
            upX, upY, upZ,
            eyeX, eyeY, eyeZ,
            center.X, center.Y, center.Z,
            axisScaleX, axisScaleY, axisScaleZ,
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

        // 反向轴会沿该轴翻转投影，相机在正侧时远墙可能落在数值较大的一端。
        var farX = (eyeX >= 0) == !xReversed ? xMin : xMax;
        var farY = (eyeY >= 0) == !yReversed ? yMin : yMax;
        var farZ = (eyeZ >= 0) == !zReversed ? zMin : zMax;

        // 面板角点按远墙所在的数据端选择：bit = 0 的角点取各轴的较小值。
        var xFace = NearEqual(farX, xMin) ? (A: 0, B: 1, C: 3, D: 2) : (A: 4, B: 5, C: 7, D: 6);
        var yFace = NearEqual(farY, yMin) ? (A: 0, B: 1, C: 5, D: 4) : (A: 2, B: 3, C: 7, D: 6);
        var zFace = NearEqual(farZ, zMin) ? (A: 0, B: 2, C: 6, D: 4) : (A: 1, B: 3, C: 7, D: 5);

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
            FarX = farX,
            FarY = farY,
            FarZ = farZ,
            XFace = xFace,
            YFace = yFace,
            ZFace = zFace,
            Center = center,
            Diagonal = diagonal,
            VisualDiagonal = visualDiagonal,
        };
    }

    /// <summary>
    /// 时间格式轴在零跨度（单点 / 所有值相同）时的最小可见跨度：1 天。
    /// 数值格式返回 0，表示沿用按数值量级扩展的旧行为。
    /// </summary>
    private static double GetMinimumSpan(AxisValueFormat format) =>
        format == AxisValueFormat.Numeric ? 0 : 1.0;

    /// <summary>等长轴锁定时的归一化系数：把该轴映射到长度 1。</summary>
    private static double GetNormalizeScale(double span) =>
        double.IsFinite(span) && span > 1e-12 ? 1.0 / span : 1.0;

    /// <summary>单轴解析结果：递升的数值范围、两端是否由用户固定，以及该轴是否反向显示。</summary>
    private readonly record struct AxisExtent(double Lo, double Hi, bool FixedLo, bool FixedHi, bool Reversed);

    /// <summary>
    /// 把用户设置的轴两端（左 / 右或下 / 上）解析为递升的数值范围，并保证范围覆盖全部数据。
    /// </summary>
    /// <param name="left">左端（Z 轴为底端）数值，<c>NaN</c> 表示自动。</param>
    /// <param name="right">右端（Z 轴为顶端）数值，<c>NaN</c> 表示自动。</param>
    /// <param name="dataLo">数据在该轴上的最小值。</param>
    /// <param name="dataHi">数据在该轴上的最大值。</param>
    /// <param name="hasData">是否存在有效数据。</param>
    /// <param name="minimumSpan">零跨度时使用的最小可见跨度，0 表示按数值量级扩展。</param>
    /// <param name="defaultLeftIsLow">两端都自动时，左端是否默认为数值较小的一端。</param>
    private static AxisExtent ResolveAxis(
        double left,
        double right,
        double dataLo,
        double dataHi,
        bool hasData,
        double minimumSpan,
        bool defaultLeftIsLow)
    {
        var leftFixed = double.IsFinite(left);
        var rightFixed = double.IsFinite(right);

        double leftValue;
        double rightValue;

        if (leftFixed && rightFixed)
        {
            leftValue = left;
            rightValue = right;
        }
        else if (leftFixed)
        {
            leftValue = left;
            rightValue = ResolveOpenRight(left, dataLo, dataHi, hasData, defaultLeftIsLow);
        }
        else if (rightFixed)
        {
            rightValue = right;
            leftValue = ResolveOpenLeft(right, dataLo, dataHi, hasData, defaultLeftIsLow);
        }
        else if (defaultLeftIsLow)
        {
            leftValue = dataLo;
            rightValue = dataHi;
        }
        else
        {
            leftValue = dataHi;
            rightValue = dataLo;
        }

        if (!double.IsFinite(leftValue) || !double.IsFinite(rightValue))
        {
            leftValue = 0;
            rightValue = 1;
            leftFixed = false;
            rightFixed = false;
        }

        // 左端大于右端（Y 轴默认相反）即反向坐标轴：刻度仍按数值递升生成，投影时沿该轴取负。
        var reversed = defaultLeftIsLow ? leftValue > rightValue : leftValue < rightValue;
        var leftIsLow = leftValue <= rightValue;
        var lo = leftIsLow ? leftValue : rightValue;
        var hi = leftIsLow ? rightValue : leftValue;
        var fixedLo = leftIsLow ? leftFixed : rightFixed;
        var fixedHi = leftIsLow ? rightFixed : leftFixed;

        // 数据超出用户指定范围时自动延伸该端，并把延伸端交给刻度取整，避免图形跑出坐标框。
        if (hasData)
        {
            if (dataLo < lo)
            {
                lo = dataLo;
                fixedLo = false;
            }

            if (dataHi > hi)
            {
                hi = dataHi;
                fixedHi = false;
            }
        }

        if (!(hi > lo))
        {
            var span = minimumSpan > 0 ? minimumSpan : Math.Max(Math.Abs(hi), 1.0);
            if (fixedLo)
            {
                hi = lo + span;
            }
            else if (fixedHi)
            {
                lo = hi - span;
            }
            else
            {
                lo -= span * 0.5;
                hi += span * 0.5;
            }
        }

        return new AxisExtent(lo, hi, fixedLo, fixedHi, reversed);
    }

    /// <summary>
    /// 求解自动的右端：保证数据落在 [右端, 左端] 或 [左端, 右端] 之间，且不强制反向。
    /// </summary>
    private static double ResolveOpenRight(double left, double dataLo, double dataHi, bool hasData, bool defaultLeftIsLow)
    {
        if (!hasData)
        {
            return left + (defaultLeftIsLow ? 1 : -1);
        }

        // 左端已越过数据上界时，把右端放在数据下界，形成反向轴并仍在同侧显示全部数据。
        return left >= dataHi ? dataLo : dataHi;
    }

    /// <summary>求解自动的左端，规则与 <see cref="ResolveOpenRight" /> 对称。</summary>
    private static double ResolveOpenLeft(double right, double dataLo, double dataHi, bool hasData, bool defaultLeftIsLow)
    {
        if (!hasData)
        {
            return right + (defaultLeftIsLow ? -1 : 1);
        }

        return right <= dataLo ? dataHi : dataLo;
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

    /// <summary>比较两个坐标值是否相同（允许极小的浮点误差）。</summary>
    private static bool NearEqual(double a, double b)
    {
        if (a.Equals(b))
        {
            return true;
        }

        var scale = Math.Max(Math.Abs(a), Math.Abs(b));
        return Math.Abs(a - b) <= Math.Max(1e-12, scale * 1e-12);
    }

    private static double DistanceSquared(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
