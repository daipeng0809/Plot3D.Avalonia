using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 负责绘制坐标框、背景面板、网格线、刻度与轴标题。
/// </summary>
internal sealed class FrameRenderer
{
    private const double TickLength = 4;
    private const double TickLabelGap = 3;

    private readonly Plot3DView _owner;
    private readonly Dictionary<string, TextLayout> _textCache = new(StringComparer.Ordinal);
    private FontFamily _cachedFamily = FontFamily.Default;
    private double _cachedFontSize = -1;
    private IBrush? _cachedTextBrush;

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
        var textBrush = _owner.ResolveTextBrush();

        DrawPanes(dc, scene);
        DrawGrid(dc, scene, gridBrush);
        DrawBox(dc, scene, axisBrush);

        if (_owner.ShowTicks)
        {
            DrawTicks(dc, scene, axisBrush, textBrush);
        }

        if (_owner.ShowAxisTitles)
        {
            DrawAxisTitles(dc, scene, textBrush);
        }
    }

    private void DrawPanes(DrawingContext dc, PlotScene scene)
    {
        if (!_owner.ShowPane)
        {
            return;
        }

        var brush = _owner.ResolvePaneBrush();
        var xFace = scene.FarX <= scene.Center.X ? (A: 0, B: 1, C: 3, D: 2) : (A: 4, B: 5, C: 7, D: 6);
        var yFace = scene.FarY <= scene.Center.Y ? (A: 0, B: 1, C: 5, D: 4) : (A: 2, B: 3, C: 7, D: 6);
        var zFace = scene.FarZ <= scene.Center.Z ? (A: 0, B: 2, C: 6, D: 4) : (A: 1, B: 3, C: 7, D: 5);

        FillQuad(dc, scene, brush, xFace);
        FillQuad(dc, scene, brush, yFace);
        FillQuad(dc, scene, brush, zFace);
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
        DrawAxisTicks(dc, scene, scene.ZEdge, scene.ZTicks, pen, textBrush, isVerticalAxis: true);
        DrawAxisTicks(dc, scene, scene.XEdge, scene.XTicks, pen, textBrush, isVerticalAxis: false);
        DrawAxisTicks(dc, scene, scene.YEdge, scene.YTicks, pen, textBrush, isVerticalAxis: false);
    }

    private void DrawAxisTicks(
        DrawingContext dc,
        PlotScene scene,
        AxisEdge edge,
        AxisTicks ticks,
        Pen pen,
        IBrush textBrush,
        bool isVerticalAxis)
    {
        var span = ticks.Max - ticks.Min;
        if (span <= 0)
        {
            return;
        }

        for (var i = 0; i < ticks.Values.Length; i++)
        {
            var t = Math.Clamp((ticks.Values[i] - ticks.Min) / span, 0, 1);
            var world = Point3D.Lerp(edge.Start, edge.End, t);
            if (!scene.Projection.TryProject(world, out var point))
            {
                continue;
            }

            var tip = point + edge.Outward * TickLength;
            dc.DrawLine(pen, point, tip);

            if (!_owner.ShowTickLabels)
            {
                continue;
            }

            var layout = GetText(ticks.Labels[i], textBrush);
            if (isVerticalAxis && Math.Abs(layout.Width) < 0.01)
            {
                continue;
            }

            layout.Draw(dc, ComputeLabelOrigin(tip, edge.Outward, layout));
        }
    }

    private static Point ComputeLabelOrigin(Point tip, Point outward, TextLayout layout)
    {
        var x = tip.X + outward.X * TickLabelGap;
        var y = tip.Y + outward.Y * TickLabelGap;

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
                    maxLabelWidth = Math.Max(maxLabelWidth, GetText(label, textBrush).Width);
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
        var layout = GetText(title, textBrush);
        var mid = new Point(
            (edge.ScreenStart.X + edge.ScreenEnd.X) * 0.5,
            (edge.ScreenStart.Y + edge.ScreenEnd.Y) * 0.5);

        var distance = TickLength + TickLabelGap + _owner.FontSize + 14 + extraDistance;
        var anchor = mid + edge.Outward * distance;
        var origin = new Point(anchor.X - layout.Width * 0.5, anchor.Y - layout.Height * 0.5);
        layout.Draw(dc, origin);
    }

    /// <summary>绘制图例（左上角）。</summary>
    public void DrawLegend(DrawingContext dc, PlotScene scene, IReadOnlyList<(string Text, Color Color)> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var textBrush = _owner.ResolveTextBrush();
        var rowHeight = _owner.FontSize + 8;
        var contentWidth = 0.0;
        var layouts = new TextLayout[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            layouts[i] = GetText(items[i].Text, textBrush);
            contentWidth = Math.Max(contentWidth, layouts[i].Width);
        }

        var width = 20 + contentWidth + 12;
        var height = items.Count * rowHeight + 8;
        var left = Math.Max(6, scene.ViewSize.Width * 0.02);
        var top = Math.Max(6, scene.ViewSize.Height * 0.02);
        var box = new Rect(left, top, width, height);

        var background = _owner.ResolveLegendBackground();
        dc.DrawRectangle(background, new Pen(_owner.ResolveGridBrush(), 1), box, 4, 4);

        for (var i = 0; i < items.Count; i++)
        {
            var rowTop = box.Y + 4 + i * rowHeight;
            var swatch = new Rect(box.X + 8, rowTop + (rowHeight - 3) * 0.5, 14, 3);
            dc.FillRectangle(new ImmutableSolidColorBrush(items[i].Color), swatch);
            layouts[i].Draw(dc, new Point(box.X + 28, rowTop + (rowHeight - layouts[i].Height) * 0.5));
        }
    }

    private TextLayout GetText(string text, IBrush brush)
    {
        var family = _owner.FontFamily;
        var fontSize = _owner.FontSize;

        if (!Equals(family, _cachedFamily) ||
            Math.Abs(fontSize - _cachedFontSize) > 1e-6 ||
            !ReferenceEquals(brush, _cachedTextBrush))
        {
            _textCache.Clear();
            _cachedFamily = family;
            _cachedFontSize = fontSize;
            _cachedTextBrush = brush;
        }

        if (_textCache.TryGetValue(text, out var cached))
        {
            return cached;
        }

        var layout = new TextLayout(text, new Typeface(family), fontSize, brush);
        _textCache[text] = layout;
        return layout;
    }

    private static bool IsBoundary(double value, double min, double max) =>
        Math.Abs(value - min) <= double.Epsilon || Math.Abs(value - max) <= double.Epsilon;
}
