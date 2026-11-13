using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Rendering;

/// <summary>
/// 绘制交互叠加层：十字准线、悬停 / 选中高亮、悬停提示框与坐标读数框。
/// </summary>
internal sealed class InteractionOverlayRenderer
{
    private const double ToolTipOffset = 14;

    private readonly Plot3DView _owner;
    private readonly Dictionary<(string Text, double FontSize), TextLayout> _textCache = new();
    private FontFamily _cachedFamily = FontFamily.Default;
    private IBrush? _cachedCachedBrush;

    public InteractionOverlayRenderer(Plot3DView owner)
    {
        _owner = owner;
    }

    /// <summary>清除文字布局缓存（字体或画刷变化时调用）。</summary>
    public void ResetCache() => _textCache.Clear();

    public void Render(
        DrawingContext dc,
        PlotScene scene,
        PlotPointInfo? hover,
        PlotPointInfo? selected,
        Point pointer,
        Point3D? crosshairWorld,
        string? tooltipText,
        string? readoutText)
    {
        if (crosshairWorld is { } world)
        {
            DrawCrosshair(dc, scene, world);
        }

        if (selected is { } selectedPoint)
        {
            DrawSelectedPoint(dc, scene, selectedPoint);
        }

        if (hover is { } hoverPoint)
        {
            DrawHoverPoint(dc, scene, hoverPoint);
        }

        if (!string.IsNullOrEmpty(readoutText))
        {
            DrawBox(
                dc,
                scene,
                pointer,
                readoutText!,
                _owner.ToolTipForeground ?? _owner.ResolveTextBrush());
        }

        if (!string.IsNullOrEmpty(tooltipText))
        {
            DrawBox(
                dc,
                scene,
                pointer,
                tooltipText!,
                _owner.ToolTipForeground ?? _owner.ResolveTextBrush());
        }
    }

    private void DrawCrosshair(DrawingContext dc, PlotScene scene, in Point3D world)
    {
        var projection = scene.Projection;
        var brush = _owner.CrosshairBrush ?? _owner.ResolveAxisBrush();
        var pen = new Pen(brush, 1, DashStyle.Dash);

        var x = Math.Clamp(world.X, scene.XTicks.Min, scene.XTicks.Max);
        var y = Math.Clamp(world.Y, scene.YTicks.Min, scene.YTicks.Max);
        var z = Math.Clamp(world.Z, scene.ZTicks.Min, scene.ZTicks.Max);

        DrawGuide(dc, pen, scene, x, y, z, scene.FarX, y, z);
        DrawGuide(dc, pen, scene, x, y, z, x, scene.FarY, z);
        DrawGuide(dc, pen, scene, x, y, z, x, y, scene.FarZ);

        if (projection.TryProject(x, y, z, out var sx, out var sy))
        {
            var center = new Point(sx, sy);
            var accent = _owner.CrosshairBrush is ISolidColorBrush solid
                ? solid.Color
                : _owner.HoverHighlightColor;

            dc.DrawEllipse(new ImmutableSolidColorBrush(accent), null, center, 2.4, 2.4);
        }
    }

    private static void DrawGuide(
        DrawingContext dc,
        Pen pen,
        PlotScene scene,
        double x1, double y1, double z1,
        double x2, double y2, double z2)
    {
        if (!scene.Projection.TryProject(x1, y1, z1, out var ax, out var ay) ||
            !scene.Projection.TryProject(x2, y2, z2, out var bx, out var by))
        {
            return;
        }

        var start = new Point(ax, ay);
        var end = new Point(bx, by);
        if (Math.Abs(start.X - end.X) < 0.01 && Math.Abs(start.Y - end.Y) < 0.01)
        {
            return;
        }

        dc.DrawLine(pen, start, end);
    }

    private void DrawHoverPoint(DrawingContext dc, PlotScene scene, in PlotPointInfo info)
    {
        if (!scene.Projection.TryProject(info.Position, out var screen))
        {
            return;
        }

        var center = new Point(screen.X, screen.Y);
        var color = _owner.HoverHighlightColor;
        var baseSize = ResolveBaseMarkerSize(info.Series);
        var size = _owner.HoverHighlightSize > 0 && double.IsFinite(_owner.HoverHighlightSize)
            ? _owner.HoverHighlightSize
            : baseSize + 4;

        var radius = Math.Max(size * 0.5, 3.5);
        var halo = new ImmutableSolidColorBrush(Color.FromArgb(70, color.R, color.G, color.B));
        dc.DrawEllipse(halo, null, center, radius + 3.5, radius + 3.5);
        dc.DrawEllipse(new ImmutableSolidColorBrush(color), null, center, radius, radius);
    }

    private void DrawSelectedPoint(DrawingContext dc, PlotScene scene, in PlotPointInfo info)
    {
        if (!scene.Projection.TryProject(info.Position, out var screen))
        {
            return;
        }

        var center = new Point(screen.X, screen.Y);
        var color = _owner.SelectionHighlightColor;
        var baseSize = ResolveBaseMarkerSize(info.Series);
        var size = _owner.SelectionHighlightSize > 0 && double.IsFinite(_owner.SelectionHighlightSize)
            ? _owner.SelectionHighlightSize
            : baseSize + 4;

        var radius = Math.Max(size * 0.5, 3.5);
        var brush = new ImmutableSolidColorBrush(Color.FromArgb(190, color.R, color.G, color.B));
        var pen = new Pen(new ImmutableSolidColorBrush(color), 2);

        dc.DrawEllipse(brush, null, center, radius * 0.6, radius * 0.6);
        dc.DrawEllipse(null, pen, center, radius + 3, radius + 3);
    }

    private static double ResolveBaseMarkerSize(PlotSeries3D series)
    {
        var markerSize = series.MarkerSize;
        return markerSize > 0 && double.IsFinite(markerSize) ? markerSize : 6;
    }

    private void DrawBox(DrawingContext dc, PlotScene scene, Point anchor, string text, IBrush textBrush)
    {
        var layout = GetText(text, ResolveToolTipFontSize(), textBrush);
        const double padX = 7;
        const double padY = 5;

        var width = layout.Width + padX * 2;
        var height = layout.Height + padY * 2;

        var x = anchor.X + ToolTipOffset;
        var y = anchor.Y + ToolTipOffset;

        if (x + width > scene.ViewSize.Width - 2)
        {
            x = anchor.X - ToolTipOffset - width;
        }

        if (y + height > scene.ViewSize.Height - 2)
        {
            y = anchor.Y - ToolTipOffset - height;
        }

        x = Math.Max(2, Math.Min(x, scene.ViewSize.Width - width - 2));
        y = Math.Max(2, Math.Min(y, scene.ViewSize.Height - height - 2));

        var box = new Rect(x, y, width, height);
        var background = _owner.ToolTipBackground ?? _owner.ResolveLegendBackground();
        var border = _owner.ToolTipBorderBrush ?? _owner.ResolveGridBrush();

        dc.DrawRectangle(background, new Pen(border, 1), box, 4, 4);
        layout.Draw(dc, new Point(box.X + padX, box.Y + padY));
    }

    /// <summary>提示框字号；<see cref="Plot3DView.FontSize" /> 无效时回退为 12。</summary>
    private double ResolveToolTipFontSize()
    {
        var size = _owner.FontSize;
        return double.IsFinite(size) && size > 0.1 ? Math.Clamp(size, 4, 96) : 12;
    }

    private TextLayout GetText(string text, double fontSize, IBrush brush)
    {
        var family = _owner.FontFamily;

        if (!Equals(family, _cachedFamily) || !ReferenceEquals(brush, _cachedCachedBrush))
        {
            _textCache.Clear();
            _cachedFamily = family;
            _cachedCachedBrush = brush;
        }

        var key = (text, fontSize);
        if (_textCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // 提示 / 读数文本包含实时坐标，几乎每次鼠标移动都会产生新键，必须限制缓存规模。
        if (_textCache.Count >= 64)
        {
            _textCache.Clear();
        }

        var layout = new TextLayout(text, new Typeface(family), fontSize, brush);
        _textCache[key] = layout;
        return layout;
    }
}
