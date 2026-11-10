using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Plot3D.Avalonia.Modeling;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Controls;

/// <summary>
/// <see cref="Plot3DView"/> 的数据点交互：悬停高亮与提示、十字准线坐标读数、点击选中。
/// </summary>
public partial class Plot3DView
{
    #region 属性注册

    /// <summary>是否启用数据点悬停高亮与提示。</summary>
    public static readonly StyledProperty<bool> IsPointHoverEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsPointHoverEnabled), true);

    /// <summary>悬停命中半径（像素）。</summary>
    public static readonly StyledProperty<double> HoverRadiusProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(HoverRadius), 10.0);

    /// <summary>悬停高亮颜色。</summary>
    public static readonly StyledProperty<Color> HoverHighlightColorProperty =
        AvaloniaProperty.Register<Plot3DView, Color>(nameof(HoverHighlightColor), Color.Parse("#FF9F0A"));

    /// <summary>悬停高亮标记尺寸；0 表示自动（普通标记尺寸 + 4）。</summary>
    public static readonly StyledProperty<double> HoverHighlightSizeProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(HoverHighlightSize));

    /// <summary>是否在鼠标旁显示悬停提示框。</summary>
    public static readonly StyledProperty<bool> IsHoverToolTipEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsHoverToolTipEnabled), true);

    /// <summary>提示框背景画刷；null 时使用主题图例背景。</summary>
    public static readonly StyledProperty<IBrush?> ToolTipBackgroundProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(ToolTipBackground));

    /// <summary>提示框边框画刷；null 时使用主题网格色。</summary>
    public static readonly StyledProperty<IBrush?> ToolTipBorderBrushProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(ToolTipBorderBrush));

    /// <summary>提示框文字画刷；null 时使用主题文字色。</summary>
    public static readonly StyledProperty<IBrush?> ToolTipForegroundProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(ToolTipForeground));

    /// <summary>是否启用数据点点击选中。</summary>
    public static readonly StyledProperty<bool> IsPointSelectionEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsPointSelectionEnabled), true);

    /// <summary>点击选中命中半径（像素）。</summary>
    public static readonly StyledProperty<double> SelectionRadiusProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(SelectionRadius), 12.0);

    /// <summary>选中高亮颜色。</summary>
    public static readonly StyledProperty<Color> SelectionHighlightColorProperty =
        AvaloniaProperty.Register<Plot3DView, Color>(nameof(SelectionHighlightColor), Color.Parse("#0A84FF"));

    /// <summary>选中高亮标记尺寸；0 表示自动（普通标记尺寸 + 4）。</summary>
    public static readonly StyledProperty<double> SelectionHighlightSizeProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(SelectionHighlightSize));

    /// <summary>是否显示鼠标十字准线与坐标读数。</summary>
    public static readonly StyledProperty<bool> IsCrosshairEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsCrosshairEnabled), true);

    /// <summary>十字准线画刷；null 时使用主题坐标轴色。</summary>
    public static readonly StyledProperty<IBrush?> CrosshairBrushProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(CrosshairBrush));

    /// <summary>是否显示十字准线的 X / Y / Z 坐标读数框。</summary>
    public static readonly StyledProperty<bool> ShowCrosshairLabelsProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowCrosshairLabels), true);

    #endregion

    private PlotPointInfo? _hoverPoint;
    private PlotPointInfo? _selectedPoint;
    private Point _pointerPosition;
    private bool _pointerInside;
    private Point3D? _crosshairWorld;
    private InteractionOverlayRenderer? _interactionRenderer;
    private Cursor? _hoverCursor;
    private Func<PlotPointInfo, string?>? _hoverTextFormatter;

    /// <summary>准线交点相对视图平面的最大偏移（包围盒对角线倍数），超过则隐藏准线。</summary>
    private const double CrosshairMaxOffsetFactor = 1.5;

    /// <summary>当用户点击选中某个数据点时触发。</summary>
    public event EventHandler<PlotPointEventArgs>? PointSelected;

    /// <summary>当选中的数据点被取消（点击空白处、删除或数据变化）时触发。</summary>
    public event EventHandler<PlotPointEventArgs>? PointDeselected;

    /// <summary>是否启用数据点悬停高亮与提示。</summary>
    public bool IsPointHoverEnabled
    {
        get => GetValue(IsPointHoverEnabledProperty);
        set => SetValue(IsPointHoverEnabledProperty, value);
    }

    /// <summary>悬停命中半径（像素）。</summary>
    public double HoverRadius
    {
        get => GetValue(HoverRadiusProperty);
        set => SetValue(HoverRadiusProperty, value);
    }

    /// <summary>悬停高亮颜色。</summary>
    public Color HoverHighlightColor
    {
        get => GetValue(HoverHighlightColorProperty);
        set => SetValue(HoverHighlightColorProperty, value);
    }

    /// <summary>悬停高亮标记尺寸；0 表示自动。</summary>
    public double HoverHighlightSize
    {
        get => GetValue(HoverHighlightSizeProperty);
        set => SetValue(HoverHighlightSizeProperty, value);
    }

    /// <summary>是否在鼠标旁显示悬停提示框。</summary>
    public bool IsHoverToolTipEnabled
    {
        get => GetValue(IsHoverToolTipEnabledProperty);
        set => SetValue(IsHoverToolTipEnabledProperty, value);
    }

    /// <summary>提示框背景画刷。</summary>
    public IBrush? ToolTipBackground
    {
        get => GetValue(ToolTipBackgroundProperty);
        set => SetValue(ToolTipBackgroundProperty, value);
    }

    /// <summary>提示框边框画刷。</summary>
    public IBrush? ToolTipBorderBrush
    {
        get => GetValue(ToolTipBorderBrushProperty);
        set => SetValue(ToolTipBorderBrushProperty, value);
    }

    /// <summary>提示框文字画刷。</summary>
    public IBrush? ToolTipForeground
    {
        get => GetValue(ToolTipForegroundProperty);
        set => SetValue(ToolTipForegroundProperty, value);
    }

    /// <summary>是否启用数据点点击选中。</summary>
    public bool IsPointSelectionEnabled
    {
        get => GetValue(IsPointSelectionEnabledProperty);
        set => SetValue(IsPointSelectionEnabledProperty, value);
    }

    /// <summary>点击选中命中半径（像素）。</summary>
    public double SelectionRadius
    {
        get => GetValue(SelectionRadiusProperty);
        set => SetValue(SelectionRadiusProperty, value);
    }

    /// <summary>选中高亮颜色。</summary>
    public Color SelectionHighlightColor
    {
        get => GetValue(SelectionHighlightColorProperty);
        set => SetValue(SelectionHighlightColorProperty, value);
    }

    /// <summary>选中高亮标记尺寸；0 表示自动。</summary>
    public double SelectionHighlightSize
    {
        get => GetValue(SelectionHighlightSizeProperty);
        set => SetValue(SelectionHighlightSizeProperty, value);
    }

    /// <summary>是否显示鼠标十字准线与坐标读数。</summary>
    public bool IsCrosshairEnabled
    {
        get => GetValue(IsCrosshairEnabledProperty);
        set => SetValue(IsCrosshairEnabledProperty, value);
    }

    /// <summary>十字准线画刷。</summary>
    public IBrush? CrosshairBrush
    {
        get => GetValue(CrosshairBrushProperty);
        set => SetValue(CrosshairBrushProperty, value);
    }

    /// <summary>是否显示十字准线的坐标读数框。</summary>
    public bool ShowCrosshairLabels
    {
        get => GetValue(ShowCrosshairLabelsProperty);
        set => SetValue(ShowCrosshairLabelsProperty, value);
    }

    /// <summary>
    /// 悬停提示文本格式化回调；返回 null / 空字符串时依次回退到数据点 Tag 与坐标文本。
    /// </summary>
    public Func<PlotPointInfo, string?>? HoverTextFormatter
    {
        get => _hoverTextFormatter;
        set
        {
            _hoverTextFormatter = value;
            InvalidateVisual();
        }
    }

    /// <summary>当前悬停命中的数据点；未命中时为 null。</summary>
    public PlotPointInfo? HoveredPoint => _hoverPoint;

    /// <summary>当前选中的数据点；未选中时为 null。</summary>
    public PlotPointInfo? SelectedPoint => _selectedPoint;

    internal InteractionOverlayRenderer InteractionRenderer => _interactionRenderer ??= new InteractionOverlayRenderer(this);

    /// <summary>以当前命中结果选中数据点并触发 <see cref="PointSelected"/>。</summary>
    private void SelectPointCore(in PlotPointInfo info)
    {
        if (_selectedPoint is { } current &&
            current.Series == info.Series &&
            current.PointIndex == info.PointIndex &&
            current.Position == info.Position)
        {
            return;
        }

        _selectedPoint = info;
        PointSelected?.Invoke(this, new PlotPointEventArgs(info));
        InvalidateVisual();
    }

    /// <summary>通过系列与索引选中数据点；索引无效时返回 false。</summary>
    public bool SelectPoint(PlotSeries3D series, int pointIndex)
    {
        if (series is not PointSeries3D points || pointIndex < 0 || pointIndex >= points.Count)
        {
            return false;
        }

        SelectPointCore(new PlotPointInfo(series, pointIndex, points[pointIndex], points.GetTag(pointIndex)));
        return true;
    }

    /// <summary>清除当前选中的点，并触发 <see cref="PointDeselected"/>。</summary>
    public void ClearSelection()
    {
        if (_selectedPoint is not { } selected)
        {
            return;
        }

        _selectedPoint = null;
        PointDeselected?.Invoke(this, new PlotPointEventArgs(selected));
        InvalidateVisual();
    }

    /// <summary>删除当前选中的点（若存在），并触发 <see cref="PointDeselected"/>。</summary>
    public bool DeleteSelectedPoint()
    {
        if (_selectedPoint is not { } selected || selected.Series is not PointSeries3D points)
        {
            return false;
        }

        _selectedPoint = null;
        var removed = points.RemoveAt(selected.PointIndex);
        PointDeselected?.Invoke(this, new PlotPointEventArgs(selected));
        InvalidateVisual();
        return removed;
    }

    /// <summary>命中测试：返回距离屏幕位置最近且位于半径内的数据点。</summary>
    /// <param name="position">屏幕坐标。</param>
    /// <param name="radius">命中半径（像素）。</param>
    /// <param name="info">命中的数据点。</param>
    public bool TryHitTest(Point position, double radius, out PlotPointInfo info)
    {
        info = default;
        var scene = GetInteractionScene();
        if (scene is null)
        {
            return false;
        }

        var safeRadius = SanitizeRadius(radius, 10);
        var best = double.MaxValue;
        var found = false;

        var series = Series;
        for (var i = 0; i < series.Count; i++)
        {
            var item = series[i];
            if (!item.IsVisible || item is not PointSeries3D pointSeries)
            {
                continue;
            }

            if (!pointSeries.TryFindNearestPoint(scene.Projection, position, out var index, out var distance))
            {
                continue;
            }

            if (distance > safeRadius || distance >= best)
            {
                continue;
            }

            best = distance;
            info = new PlotPointInfo(item, index, pointSeries[index], pointSeries.GetTag(index));
            found = true;
        }

        return found;
    }

    private void UpdateInteraction(Point position)
    {
        _pointerPosition = position;
        _pointerInside = true;

        PlotPointInfo? hover = null;
        if (IsPointHoverEnabled && TryHitTest(position, SanitizeRadius(HoverRadius, 10), out var hit))
        {
            hover = hit;
        }

        Point3D? crosshair = null;
        if (IsCrosshairEnabled && TryGetCrosshairWorld(position, out var world))
        {
            crosshair = world;
        }

        var hoverChanged = !SamePoint(hover, _hoverPoint);
        var crosshairChanged = !SamePoint(crosshair, _crosshairWorld);

        _hoverPoint = hover;
        _crosshairWorld = crosshair;

        UpdateHoverCursor(hover.HasValue);

        // 提示框 / 读数框跟随鼠标移动，只要可见就需要重绘。
        if (hoverChanged || crosshairChanged ||
            (hover is not null && IsHoverToolTipEnabled) ||
            (crosshair is not null && ShowCrosshairLabels))
        {
            InvalidateVisual();
        }
    }

    private void ClearInteraction()
    {
        var hadState = _pointerInside || _hoverPoint is not null || _crosshairWorld is not null;
        _pointerInside = false;
        _hoverPoint = null;
        _crosshairWorld = null;

        if (_dragMode == DragMode.None && !ReferenceEquals(Cursor, Cursor.Default))
        {
            Cursor = Cursor.Default;
        }

        if (hadState)
        {
            InvalidateVisual();
        }
    }

    private void UpdateHoverCursor(bool hasHover)
    {
        if (_dragMode != DragMode.None)
        {
            return;
        }

        var target = hasHover ? _hoverCursor ??= new Cursor(StandardCursorType.Hand) : Cursor.Default;
        if (!ReferenceEquals(Cursor, target))
        {
            Cursor = target;
        }
    }

    private bool TryGetCrosshairWorld(Point position, out Point3D world)
    {
        world = default;

        var scene = GetInteractionScene();
        if (scene is null)
        {
            return false;
        }

        var padding = PlotPadding;
        var plotRect = new Rect(
            padding.Left,
            padding.Top,
            Math.Max(0, Bounds.Width - padding.Left - padding.Right),
            Math.Max(0, Bounds.Height - padding.Top - padding.Bottom));

        if (plotRect.Width < 4 || plotRect.Height < 4 || !plotRect.Contains(position))
        {
            return false;
        }

        if (!scene.Projection.TryIntersectBounds(
                position.X,
                position.Y,
                scene.XTicks.Min,
                scene.YTicks.Min,
                scene.ZTicks.Min,
                scene.XTicks.Max,
                scene.YTicks.Max,
                scene.ZTicks.Max,
                out var hit,
                out var offset))
        {
            return false;
        }

        // 射线几乎擦过包围盒时交点会远离视图平面，隐藏准线与读数，避免显示极端坐标。
        if (offset > scene.Diagonal * CrosshairMaxOffsetFactor)
        {
            return false;
        }

        world = hit;
        return true;
    }

    private PlotScene? GetInteractionScene()
    {
        var size = Bounds.Size;
        if (!(size.Width > 1) || !(size.Height > 1))
        {
            return null;
        }

        return GetScene(size);
    }

    private void ValidateInteractionState(bool raiseEvent)
    {
        if (_selectedPoint is { } selected && !IsPointValid(selected))
        {
            _selectedPoint = null;
            if (raiseEvent)
            {
                PointDeselected?.Invoke(this, new PlotPointEventArgs(selected));
            }
        }

        if (_hoverPoint is { } hover && !IsPointValid(hover))
        {
            _hoverPoint = null;
        }
    }

    private bool IsPointValid(in PlotPointInfo info) =>
        info.Series.IsVisible &&
        Series.Contains(info.Series) &&
        info.Series is PointSeries3D points &&
        info.PointIndex >= 0 &&
        info.PointIndex < points.Count &&
        points[info.PointIndex] == info.Position;

    private void RenderInteractionOverlay(DrawingContext context, PlotScene scene)
    {
        // 渲染期只做静默兜底，不触发事件；PointDeselected 统一在数据 / 属性变化通知中抛出。
        ValidateInteractionState(raiseEvent: false);

        if (_hoverPoint is null && _selectedPoint is null && _crosshairWorld is null)
        {
            return;
        }

        string? tooltipText = null;
        if (IsPointHoverEnabled && IsHoverToolTipEnabled && _hoverPoint is { } hover)
        {
            tooltipText = BuildTooltipText(hover);
        }

        Point3D? crosshairWorld = null;
        string? readoutText = null;
        if (IsCrosshairEnabled && _pointerInside && _crosshairWorld is { } crosshair)
        {
            crosshairWorld = crosshair;
            if (ShowCrosshairLabels && tooltipText is null)
            {
                readoutText = BuildReadoutText(crosshair);
            }
        }

        InteractionRenderer.Render(
            context,
            scene,
            _hoverPoint,
            _selectedPoint,
            _pointerPosition,
            crosshairWorld,
            tooltipText,
            readoutText);
    }

    private string BuildTooltipText(in PlotPointInfo info)
    {
        if (_hoverTextFormatter is { } formatter)
        {
            var custom = formatter(info);
            if (!string.IsNullOrWhiteSpace(custom))
            {
                return custom!;
            }
        }

        if (info.Tag is { } tag)
        {
            var text = tag.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text!;
            }
        }

        return BuildReadoutText(info.Position);
    }

    private string BuildReadoutText(in Point3D point) =>
        $"X: {FormatXAxisValue(point.X)}\nY: {FormatYAxisValue(point.Y)}\nZ: {FormatZAxisValue(point.Z)}";

    private static double SanitizeRadius(double value, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, 1, 64) : fallback;

    private static bool SamePoint(PlotPointInfo? left, PlotPointInfo? right) =>
        left.HasValue == right.HasValue &&
        (!left.HasValue || left.Value.Equals(right!.Value));

    private static bool SamePoint(Point3D? left, Point3D? right) =>
        left.HasValue == right.HasValue &&
        (!left.HasValue || left!.Value == right!.Value);
}
