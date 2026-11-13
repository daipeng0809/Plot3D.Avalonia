using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 负责绘制坐标框、背景面板、网格线、刻度与轴标题。
/// </summary>
internal sealed class FrameRenderer
{
    /// <summary>标签避让时沿轴向外方向最多尝试的次数。</summary>
    private const int MaxLabelNudgeAttempts = 6;

    /// <summary>图例与文字之间保持的最小间隙（像素），避让、粘滞与搜索使用同一阈值。</summary>
    private const double LegendClearance = 2;

    /// <summary>网格搜索时额外偏好的留白（像素），用于选择更宽阔的停放位置。</summary>
    private const double LegendPreferredClearance = 6;

    /// <summary>障碍消失后，图例每帧向配置位置回移的最大步长（像素）。</summary>
    private const double LegendReturnStep = 6;

    /// <summary>有重叠候选的评分惩罚，保证“无重叠”的候选始终优于“有重叠”的候选。</summary>
    private const double OverlapPenalty = 1_000_000_000_000;

    private readonly Plot3DView _owner;
    private readonly Dictionary<(string Text, double FontSize), TextLayout> _textCache = new();
    private readonly List<TickLabelPlacement> _tickLabels = new();
    private readonly List<(Rect Rect, int Axis)> _placedLabels = new();
    private readonly List<Rect> _textRects = new();
    private FontFamily _cachedFamily = FontFamily.Default;
    private IBrush? _cachedTextBrush;

    // 图例避让的粘滞状态：上一帧的位置在当前帧仍可用时保持不变，避免旋转 / 缩放时抖动。
    private Rect? _lastLegendBox;
    private LegendPosition _lastLegendPosition;
    private Vector _lastLegendOffset;
    private bool _lastLegendAvoidance;
    private Size _legendViewSize;
    private Vector _legendPushDirection;
    private PlotScene? _legendLastScene;
    private int _legendSceneStableFrames;
    private bool _legendReturnPending;
    private bool _legendStepFailed;
    private bool _legendReturnArmed;

    /// <summary>最近一次绘制图例时实际使用的外框（含自动避让结果），供诊断使用。</summary>
    internal Rect LastLegendBounds { get; private set; }

    /// <summary>
    /// 开启一次“障碍消失后回位”的尝试。由控件在拖动 / 缩放等交互结束时调用，
    /// 下一次同一场景的渲染开始逐步回到配置位置；场景再次变化会自动解除。
    /// </summary>
    internal void ArmLegendReturn() => _legendReturnArmed = true;

    /// <summary>待绘制的刻度标签：布局、基准矩形、轴的向外方向与轴索引（0=X、1=Y、2=Z）。</summary>
    private readonly record struct TickLabelPlacement(TextLayout Layout, Rect Bounds, Point Outward, int Axis);

    public FrameRenderer(Plot3DView owner)
    {
        _owner = owner;
    }

    /// <summary>清除文字布局缓存（字体或前景色变化时调用）。</summary>
    public void ResetCache() => _textCache.Clear();

    public void Render(DrawingContext dc, PlotScene scene)
    {
        var axisBrush = _owner.ResolveAxisBrush();
        var gridBrush = _owner.ResolveGridBrush();

        _textRects.Clear();
        DrawPanes(dc, scene);
        DrawGrid(dc, scene, gridBrush);
        DrawBox(dc, scene, axisBrush);

        if (_owner.ShowTicks)
        {
            DrawTicks(dc, scene, axisBrush, _owner.ResolveTextBrush());
        }
    }

    /// <summary>
    /// 在数据系列之后绘制刻度文本与轴标题，避免曲线遮挡文字。
    /// 刻度线本身仍在数据之前绘制。
    /// </summary>
    public void RenderLabels(DrawingContext dc, PlotScene scene)
    {
        if (_owner.ShowTicks)
        {
            DrawTickLabels(dc, scene);
        }

        if (_owner.ShowAxisTitles)
        {
            DrawAxisTitles(dc, scene, _owner.ResolveTextBrush());
        }
    }

    private void DrawPanes(DrawingContext dc, PlotScene scene)
    {
        if (!_owner.ShowPane)
        {
            return;
        }

        // 背景完全透明时，只有在宿主显式设置了 PaneBrush 的情况下才填充墙面，
        // 避免叠放在其它控件上时出现不透明色块。
        if (_owner.IsBackgroundTransparent && !_owner.IsSet(Plot3DView.PaneBrushProperty))
        {
            return;
        }

        var brush = _owner.ResolvePaneBrush();
        FillQuad(dc, scene, brush, scene.XFace);
        FillQuad(dc, scene, brush, scene.YFace);
        FillQuad(dc, scene, brush, scene.ZFace);
    }

    private static void FillQuad(DrawingContext dc, PlotScene scene, IBrush brush, (int A, int B, int C, int D) face)
    {
        var corners = scene.ScreenCorners;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(corners[face.A], true);
            g.LineTo(corners[face.B]);
            g.LineTo(corners[face.C]);
            g.LineTo(corners[face.D]);
            g.EndFigure(true);
        }

        dc.DrawGeometry(brush, null, geometry);
    }

    private void DrawGrid(DrawingContext dc, PlotScene scene, IBrush brush)
    {
        if (!_owner.ShowGrid)
        {
            return;
        }

        var xMin = scene.XTicks.Min;
        var xMax = scene.XTicks.Max;
        var yMin = scene.YTicks.Min;
        var yMax = scene.YTicks.Max;
        var zMin = scene.ZTicks.Min;
        var zMax = scene.ZTicks.Max;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            foreach (var tick in scene.YTicks.Values)
            {
                if (!IsBoundary(tick, yMin, yMax))
                {
                    AddLine(g, scene, scene.FarX, tick, zMin, scene.FarX, tick, zMax);
                }
            }

            foreach (var tick in scene.ZTicks.Values)
            {
                if (!IsBoundary(tick, zMin, zMax))
                {
                    AddLine(g, scene, scene.FarX, yMin, tick, scene.FarX, yMax, tick);
                }
            }

            foreach (var tick in scene.XTicks.Values)
            {
                if (!IsBoundary(tick, xMin, xMax))
                {
                    AddLine(g, scene, tick, scene.FarY, zMin, tick, scene.FarY, zMax);
                }
            }

            foreach (var tick in scene.ZTicks.Values)
            {
                if (!IsBoundary(tick, zMin, zMax))
                {
                    AddLine(g, scene, xMin, scene.FarY, tick, xMax, scene.FarY, tick);
                }
            }

            foreach (var tick in scene.XTicks.Values)
            {
                if (!IsBoundary(tick, xMin, xMax))
                {
                    AddLine(g, scene, tick, yMin, scene.FarZ, tick, yMax, scene.FarZ);
                }
            }

            foreach (var tick in scene.YTicks.Values)
            {
                if (!IsBoundary(tick, yMin, yMax))
                {
                    AddLine(g, scene, xMin, tick, scene.FarZ, xMax, tick, scene.FarZ);
                }
            }
        }

        dc.DrawGeometry(null, new Pen(brush, 1), geometry);
    }

    private static void AddLine(
        StreamGeometryContext g,
        PlotScene scene,
        double x1, double y1, double z1,
        double x2, double y2, double z2)
    {
        if (!scene.Projection.TryProject(x1, y1, z1, out var ax, out var ay) ||
            !scene.Projection.TryProject(x2, y2, z2, out var bx, out var by))
        {
            return;
        }

        g.BeginFigure(new Point(ax, ay), false);
        g.LineTo(new Point(bx, by));
        g.EndFigure(false);
    }

    private void DrawBox(DrawingContext dc, PlotScene scene, IBrush brush)
    {
        if (!_owner.ShowBox)
        {
            return;
        }

        var pen = new Pen(brush, 1);
        var corners = scene.ScreenCorners;

        // 沿 X 的 4 条棱
        dc.DrawLine(pen, corners[0], corners[4]);
        dc.DrawLine(pen, corners[1], corners[5]);
        dc.DrawLine(pen, corners[2], corners[6]);
        dc.DrawLine(pen, corners[3], corners[7]);

        // 沿 Y 的 4 条棱
        dc.DrawLine(pen, corners[0], corners[2]);
        dc.DrawLine(pen, corners[1], corners[3]);
        dc.DrawLine(pen, corners[4], corners[6]);
        dc.DrawLine(pen, corners[5], corners[7]);

        // 沿 Z 的 4 条棱
        dc.DrawLine(pen, corners[0], corners[1]);
        dc.DrawLine(pen, corners[2], corners[3]);
        dc.DrawLine(pen, corners[4], corners[5]);
        dc.DrawLine(pen, corners[6], corners[7]);
    }

    private void DrawTicks(DrawingContext dc, PlotScene scene, IBrush axisBrush, IBrush textBrush)
    {
        var pen = new Pen(axisBrush, 1);
        _tickLabels.Clear();

        // 先收集三个轴的刻度与标签位置，再统一处理标签重叠（标签本身在数据之后绘制）。
        DrawAxisTicks(dc, scene, scene.ZEdge, scene.ZTicks, scene.ZEdge.Start.Z, scene.ZEdge.End.Z, pen, textBrush, axis: 2);
        DrawAxisTicks(dc, scene, scene.XEdge, scene.XTicks, scene.XEdge.Start.X, scene.XEdge.End.X, pen, textBrush, axis: 0);
        DrawAxisTicks(dc, scene, scene.YEdge, scene.YTicks, scene.YEdge.Start.Y, scene.YEdge.End.Y, pen, textBrush, axis: 1);
    }

    private void DrawAxisTicks(
        DrawingContext dc,
        PlotScene scene,
        AxisEdge edge,
        AxisTicks ticks,
        double startValue,
        double endValue,
        Pen pen,
        IBrush textBrush,
        int axis)
    {
        // 沿棱的实际轴端数值插值：反向轴（startValue > endValue）时 t 自动反向，
        // 刻度仍按数值递升生成，但在屏幕上按反向顺序显示。
        var span = endValue - startValue;
        if (!(Math.Abs(span) > 1e-12))
        {
            return;
        }

        var tickLength = SanitizeTickLength(_owner.TickLength);
        var labelGap = SanitizeGap(_owner.TickLabelGap);
        var labelFontSize = _owner.ResolveTickLabelFontSize();
        var labelOffset = _owner.TickLabelOffset;

        for (var i = 0; i < ticks.Values.Length; i++)
        {
            var t = Math.Clamp((ticks.Values[i] - startValue) / span, 0, 1);
            var world = Point3D.Lerp(edge.Start, edge.End, t);
            if (!scene.Projection.TryProject(world, out var point))
            {
                continue;
            }

            var tip = point + edge.Outward * tickLength;
            dc.DrawLine(pen, point, tip);

            if (!_owner.ShowTickLabels)
            {
                continue;
            }

            var layout = GetText(ticks.Labels[i], textBrush, labelFontSize);
            if (axis == 2 && Math.Abs(layout.Width) < 0.01)
            {
                continue;
            }

            var origin = ComputeLabelOrigin(tip, edge.Outward, layout, labelGap, labelOffset);
            _tickLabels.Add(new TickLabelPlacement(
                layout,
                new Rect(origin, new Size(layout.Width, layout.Height)),
                edge.Outward,
                axis));
        }
    }

    /// <summary>
    /// 绘制刻度标签并避免文字重叠：
    /// 与其它轴相交时沿该轴的向外方向逐步外移；同一轴过密（轴太短）时直接舍弃该标签，
    /// 避免把标签推离自己的刻度。
    /// </summary>
    private void DrawTickLabels(DrawingContext dc, PlotScene scene)
    {
        _placedLabels.Clear();
        if (_tickLabels.Count == 0)
        {
            return;
        }

        var view = scene.ViewSize;
        const double margin = 0.5;

        for (var i = 0; i < _tickLabels.Count; i++)
        {
            var label = _tickLabels[i];
            var rect = label.Bounds;

            if (!TryFindCollision(rect, out var collidingAxis))
            {
                // 未发生冲突的越界标签仍然绘制，由控件 ClipToBounds 裁切，保留半可见效果。
                PlaceLabel(dc, label.Layout, rect, label.Axis);
                continue;
            }

            // 同一轴上的刻度标签相互重叠：轴太短、刻度太密，直接略过该标签。
            if (collidingAxis == label.Axis)
            {
                continue;
            }

            var step = Math.Max(2.0, rect.Height * 0.35);
            for (var attempt = 1; attempt <= MaxLabelNudgeAttempts; attempt++)
            {
                var candidate = rect.Translate(label.Outward * (step * attempt));
                if (!IsInside(candidate, view, margin))
                {
                    continue;
                }

                if (!TryFindCollision(candidate, out var axis))
                {
                    PlaceLabel(dc, label.Layout, candidate, label.Axis);
                    break;
                }

                if (axis == label.Axis)
                {
                    break;
                }
            }
        }
    }

    private void PlaceLabel(DrawingContext dc, TextLayout layout, Rect rect, int axis)
    {
        _placedLabels.Add((rect, axis));
        _textRects.Add(rect);
        layout.Draw(dc, new Point(rect.X, rect.Y));
    }

    private bool TryFindCollision(Rect rect, out int axis)
    {
        for (var i = 0; i < _placedLabels.Count; i++)
        {
            var other = _placedLabels[i];
            if (rect.X < other.Rect.Right && rect.Right > other.Rect.X &&
                rect.Y < other.Rect.Bottom && rect.Bottom > other.Rect.Y)
            {
                axis = other.Axis;
                return true;
            }
        }

        axis = -1;
        return false;
    }

    private static bool IsInside(Rect rect, Size view, double margin) =>
        rect.X >= margin && rect.Y >= margin &&
        rect.Right <= view.Width - margin && rect.Bottom <= view.Height - margin;

    internal static double SanitizeTickLength(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 24) : 4;

    internal static double SanitizeGap(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, -24, 48) : 3;

    private static Point ComputeLabelOrigin(Point tip, Point outward, TextLayout layout, double gap, Vector offset)
    {
        var x = tip.X + outward.X * gap + offset.X;
        var y = tip.Y + outward.Y * gap + offset.Y;

        // outward 指向图形外侧：负向时把文本整体移到外侧，正向时保持边缘对齐。
        if (outward.X < -0.35)
        {
            x -= layout.Width;
        }
        else if (Math.Abs(outward.X) <= 0.35)
        {
            x -= layout.Width * 0.5;
        }

        if (outward.Y < -0.35)
        {
            y -= layout.Height;
        }
        else if (Math.Abs(outward.Y) <= 0.35)
        {
            y -= layout.Height * 0.5;
        }

        return new Point(x, y);
    }

    private void DrawAxisTitles(DrawingContext dc, PlotScene scene, IBrush textBrush)
    {
        var xTitle = _owner.XAxisTitle;
        if (!string.IsNullOrWhiteSpace(xTitle))
        {
            DrawAxisTitle(dc, scene.XEdge, xTitle, textBrush, extraDistance: 0);
        }

        var yTitle = _owner.YAxisTitle;
        if (!string.IsNullOrWhiteSpace(yTitle))
        {
            DrawAxisTitle(dc, scene.YEdge, yTitle, textBrush, extraDistance: 0);
        }

        var zTitle = _owner.ZAxisTitle;
        if (!string.IsNullOrWhiteSpace(zTitle))
        {
            // Z 轴标题需要避开刻度标签的宽度。
            var maxLabelWidth = 0.0;
            if (_owner.ShowTickLabels)
            {
                foreach (var label in scene.ZTicks.Labels)
                {
                    maxLabelWidth = Math.Max(maxLabelWidth, GetText(label, textBrush, _owner.ResolveTickLabelFontSize()).Width);
                }
            }

            DrawAxisTitle(dc, scene.ZEdge, zTitle, textBrush, extraDistance: maxLabelWidth);
        }
    }

    private void DrawAxisTitle(
        DrawingContext dc,
        AxisEdge edge,
        string title,
        IBrush textBrush,
        double extraDistance)
    {
        var titleFontSize = _owner.ResolveAxisTitleFontSize();
        var layout = GetText(title, textBrush, titleFontSize);
        var mid = new Point(
            (edge.ScreenStart.X + edge.ScreenEnd.X) * 0.5,
            (edge.ScreenStart.Y + edge.ScreenEnd.Y) * 0.5);

        var tickLength = SanitizeTickLength(_owner.TickLength);
        var labelGap = SanitizeGap(_owner.TickLabelGap);
        var titleGap = SanitizeGap(_owner.AxisTitleGap);
        var increase = _owner.ResolveTickLabelFontSize() - titleFontSize;

        var distance = tickLength + labelGap + titleFontSize + 14 + titleGap + extraDistance + Math.Max(0, increase);
        var offset = _owner.AxisTitleOffset;
        var anchor = mid + edge.Outward * distance + new Vector(offset.X, offset.Y);
        var origin = new Point(anchor.X - layout.Width * 0.5, anchor.Y - layout.Height * 0.5);
        layout.Draw(dc, origin);
        _textRects.Add(new Rect(origin, new Size(layout.Width, layout.Height)));
    }

    /// <summary>绘制图例（位置由 <see cref="Plot3DView.LegendPosition" /> / <see cref="Plot3DView.LegendOffset" /> 决定，可按需自动避让文本）。</summary>
    public void DrawLegend(DrawingContext dc, PlotScene scene, IReadOnlyList<(string Text, Color Color)> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var textBrush = _owner.ResolveTextBrush();
        var fontSize = ResolveLegendFontSize();
        var contentWidth = 0.0;
        var textHeight = 0.0;
        var layouts = new TextLayout[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            layouts[i] = GetText(items[i].Text, textBrush, fontSize);
            contentWidth = Math.Max(contentWidth, layouts[i].Width);
            textHeight = Math.Max(textHeight, layouts[i].Height);
        }

        var width = 20 + contentWidth + 12;

        // 行高按实测文本高度计算，避免紧凑模式下行间粘连。
        var normalRowHeight = Math.Max(fontSize + 8, textHeight + 4);
        var compactRowHeight = Math.Max(10, textHeight + 1);
        var avoid = _owner.IsLegendAvoidanceEnabled;
        var canCompact = avoid && items.Count > 1 && compactRowHeight < normalRowHeight;
        var offset = SanitizeLegendOffset(_owner.LegendOffset);
        var sceneStable = ReferenceEquals(scene, _legendLastScene);
        if (!sceneStable)
        {
            // 相机 / 数据 / 尺寸变化：本次回位作废，等下一次交互结束时重新武装。
            _legendReturnArmed = false;
        }

        _legendSceneStableFrames = sceneStable ? _legendSceneStableFrames + 1 : 0;
        _legendStepFailed = false;

        // 仅在交互结束后（或回位动画进行中）且场景未变化时回位。
        var canReturn = _legendReturnArmed && _legendSceneStableFrames >= 1;

        if (_owner.LegendPosition != _lastLegendPosition ||
            offset != _lastLegendOffset ||
            avoid != _lastLegendAvoidance ||
            _legendViewSize != scene.ViewSize)
        {
            // 位置 / 偏移 / 开关 / 控件尺寸变化后不再沿用旧位置。
            _lastLegendBox = null;
            _legendPushDirection = default;
            _lastLegendPosition = _owner.LegendPosition;
            _lastLegendOffset = offset;
            _lastLegendAvoidance = avoid;
            _legendViewSize = scene.ViewSize;
        }

        double rowHeight;
        double topPadding;
        Rect box;

        // 上一帧的位置仍然有效时直接沿用，避免旋转 / 缩放时反复搜索与跳动。
        if (TryReuseLastLegendBox(scene.ViewSize, width, items.Count, normalRowHeight, compactRowHeight, offset, avoid, canReturn,
                out var reusable, out var reusableRowHeight, out var reusablePadding))
        {
            box = reusable;
            rowHeight = reusableRowHeight;
            topPadding = reusablePadding;
        }
        else
        {
            var normalHeight = items.Count * normalRowHeight + 8;
            var normalBox = ResolveLegendBox(scene, width, normalHeight, offset, previous: _lastLegendBox, hint: _lastLegendBox);
            var normalOverlap = TextOverlapArea(normalBox);
            var normalMove = _lastLegendBox is { } previousNormal ? CenterDistance(normalBox, previousNormal) : 0.0;

            rowHeight = normalRowHeight;
            topPadding = 4;
            box = normalBox;

            // 空间紧张（仍重叠，或为了避让而大幅位移）时尝试紧凑行距：
            // 缩小后的图例往往能在原位置附近放下，避免一次性跳得很远。
            if (canCompact && (normalOverlap > 0.5 || (_lastLegendBox is { } && normalMove > 24)))
            {
                var compactHeight = items.Count * compactRowHeight + 6;
                var compactBox = ResolveLegendBox(scene, width, compactHeight, offset, previous: null, hint: _lastLegendBox);
                var compactOverlap = TextOverlapArea(compactBox);
                var compactMove = _lastLegendBox is { } previousCompact ? CenterDistance(compactBox, previousCompact) : 0.0;

                var better = compactOverlap < normalOverlap - 0.5 ||
                             (compactOverlap <= normalOverlap + 0.5 && compactMove + 12 < normalMove);
                if (better)
                {
                    rowHeight = compactRowHeight;
                    topPadding = 3;
                    box = compactBox;
                }
            }
        }

        _lastLegendBox = box;
        LastLegendBounds = box;
        _legendLastScene = scene;

        // 驱动回位：回位已武装、目标可放置且当前位置不一致时主动请求下一帧。
        // 到位或路径被文字阻挡时自动停止。
        if (avoid && canReturn && !_owner.IsExporting && !_legendReturnPending && _textRects.Count > 0 &&
            width <= scene.ViewSize.Width - 4 && box.Height <= scene.ViewSize.Height - 4)
        {
            var target = ComputeLegendBox(scene.ViewSize, width, box.Height, _owner.LegendPosition, offset);
            if (!box.Equals(target) && TextOverlapArea(target) <= 0.5 && !_legendStepFailed)
            {
                _legendReturnPending = true;
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        _legendReturnPending = false;
                        _owner.InvalidateVisual();
                    },
                    DispatcherPriority.Background);
            }
        }

        var background = _owner.ResolveLegendBackground();
        dc.DrawRectangle(background, new Pen(_owner.ResolveGridBrush(), 1), box, 4, 4);

        for (var i = 0; i < items.Count; i++)
        {
            var rowTop = box.Y + topPadding + i * rowHeight;
            var swatch = new Rect(box.X + 8, rowTop + (rowHeight - 3) * 0.5, 14, 3);
            dc.FillRectangle(new ImmutableSolidColorBrush(items[i].Color), swatch);
            layouts[i].Draw(dc, new Point(box.X + 28, rowTop + (rowHeight - layouts[i].Height) * 0.5));
        }
    }

    /// <summary>
    /// 上一帧的图例外框在当前帧仍有效（尺寸一致、在控件内、不压文字）时直接沿用，
    /// 避免旋转 / 缩放时刻度文字连续变化导致图例反复跳动。
    /// </summary>
    private bool TryReuseLastLegendBox(
        Size viewSize,
        double width,
        int itemCount,
        double normalRowHeight,
        double compactRowHeight,
        Vector offset,
        bool avoid,
        bool canReturn,
        out Rect box,
        out double rowHeight,
        out double topPadding)
    {
        box = default;
        rowHeight = normalRowHeight;
        topPadding = 4;

        if (!avoid || _lastLegendBox is not { } previous || _textRects.Count == 0)
        {
            return false;
        }

        var normalHeight = itemCount * normalRowHeight + 8;
        var compactHeight = itemCount * compactRowHeight + 6;

        if (previous.Height.Equals(compactHeight) && compactHeight < normalHeight)
        {
            rowHeight = compactRowHeight;
            topPadding = 3;
        }
        else if (!previous.Height.Equals(normalHeight))
        {
            return false;
        }

        if (!previous.Width.Equals(width) || !IsRectInside(previous, viewSize) || TextOverlapArea(previous) > 0.5)
        {
            return false;
        }

        // 障碍已经消失时逐帧向配置位置回移，避免一次性跳回；回移路径被挡则保持原位。
        // 仅在相机 / 数据未变化（同一场景）时回位，避免旋转、缩放过程中与避让互相拉扯。
        var preferred = ComputeLegendBox(viewSize, width, previous.Height, _owner.LegendPosition, offset);
        if (canReturn && !_owner.IsExporting && TextOverlapArea(preferred) <= 0.5)
        {
            if (TryStepToward(previous, preferred, viewSize, out var stepped))
            {
                box = stepped;
                return true;
            }

            // 目标可放置但路径被文字挡住：本帧不移动，也不继续驱动重绘。
            _legendStepFailed = true;
        }

        box = previous;
        return true;
    }

    /// <summary>从当前位置向配置位置迈出一小步；目标被文字遮挡或超出控件时不移动。</summary>
    private bool TryStepToward(Rect from, Rect target, Size viewSize, out Rect stepped)
    {
        stepped = from;
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 0.01)
        {
            return false;
        }

        var t = Math.Min(1.0, LegendReturnStep / distance);
        var candidate = new Rect(from.X + dx * t, from.Y + dy * t, from.Width, from.Height);
        if (!IsRectInside(candidate, viewSize) || TextOverlapArea(candidate) > 0.5)
        {
            return false;
        }

        stepped = candidate;
        return true;
    }

    /// <summary>
    /// 计算图例外框：先按请求的位置 / 偏移摆放；若开启自动避让且会压住刻度文本或轴标题，
    /// 则从上一帧位置（或位置提示）出发做“最小位移推出”，必要时再回退到网格搜索。
    /// </summary>
    private Rect ResolveLegendBox(PlotScene scene, double width, double height, Vector offset, Rect? previous, Rect? hint = null)
    {
        var viewSize = scene.ViewSize;
        var preferred = ComputeLegendBox(viewSize, width, height, _owner.LegendPosition, offset);

        if (!_owner.IsLegendAvoidanceEnabled || _textRects.Count == 0)
        {
            return preferred;
        }

        // 图例比控件还大时无法完全避让，直接夹取到控件内，避免每帧重复搜索。
        if (width > viewSize.Width - 4 || height > viewSize.Height - 4)
        {
            return ClampLegendRect(preferred, viewSize, width, height);
        }

        var hasPrevious = previous is { } prev &&
                          prev.Width.Equals(width) &&
                          prev.Height.Equals(height) &&
                          IsRectInside(prev, viewSize);

        if (!hasPrevious && hint is null)
        {
            // 首次摆放直接全局搜索，优先选一个离文字更远、更稳定的“宽阔”位置，
            // 避免贴着标签停放后又被后续扫过的标签顶动。
            return TextOverlapArea(preferred) <= 0.5
                ? preferred
                : FindFreeLegendBox(viewSize, width, height, preferred, preferred);
        }

        // 有上一帧位置（或尺寸变化后的位置提示）时优先保持原位：只做最小位移推出。
        var start = hasPrevious
            ? previous!.Value
            : ClampLegendRect(new Rect(hint!.Value.X, hint.Value.Y, width, height), viewSize, width, height);

        var pushed = PushOutOfText(start, viewSize, width, height);
        if (TextOverlapArea(pushed) <= 0.5)
        {
            return pushed;
        }

        if (TextOverlapArea(preferred) <= 0.5)
        {
            return preferred;
        }

        return FindFreeLegendBox(viewSize, width, height, preferred, start);
    }

    /// <summary>
    /// 把外框沿最小位移方向逐步推出所有文字矩形：每轮处理重叠面积最大的一块文字，
    /// 优先延续上一次的推出方向，避免在文字两侧来回跳。
    /// </summary>
    private Rect PushOutOfText(Rect start, Size viewSize, double width, double height)
    {
        var rect = ClampLegendRect(start, viewSize, width, height);
        var direction = _legendPushDirection;
        var lastArea = double.MaxValue;

        for (var iteration = 0; iteration < 16; iteration++)
        {
            if (!TryFindDeepestTextOverlap(rect, LegendClearance, out var text))
            {
                _legendPushDirection = direction;
                return rect;
            }

            // 重叠面积不再下降说明卡在两个文字之间，放弃方向粘滞，改用最小位移逃逸。
            var area = TextOverlapArea(rect);
            if (area >= lastArea - 0.5)
            {
                direction = default;
            }

            lastArea = area;

            const double gap = 1;
            var candidates = new (double Dx, double Dy)[]
            {
                (text.X - gap - rect.Right, 0),
                (text.Right + gap - rect.X, 0),
                (0, text.Y - gap - rect.Bottom),
                (0, text.Bottom + gap - rect.Y),
            };

            var moved = false;
            var minMagnitude = double.MaxValue;

            foreach (var (dx, dy) in candidates)
            {
                if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9)
                {
                    continue;
                }

                var candidate = new Rect(rect.X + dx, rect.Y + dy, width, height);
                if (!IsRectInside(candidate, viewSize))
                {
                    continue;
                }

                moved = true;
                minMagnitude = Math.Min(minMagnitude, Math.Abs(dx) + Math.Abs(dy));
            }

            if (!moved)
            {
                return rect;
            }

            // 只在位移接近最小的候选里延续上一次的推出方向，避免为了保持方向而做出大幅跳动。
            var bestAlignment = double.MinValue;
            var bestDx = 0.0;
            var bestDy = 0.0;

            foreach (var (dx, dy) in candidates)
            {
                if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9)
                {
                    continue;
                }

                var candidate = new Rect(rect.X + dx, rect.Y + dy, width, height);
                if (!IsRectInside(candidate, viewSize))
                {
                    continue;
                }

                var magnitude = Math.Abs(dx) + Math.Abs(dy);
                if (magnitude > minMagnitude + 2)
                {
                    continue;
                }

                var alignment = direction.Length > 0.1
                    ? (dx * direction.X + dy * direction.Y) / magnitude
                    : 0.0;

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestDx = dx;
                    bestDy = dy;
                }
            }

            rect = new Rect(rect.X + bestDx, rect.Y + bestDy, width, height);
            var length = Math.Sqrt(bestDx * bestDx + bestDy * bestDy);
            if (length > 1e-6)
            {
                direction = new Vector(bestDx / length, bestDy / length);
            }
        }

        _legendPushDirection = direction;
        return rect;
    }

    /// <summary>找出与外框重叠面积最大的文字矩形（按指定间隙外扩）。</summary>
    private bool TryFindDeepestTextOverlap(Rect rect, double margin, out Rect worst)
    {
        worst = default;
        var worstArea = 0.0;

        for (var i = 0; i < _textRects.Count; i++)
        {
            var text = InflateTextRect(_textRects[i], margin);
            var overlap = rect.Intersect(text);
            if (overlap.Width <= 0 || overlap.Height <= 0)
            {
                continue;
            }

            var area = overlap.Width * overlap.Height;
            if (area > worstArea)
            {
                worstArea = area;
                worst = text;
            }
        }

        return worstArea > 0;
    }

    private static Rect ClampLegendRect(Rect rect, Size view, double width, double height)
    {
        var maxLeft = Math.Max(0.5, view.Width - width - 0.5);
        var maxTop = Math.Max(0.5, view.Height - height - 0.5);
        return new Rect(
            Math.Clamp(rect.X, Math.Min(0.5, maxLeft), maxLeft),
            Math.Clamp(rect.Y, Math.Min(0.5, maxTop), maxTop),
            width,
            height);
    }

    /// <summary>在控件范围内搜索不压住文字、且离参考位置最近的外框。</summary>
    private Rect FindFreeLegendBox(Size viewSize, double width, double height, Rect preferred, Rect reference)
    {
        var best = preferred;
        var bestScore = ScoreLegendCandidate(preferred, reference);

        const double margin = 2;
        var maxLeft = Math.Max(margin, viewSize.Width - width - margin);
        var maxTop = Math.Max(margin, viewSize.Height - height - margin);

        void Consider(Rect candidate)
        {
            // 距离是评分的下界，先做最便宜的剪枝，避免不必要的全量求交。
            var distance = DistanceSquared(candidate, reference);
            if (distance >= bestScore)
            {
                return;
            }

            var score = ScoreLegendCandidate(candidate, reference, distance);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        // 参考位置自身也是候选（可能它仍然可用）。
        Consider(reference);

        // 先试 9 个标准锚点，通常已足够（例如左中位置被 Z 轴标签占用时改到中间）。
        foreach (var anchor in AnchoredPositions)
        {
            Consider(ComputeLegendBox(viewSize, width, height, anchor, default));
        }

        // 再以网格搜索最近的可行位置。
        var step = Math.Max(2, Math.Min(8, Math.Min(viewSize.Width, viewSize.Height) / 48));
        for (var top = margin; top <= maxTop; top += step)
        {
            for (var left = margin; left <= maxLeft; left += step)
            {
                Consider(new Rect(left, top, width, height));
            }

            // 补最右列：step 通常不能整除可用宽度，贴边的狭缝也要参与评分。
            Consider(new Rect(maxLeft, top, width, height));
        }

        // 补最底行。
        for (var left = margin; left <= maxLeft; left += step)
        {
            Consider(new Rect(left, maxTop, width, height));
        }

        return best;
    }

    /// <summary>
    /// 候选评分：先保证“无重叠”恒优于“有重叠”（有重叠的候选统一加巨大惩罚），
    /// 再比较与文字的留白（避免贴着文字停放而被后续扫过的标签反复顶动），最后才是位移。
    /// </summary>
    private double ScoreLegendCandidate(Rect candidate, Rect reference) =>
        ScoreLegendCandidate(candidate, reference, DistanceSquared(candidate, reference));

    private double ScoreLegendCandidate(Rect candidate, Rect reference, double distance)
    {
        var overlap = TextOverlapArea(candidate);
        if (overlap > 0.5)
        {
            return OverlapPenalty + overlap;
        }

        return TextClearanceArea(candidate, LegendPreferredClearance) * 50 + distance;
    }

    /// <summary>外框与文字（外扩指定留白）的重叠面积，用于选择更“宽阔”的停放位置。</summary>
    private double TextClearanceArea(Rect rect, double margin)
    {
        var area = 0.0;
        for (var i = 0; i < _textRects.Count; i++)
        {
            var text = _textRects[i];
            var inflated = new Rect(text.X - margin, text.Y - margin, text.Width + margin * 2, text.Height + margin * 2);
            var overlap = rect.Intersect(inflated);
            if (overlap.Width > 0 && overlap.Height > 0)
            {
                area += overlap.Width * overlap.Height;
            }
        }

        return area;
    }

    private static double DistanceSquared(Rect a, Rect b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double CenterDistance(Rect a, Rect b)
    {
        var dx = a.X + a.Width * 0.5 - (b.X + b.Width * 0.5);
        var dy = a.Y + a.Height * 0.5 - (b.Y + b.Height * 0.5);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsRectInside(Rect rect, Size view) =>
        rect.X >= 0.5 && rect.Y >= 0.5 &&
        rect.Right <= view.Width - 0.5 && rect.Bottom <= view.Height - 0.5;

    private static readonly LegendPosition[] AnchoredPositions = Enum.GetValues<LegendPosition>();

    /// <summary>按指定位置与偏移计算图例外框，并限制在控件内。</summary>
    private static Rect ComputeLegendBox(Size viewSize, double width, double height, LegendPosition position, Vector offset)
    {
        var insetX = Math.Max(6, viewSize.Width * 0.02);
        var insetY = Math.Max(6, viewSize.Height * 0.02);

        // 枚举按“行优先”排列：/ 3 为行（上 / 中 / 下），% 3 为列（左 / 中 / 右）。
        var column = (int)position % 3;
        var row = (int)position / 3;

        var left = column switch
        {
            1 => (viewSize.Width - width) * 0.5,
            2 => viewSize.Width - width - insetX,
            _ => insetX,
        };

        var top = row switch
        {
            1 => (viewSize.Height - height) * 0.5,
            2 => viewSize.Height - height - insetY,
            _ => insetY,
        };

        if (double.IsFinite(offset.X))
        {
            left += offset.X;
        }

        if (double.IsFinite(offset.Y))
        {
            top += offset.Y;
        }

        var maxLeft = Math.Max(2, viewSize.Width - width - 2);
        var maxTop = Math.Max(2, viewSize.Height - height - 2);
        left = Math.Clamp(left, Math.Min(2, maxLeft), maxLeft);
        top = Math.Clamp(top, Math.Min(2, maxTop), maxTop);

        return new Rect(left, top, width, height);
    }

    /// <summary>外框与已绘制文本（刻度标签 / 轴标题）的重叠面积，文本外扩 <see cref="LegendClearance" /> 像素作为缓冲。</summary>
    private double TextOverlapArea(Rect rect)
    {
        var area = 0.0;
        for (var i = 0; i < _textRects.Count; i++)
        {
            var text = InflateTextRect(_textRects[i], LegendClearance);
            var overlap = rect.Intersect(text);
            if (overlap.Width > 0 && overlap.Height > 0)
            {
                area += overlap.Width * overlap.Height;
            }
        }

        return area;
    }

    private static Rect InflateTextRect(Rect rect, double margin) =>
        new(rect.X - margin, rect.Y - margin, rect.Width + margin * 2, rect.Height + margin * 2);

    /// <summary>把非有限的偏移量规约为 0，避免粘滞状态因 NaN 比较而每帧重置。</summary>
    private static Vector SanitizeLegendOffset(Vector offset) =>
        new(double.IsFinite(offset.X) ? offset.X : 0, double.IsFinite(offset.Y) ? offset.Y : 0);

    /// <summary>图例字号；<see cref="Plot3DView.FontSize" /> 无效时回退为 12。</summary>
    private double ResolveLegendFontSize()
    {
        var size = _owner.FontSize;
        return double.IsFinite(size) && size > 0.1 ? Math.Clamp(size, 4, 96) : 12;
    }

    private TextLayout GetText(string text, IBrush brush, double fontSize)
    {
        var family = _owner.FontFamily;

        if (!Equals(family, _cachedFamily) || !ReferenceEquals(brush, _cachedTextBrush))
        {
            _textCache.Clear();
            _cachedFamily = family;
            _cachedTextBrush = brush;
        }

        var key = (text, fontSize);
        if (_textCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var layout = new TextLayout(text, new Typeface(family), fontSize, brush);
        _textCache[key] = layout;
        return layout;
    }

    private static bool IsBoundary(double value, double min, double max) =>
        Math.Abs(value - min) <= double.Epsilon || Math.Abs(value - max) <= double.Epsilon;
}
