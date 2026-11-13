using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Plot3D.Avalonia.Modeling;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Controls;

/// <summary>
/// 科学计算风格的轻量三维绘图控件，支持折线 / 散点、轨道相机、网格、刻度、图例与自动配色，
/// 并提供数据点悬停提示、十字准线坐标读数、点选事件、右键菜单与 PNG 导出等交互能力。
/// </summary>
/// <remarks>
/// 控件完全使用 Avalonia 绘图 API 自绘，不依赖 SkiaSharp 或反射，可安全用于 NativeAOT / 裁剪环境。
/// </remarks>
public partial class Plot3DView : Control
{
    #region 颜色常量

    internal static readonly Color LightBackground = Color.Parse("#FFFFFF");
    internal static readonly Color DarkBackground = Color.Parse("#1E1E24");
    internal static readonly Color LightForeground = Color.Parse("#3C3C43");
    internal static readonly Color DarkForeground = Color.Parse("#E6E6EC");
    internal static readonly Color LightAxis = Color.Parse("#8E8E99");
    internal static readonly Color DarkAxis = Color.Parse("#70707C");
    internal static readonly Color LightGrid = Color.Parse("#DCDCE4");
    internal static readonly Color DarkGrid = Color.Parse("#45454F");
    internal static readonly Color LightPane = Color.Parse("#F7F7FA");
    internal static readonly Color DarkPane = Color.Parse("#25252E");
    internal static readonly Color LightLegendBackground = Color.FromArgb(225, 255, 255, 255);
    internal static readonly Color DarkLegendBackground = Color.FromArgb(225, 38, 38, 48);

    #endregion

    #region 属性注册

    /// <summary>数据系列集合。</summary>
    public static readonly StyledProperty<PlotSeries3DCollection?> SeriesProperty =
        AvaloniaProperty.Register<Plot3DView, PlotSeries3DCollection?>(nameof(Series));

    /// <summary>自动取色时使用的调色板。</summary>
    public static readonly StyledProperty<IReadOnlyList<Color>?> PaletteProperty =
        AvaloniaProperty.Register<Plot3DView, IReadOnlyList<Color>?>(nameof(Palette), PlotPalette.Tab10);

    /// <summary>背景画刷；为 null 时使用主题默认背景。</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(Background));

    /// <summary>文字画刷；为 null 时使用主题默认文字色。</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(Foreground));

    /// <summary>坐标轴 / 边框画刷；为 null 时使用主题默认颜色。</summary>
    public static readonly StyledProperty<IBrush?> AxisBrushProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(AxisBrush));

    /// <summary>网格线画刷；为 null 时使用主题默认颜色。</summary>
    public static readonly StyledProperty<IBrush?> GridBrushProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(GridBrush));

    /// <summary>背面墙填充画刷；为 null 时使用主题默认颜色。</summary>
    public static readonly StyledProperty<IBrush?> PaneBrushProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(PaneBrush));

    /// <summary>图例背景画刷；为 null 时使用主题默认颜色。</summary>
    public static readonly StyledProperty<IBrush?> LegendBackgroundProperty =
        AvaloniaProperty.Register<Plot3DView, IBrush?>(nameof(LegendBackground));

    /// <summary>浅色 / 深色配色主题，仅影响未显式设置的颜色。</summary>
    public static readonly StyledProperty<PlotTheme> ColorThemeProperty =
        AvaloniaProperty.Register<Plot3DView, PlotTheme>(nameof(ColorTheme));

    /// <summary>是否绘制包围盒边框。</summary>
    public static readonly StyledProperty<bool> ShowBoxProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowBox), true);

    /// <summary>是否绘制背景网格线。</summary>
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowGrid), true);

    /// <summary>是否填充远离相机的三个墙面。</summary>
    public static readonly StyledProperty<bool> ShowPaneProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowPane));

    /// <summary>是否绘制刻度线。</summary>
    public static readonly StyledProperty<bool> ShowTicksProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowTicks), true);

    /// <summary>是否绘制刻度文本。</summary>
    public static readonly StyledProperty<bool> ShowTickLabelsProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowTickLabels), true);

    /// <summary>是否绘制轴标题。</summary>
    public static readonly StyledProperty<bool> ShowAxisTitlesProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowAxisTitles), true);

    /// <summary>是否显示图例（只有设置了 Title 的系列会出现在图例中）。</summary>
    public static readonly StyledProperty<bool> ShowLegendProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ShowLegend));

    /// <summary>图例位置（控件四角 / 四边 / 居中）。</summary>
    public static readonly StyledProperty<LegendPosition> LegendPositionProperty =
        AvaloniaProperty.Register<Plot3DView, LegendPosition>(nameof(LegendPosition), LegendPosition.TopLeft);

    /// <summary>图例相对所选位置的偏移（像素）。</summary>
    public static readonly StyledProperty<Vector> LegendOffsetProperty =
        AvaloniaProperty.Register<Plot3DView, Vector>(nameof(LegendOffset));

    /// <summary>
    /// 是否让图例自动避让刻度文本与轴标题，默认开启。
    /// 开启后图例会尽量避开文字；控件过小、无可用空位时选择重叠最少的位置。
    /// </summary>
    public static readonly StyledProperty<bool> IsLegendAvoidanceEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsLegendAvoidanceEnabled), true);

    /// <summary>文字字体。</summary>
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<Plot3DView, FontFamily>(nameof(FontFamily), FontFamily.Default);

    /// <summary>文字字号。</summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(FontSize), 12.0);

    /// <summary>每个轴期望的刻度数量（含两端）。</summary>
    public static readonly StyledProperty<int> TickCountProperty =
        AvaloniaProperty.Register<Plot3DView, int>(nameof(TickCount), 6);

    /// <summary>X 轴标题。</summary>
    public static readonly StyledProperty<string?> XAxisTitleProperty =
        AvaloniaProperty.Register<Plot3DView, string?>(nameof(XAxisTitle), "X");

    /// <summary>Y 轴标题。</summary>
    public static readonly StyledProperty<string?> YAxisTitleProperty =
        AvaloniaProperty.Register<Plot3DView, string?>(nameof(YAxisTitle), "Y");

    /// <summary>Z 轴标题。</summary>
    public static readonly StyledProperty<string?> ZAxisTitleProperty =
        AvaloniaProperty.Register<Plot3DView, string?>(nameof(ZAxisTitle), "Z");

    /// <summary>
    /// X 轴左端数值；<c>NaN</c> 表示自动。
    /// “左端”是默认视角下 X 轴线位于屏幕左侧的一端；把较大的数值写到左端即可反转 X 轴，刻度自左向右递减。
    /// </summary>
    public static readonly StyledProperty<double> XAxisLeftProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(XAxisLeft), double.NaN);

    /// <summary>X 轴右端数值；<c>NaN</c> 表示自动。</summary>
    public static readonly StyledProperty<double> XAxisRightProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(XAxisRight), double.NaN);

    /// <summary>
    /// Y 轴左端数值；<c>NaN</c> 表示自动。默认视角下 Y 轴线自右下方向左上方延伸，
    /// 因此左端通常显示较大的数值；把较小的数值写到左端即可反转 Y 轴。
    /// </summary>
    public static readonly StyledProperty<double> YAxisLeftProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(YAxisLeft), double.NaN);

    /// <summary>Y 轴右端数值；<c>NaN</c> 表示自动。</summary>
    public static readonly StyledProperty<double> YAxisRightProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(YAxisRight), double.NaN);

    /// <summary>Z 轴底端数值；<c>NaN</c> 表示自动。</summary>
    public static readonly StyledProperty<double> ZAxisBottomProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(ZAxisBottom), double.NaN);

    /// <summary>Z 轴顶端数值；<c>NaN</c> 表示自动。底端大于顶端时 Z 轴反向显示。</summary>
    public static readonly StyledProperty<double> ZAxisTopProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(ZAxisTop), double.NaN);

    /// <summary>
    /// X 轴最小值（兼容旧版本，仍然作为可绑定 / 可样式化的 StyledProperty 保留，
    /// 变更时自动映射到 <see cref="XAxisLeftProperty" /> / <see cref="XAxisRightProperty" />）。
    /// </summary>
    public static readonly StyledProperty<double> XAxisMinimumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("XAxisMinimum", double.NaN);

    /// <summary>X 轴最大值（兼容旧版本，自动映射到左 / 右轴端）。</summary>
    public static readonly StyledProperty<double> XAxisMaximumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("XAxisMaximum", double.NaN);

    /// <summary>Y 轴最小值（兼容旧版本，自动映射到左 / 右轴端）。</summary>
    public static readonly StyledProperty<double> YAxisMinimumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("YAxisMinimum", double.NaN);

    /// <summary>Y 轴最大值（兼容旧版本，自动映射到左 / 右轴端）。</summary>
    public static readonly StyledProperty<double> YAxisMaximumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("YAxisMaximum", double.NaN);

    /// <summary>Z 轴最小值（兼容旧版本，自动映射到底 / 顶轴端）。</summary>
    public static readonly StyledProperty<double> ZAxisMinimumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("ZAxisMinimum", double.NaN);

    /// <summary>Z 轴最大值（兼容旧版本，自动映射到底 / 顶轴端）。</summary>
    public static readonly StyledProperty<double> ZAxisMaximumProperty =
        AvaloniaProperty.Register<Plot3DView, double>("ZAxisMaximum", double.NaN);

    /// <summary>
    /// 是否锁定三个坐标轴为等长显示（图形显示为正方体），默认 <c>false</c>。
    /// 开启后各轴只改变显示长度，刻度仍按用户设置 / 数据实际的数值范围排列。
    /// </summary>
    public static readonly StyledProperty<bool> IsAspectRatioLockedProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsAspectRatioLocked));

    /// <summary>相机方位角（度）。</summary>
    public static readonly StyledProperty<double> AzimuthProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(Azimuth), DefaultAzimuth);

    /// <summary>相机仰角（度，-90..90）。</summary>
    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(Elevation), DefaultElevation);

    /// <summary>缩放倍数。</summary>
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(Zoom), 1.0);

    /// <summary>投影方式。</summary>
    public static readonly StyledProperty<ProjectionMode> ProjectionProperty =
        AvaloniaProperty.Register<Plot3DView, ProjectionMode>(nameof(Projection));

    /// <summary>是否允许鼠标左键拖动旋转。</summary>
    public static readonly StyledProperty<bool> IsRotationEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsRotationEnabled), true);

    /// <summary>是否反转左右拖动时的旋转方向（向左拖动改为向右旋转）。</summary>
    public static readonly StyledProperty<bool> IsHorizontalRotationReversedProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsHorizontalRotationReversed));

    /// <summary>是否反转上下拖动时的旋转方向（向上拖动改为向下旋转）。</summary>
    public static readonly StyledProperty<bool> IsVerticalRotationReversedProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsVerticalRotationReversed));

    /// <summary>是否允许鼠标右键 / 中键拖动平移。</summary>
    public static readonly StyledProperty<bool> IsPanEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsPanEnabled), true);

    /// <summary>是否允许滚轮缩放。</summary>
    public static readonly StyledProperty<bool> IsZoomEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsZoomEnabled), true);

    /// <summary>是否双击重置视角。</summary>
    public static readonly StyledProperty<bool> ResetOnDoubleClickProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(ResetOnDoubleClick), true);

    /// <summary>鼠标旋转灵敏度（度 / 像素）。</summary>
    public static readonly StyledProperty<double> RotationSensitivityProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(RotationSensitivity), 0.5);

    /// <summary>滚轮缩放灵敏度（每格的指数增长量）。</summary>
    public static readonly StyledProperty<double> ZoomSensitivityProperty =
        AvaloniaProperty.Register<Plot3DView, double>(nameof(ZoomSensitivity), 0.15);

    /// <summary>绘图区域四周的留白（用于放置刻度与轴标题）。</summary>
    public static readonly StyledProperty<Thickness> PlotPaddingProperty =
        AvaloniaProperty.Register<Plot3DView, Thickness>(nameof(PlotPadding), new Thickness(56, 40, 56, 40));

    #endregion

    /// <summary>默认方位角。</summary>
    public const double DefaultAzimuth = -37.5;

    /// <summary>默认仰角。</summary>
    public const double DefaultElevation = 30.0;

    private readonly HashSet<PlotSeries3D> _hookedSeries = [];
    private FrameRenderer? _frameRenderer;

    private PlotScene? _scene;
    private Size _sceneSize;
    private bool _sceneDirty = true;
    private BoundingBox3D _dataBounds = BoundingBox3D.Empty;
    private bool _dataBoundsDirty = true;

    private double _panX;
    private double _panY;

    private IBrush? _backgroundBrushCache;
    private IBrush? _textBrushCache;
    private IBrush? _axisBrushCache;
    private IBrush? _gridBrushCache;
    private IBrush? _paneBrushCache;
    private IBrush? _legendBackgroundCache;

    /// <summary>创建控件。</summary>
    public Plot3DView()
    {
        ClipToBounds = true;
        _ = Series;
    }

    private FrameRenderer Renderer => _frameRenderer ??= new FrameRenderer(this);

    #region 属性访问器

    /// <summary>数据系列集合。修改集合会自动刷新控件。</summary>
    public PlotSeries3DCollection Series
    {
        get => GetValue(SeriesProperty) ?? InitializeSeries();
        set => SetValue(SeriesProperty, value ?? new PlotSeries3DCollection());
    }

    /// <summary>自动取色时使用的调色板，默认 <see cref="PlotPalette.Tab10"/>。</summary>
    public IReadOnlyList<Color>? Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    /// <summary>背景画刷；为 null 时使用主题默认背景。</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>文字画刷；为 null 时使用主题默认文字色。</summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>坐标轴 / 边框画刷。</summary>
    public IBrush? AxisBrush
    {
        get => GetValue(AxisBrushProperty);
        set => SetValue(AxisBrushProperty, value);
    }

    /// <summary>网格线画刷。</summary>
    public IBrush? GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>背面墙填充画刷。</summary>
    public IBrush? PaneBrush
    {
        get => GetValue(PaneBrushProperty);
        set => SetValue(PaneBrushProperty, value);
    }

    /// <summary>图例背景画刷。</summary>
    public IBrush? LegendBackground
    {
        get => GetValue(LegendBackgroundProperty);
        set => SetValue(LegendBackgroundProperty, value);
    }

    /// <summary>浅色 / 深色配色主题。</summary>
    public PlotTheme ColorTheme
    {
        get => GetValue(ColorThemeProperty);
        set => SetValue(ColorThemeProperty, value);
    }

    /// <summary>是否绘制包围盒边框。</summary>
    public bool ShowBox
    {
        get => GetValue(ShowBoxProperty);
        set => SetValue(ShowBoxProperty, value);
    }

    /// <summary>是否绘制背景网格线。</summary>
    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>是否填充远离相机的三个墙面。</summary>
    public bool ShowPane
    {
        get => GetValue(ShowPaneProperty);
        set => SetValue(ShowPaneProperty, value);
    }

    /// <summary>是否绘制刻度线。</summary>
    public bool ShowTicks
    {
        get => GetValue(ShowTicksProperty);
        set => SetValue(ShowTicksProperty, value);
    }

    /// <summary>是否绘制刻度文本。</summary>
    public bool ShowTickLabels
    {
        get => GetValue(ShowTickLabelsProperty);
        set => SetValue(ShowTickLabelsProperty, value);
    }

    /// <summary>是否绘制轴标题。</summary>
    public bool ShowAxisTitles
    {
        get => GetValue(ShowAxisTitlesProperty);
        set => SetValue(ShowAxisTitlesProperty, value);
    }

    /// <summary>是否显示图例。</summary>
    public bool ShowLegend
    {
        get => GetValue(ShowLegendProperty);
        set => SetValue(ShowLegendProperty, value);
    }

    /// <summary>图例位置，默认左上角。</summary>
    public LegendPosition LegendPosition
    {
        get => GetValue(LegendPositionProperty);
        set => SetValue(LegendPositionProperty, value);
    }

    /// <summary>图例相对所选位置的偏移（像素）。</summary>
    public Vector LegendOffset
    {
        get => GetValue(LegendOffsetProperty);
        set => SetValue(LegendOffsetProperty, value);
    }

    /// <summary>是否让图例自动避让刻度文本与轴标题，默认开启。</summary>
    public bool IsLegendAvoidanceEnabled
    {
        get => GetValue(IsLegendAvoidanceEnabledProperty);
        set => SetValue(IsLegendAvoidanceEnabledProperty, value);
    }

    /// <summary>文字字体。</summary>
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>文字字号。</summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>每个轴期望的刻度数量。</summary>
    public int TickCount
    {
        get => GetValue(TickCountProperty);
        set => SetValue(TickCountProperty, value);
    }

    /// <summary>X 轴标题。</summary>
    public string? XAxisTitle
    {
        get => GetValue(XAxisTitleProperty);
        set => SetValue(XAxisTitleProperty, value);
    }

    /// <summary>Y 轴标题。</summary>
    public string? YAxisTitle
    {
        get => GetValue(YAxisTitleProperty);
        set => SetValue(YAxisTitleProperty, value);
    }

    /// <summary>Z 轴标题。</summary>
    public string? ZAxisTitle
    {
        get => GetValue(ZAxisTitleProperty);
        set => SetValue(ZAxisTitleProperty, value);
    }

    /// <summary>
    /// X 轴左端数值（默认视角下 X 轴线在屏幕左侧的一端）；<c>NaN</c> 表示自动。
    /// 左端大于右端时 X 轴反向显示，刻度自左向右递减。
    /// </summary>
    public double XAxisLeft
    {
        get => GetValue(XAxisLeftProperty);
        set => SetValue(XAxisLeftProperty, value);
    }

    /// <summary>X 轴右端数值；<c>NaN</c> 表示自动。</summary>
    public double XAxisRight
    {
        get => GetValue(XAxisRightProperty);
        set => SetValue(XAxisRightProperty, value);
    }

    /// <summary>
    /// Y 轴左端数值；<c>NaN</c> 表示自动。默认视角下 Y 轴线自右下方向左上方延伸，
    /// 因此左端通常显示较大的数值。
    /// </summary>
    public double YAxisLeft
    {
        get => GetValue(YAxisLeftProperty);
        set => SetValue(YAxisLeftProperty, value);
    }

    /// <summary>Y 轴右端数值；<c>NaN</c> 表示自动。</summary>
    public double YAxisRight
    {
        get => GetValue(YAxisRightProperty);
        set => SetValue(YAxisRightProperty, value);
    }

    /// <summary>Z 轴底端数值；<c>NaN</c> 表示自动。</summary>
    public double ZAxisBottom
    {
        get => GetValue(ZAxisBottomProperty);
        set => SetValue(ZAxisBottomProperty, value);
    }

    /// <summary>Z 轴顶端数值；<c>NaN</c> 表示自动。底端大于顶端时 Z 轴反向显示。</summary>
    public double ZAxisTop
    {
        get => GetValue(ZAxisTopProperty);
        set => SetValue(ZAxisTopProperty, value);
    }

    /// <summary>X 轴最小值；保留为兼容旧版本的写法，建议改用 <see cref="XAxisLeft" /> / <see cref="XAxisRight" />。</summary>
    [Obsolete("请改用 XAxisLeft / XAxisRight：左端即 X 轴线在默认视角下的左端，把较大的值写到左端即可反转坐标轴。")]
    public double XAxisMinimum
    {
        get => GetValue(XAxisMinimumProperty);
        set => SetValue(XAxisMinimumProperty, value);
    }

    /// <summary>X 轴最大值；保留为兼容旧版本的写法，建议改用 <see cref="XAxisLeft" /> / <see cref="XAxisRight" />。</summary>
    [Obsolete("请改用 XAxisLeft / XAxisRight：右端即 X 轴线在默认视角下的右端，把较小的值写到右端即可反转坐标轴。")]
    public double XAxisMaximum
    {
        get => GetValue(XAxisMaximumProperty);
        set => SetValue(XAxisMaximumProperty, value);
    }

    /// <summary>Y 轴最小值；保留为兼容旧版本的写法，建议改用 <see cref="YAxisLeft" /> / <see cref="YAxisRight" />。</summary>
    [Obsolete("请改用 YAxisLeft / YAxisRight：默认视角下 Y 轴左端位于屏幕左上方，通常显示较大的数值。")]
    public double YAxisMinimum
    {
        get => GetValue(YAxisMinimumProperty);
        set => SetValue(YAxisMinimumProperty, value);
    }

    /// <summary>Y 轴最大值；保留为兼容旧版本的写法，建议改用 <see cref="YAxisLeft" /> / <see cref="YAxisRight" />。</summary>
    [Obsolete("请改用 YAxisLeft / YAxisRight：默认视角下 Y 轴左端位于屏幕左上方，通常显示较大的数值。")]
    public double YAxisMaximum
    {
        get => GetValue(YAxisMaximumProperty);
        set => SetValue(YAxisMaximumProperty, value);
    }

    /// <summary>Z 轴最小值；保留为兼容旧版本的写法，建议改用 <see cref="ZAxisBottom" /> / <see cref="ZAxisTop" />。</summary>
    [Obsolete("请改用 ZAxisBottom / ZAxisTop：底端位于坐标框下方，把较大的值写到底端即可反转 Z 轴。")]
    public double ZAxisMinimum
    {
        get => GetValue(ZAxisMinimumProperty);
        set => SetValue(ZAxisMinimumProperty, value);
    }

    /// <summary>Z 轴最大值；保留为兼容旧版本的写法，建议改用 <see cref="ZAxisBottom" /> / <see cref="ZAxisTop" />。</summary>
    [Obsolete("请改用 ZAxisBottom / ZAxisTop：顶端位于坐标框上方，把较小的值写到顶端即可反转 Z 轴。")]
    public double ZAxisMaximum
    {
        get => GetValue(ZAxisMaximumProperty);
        set => SetValue(ZAxisMaximumProperty, value);
    }

    /// <summary>
    /// 是否锁定三个坐标轴为等长显示（图形显示为正方体）。
    /// 开启后各轴只改变显示长度，不改变刻度数值，适合各轴数量级悬殊的数据。
    /// </summary>
    public bool IsAspectRatioLocked
    {
        get => GetValue(IsAspectRatioLockedProperty);
        set => SetValue(IsAspectRatioLockedProperty, value);
    }

    /// <summary>相机方位角（度）。</summary>
    public double Azimuth
    {
        get => GetValue(AzimuthProperty);
        set => SetValue(AzimuthProperty, value);
    }

    /// <summary>相机仰角（度，-90..90）。</summary>
    public double Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    /// <summary>缩放倍数。</summary>
    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>投影方式。</summary>
    public ProjectionMode Projection
    {
        get => GetValue(ProjectionProperty);
        set => SetValue(ProjectionProperty, value);
    }

    /// <summary>是否允许鼠标左键拖动旋转。</summary>
    public bool IsRotationEnabled
    {
        get => GetValue(IsRotationEnabledProperty);
        set => SetValue(IsRotationEnabledProperty, value);
    }

    /// <summary>是否反转左右拖动时的旋转方向。</summary>
    public bool IsHorizontalRotationReversed
    {
        get => GetValue(IsHorizontalRotationReversedProperty);
        set => SetValue(IsHorizontalRotationReversedProperty, value);
    }

    /// <summary>是否反转上下拖动时的旋转方向。</summary>
    public bool IsVerticalRotationReversed
    {
        get => GetValue(IsVerticalRotationReversedProperty);
        set => SetValue(IsVerticalRotationReversedProperty, value);
    }

    /// <summary>是否允许鼠标拖动平移。</summary>
    public bool IsPanEnabled
    {
        get => GetValue(IsPanEnabledProperty);
        set => SetValue(IsPanEnabledProperty, value);
    }

    /// <summary>是否允许滚轮缩放。</summary>
    public bool IsZoomEnabled
    {
        get => GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    /// <summary>是否双击重置视角。</summary>
    public bool ResetOnDoubleClick
    {
        get => GetValue(ResetOnDoubleClickProperty);
        set => SetValue(ResetOnDoubleClickProperty, value);
    }

    /// <summary>鼠标旋转灵敏度。</summary>
    public double RotationSensitivity
    {
        get => GetValue(RotationSensitivityProperty);
        set => SetValue(RotationSensitivityProperty, value);
    }

    /// <summary>滚轮缩放灵敏度。</summary>
    public double ZoomSensitivity
    {
        get => GetValue(ZoomSensitivityProperty);
        set => SetValue(ZoomSensitivityProperty, value);
    }

    /// <summary>绘图区域四周的留白。</summary>
    public Thickness PlotPadding
    {
        get => GetValue(PlotPaddingProperty);
        set => SetValue(PlotPaddingProperty, value);
    }

    /// <summary>当前水平平移量（像素）。</summary>
    public double PanX => _panX;

    /// <summary>当前垂直平移量（像素）。</summary>
    public double PanY => _panY;

    #endregion

    #region 渲染

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 360 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 280 : availableSize.Height;
        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Bounds.Size;
        if (!(size.Width > 1) || !(size.Height > 1))
        {
            return;
        }

        var scene = GetScene(size);
        if (!IsBackgroundTransparent)
        {
            context.FillRectangle(ResolveBackgroundBrush(), new Rect(size));
        }

        Renderer.Render(context, scene);

        var series = Series;
        var viewBounds = new Rect(size);
        for (var i = 0; i < series.Count; i++)
        {
            var item = series[i];
            if (!item.IsVisible)
            {
                continue;
            }

            var seriesContext = new SeriesRenderContext(
                context,
                scene.Projection,
                ResolveSeriesColor(item, i),
                item.Opacity,
                viewBounds);

            item.Render(seriesContext);
        }

        // 刻度文本与轴标题最后绘制，避免被数据曲线遮挡。
        Renderer.RenderLabels(context, scene);

        if (ShowLegend)
        {
            DrawLegend(context, scene);
        }

        if (!_isExporting)
        {
            RenderInteractionOverlay(context, scene);
        }
    }

    private void DrawLegend(DrawingContext context, PlotScene scene)
    {
        List<(string Text, Color Color)>? items = null;
        var series = Series;

        for (var i = 0; i < series.Count; i++)
        {
            var item = series[i];
            if (!item.IsVisible || string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            items ??= [];
            items.Add((item.Title!, ResolveSeriesColor(item, i)));
        }

        if (items is not null)
        {
            Renderer.DrawLegend(context, scene, items);
        }
    }

    private PlotScene GetScene(Size size)
    {
        if (_scene is not null && !_sceneDirty && _sceneSize == size)
        {
            return _scene;
        }

        if (_dataBoundsDirty)
        {
            _dataBounds = ComputeDataBounds();
            _dataBoundsDirty = false;
        }

        _scene = PlotSceneBuilder.Build(
            size,
            PlotPadding,
            _dataBounds,
            XAxisLeft,
            XAxisRight,
            YAxisLeft,
            YAxisRight,
            ZAxisBottom,
            ZAxisTop,
            TickCount,
            ResolveTickDecimals(XAxisTickDecimals),
            ResolveTickDecimals(YAxisTickDecimals),
            ResolveTickDecimals(ZAxisTickDecimals),
            XAxisValueFormat,
            YAxisValueFormat,
            ZAxisValueFormat,
            AxisTimeMilliseconds,
            Azimuth,
            Elevation,
            Zoom,
            _panX,
            _panY,
            Projection == ProjectionMode.Perspective,
            IsAspectRatioLocked);

        _sceneSize = size;
        _sceneDirty = false;

        return _scene;
    }

    private BoundingBox3D ComputeDataBounds()
    {
        var bounds = BoundingBox3D.Empty;
        var series = Series;

        for (var i = 0; i < series.Count; i++)
        {
            var item = series[i];
            if (!item.IsVisible)
            {
                continue;
            }

            bounds = bounds.Union(item.GetBounds());
        }

        return bounds;
    }

    private Color ResolveSeriesColor(PlotSeries3D series, int index)
    {
        switch (series.ColorMode)
        {
            case SeriesColorMode.Fixed:
                return series.Color;

            case SeriesColorMode.Random:
                return series.ResolveRandomColor();

            default:
                if (series.IsSet(PlotSeries3D.ColorProperty))
                {
                    return series.Color;
                }

                var palette = Palette;
                if (palette is { Count: > 0 })
                {
                    return palette[index % palette.Count];
                }

                return series.Color;
        }
    }

    #endregion

    #region 画刷解析

    internal IBrush ResolveBackgroundBrush() =>
        _backgroundBrushCache ??= Background ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkBackground : LightBackground);

    internal IBrush ResolveTextBrush() =>
        _textBrushCache ??= Foreground ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkForeground : LightForeground);

    internal IBrush ResolveAxisBrush() =>
        _axisBrushCache ??= AxisBrush ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkAxis : LightAxis);

    internal IBrush ResolveGridBrush() =>
        _gridBrushCache ??= GridBrush ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkGrid : LightGrid);

    internal IBrush ResolvePaneBrush() =>
        _paneBrushCache ??= PaneBrush ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkPane : LightPane);

    internal IBrush ResolveLegendBackground() =>
        _legendBackgroundCache ??= LegendBackground ??
            new ImmutableSolidColorBrush(ColorTheme == PlotTheme.Dark ? DarkLegendBackground : LightLegendBackground);

    private void InvalidateBrushes()
    {
        _backgroundBrushCache = null;
        _textBrushCache = null;
        _axisBrushCache = null;
        _gridBrushCache = null;
        _paneBrushCache = null;
        _legendBackgroundCache = null;
        _frameRenderer?.ResetCache();
        _interactionRenderer?.ResetCache();
    }

    #endregion

    #region 失效与订阅

    /// <summary>数据变化后调用：重新计算自动坐标范围并刷新。</summary>
    public void InvalidatePlot()
    {
        _dataBoundsDirty = true;
        _sceneDirty = true;
        InvalidateVisual();
    }

    private void InvalidateScene()
    {
        _sceneDirty = true;
        InvalidateVisual();
    }

    private PlotSeries3DCollection InitializeSeries()
    {
        var collection = new PlotSeries3DCollection();
        SetCurrentValue(SeriesProperty, collection);
        return collection;
    }

    private static bool IsVisualStyleProperty(AvaloniaProperty property) =>
        property == BackgroundProperty ||
        property == ForegroundProperty ||
        property == AxisBrushProperty ||
        property == GridBrushProperty ||
        property == PaneBrushProperty ||
        property == LegendBackgroundProperty ||
        property == ToolTipBackgroundProperty ||
        property == ToolTipBorderBrushProperty ||
        property == ToolTipForegroundProperty ||
        property == CrosshairBrushProperty ||
        property == ColorThemeProperty ||
        property == FontFamilyProperty ||
        property == FontSizeProperty;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SeriesProperty)
        {
            AttachCollection(change.OldValue as PlotSeries3DCollection, change.NewValue as PlotSeries3DCollection);
            ValidateInteractionState(raiseEvent: true);
            InvalidatePlot();
            return;
        }

        if (IsVisualStyleProperty(change.Property))
        {
            InvalidateBrushes();
        }

        // 旧版最小 / 最大值属性仍为 StyledProperty（可绑定 / 可样式化 / 可动画），
        // 值变化时映射到新的左 / 右（下 / 上）轴端属性。
        if (change.Property == XAxisMinimumProperty)
        {
            ApplyLegacyXAxisMinimum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        if (change.Property == XAxisMaximumProperty)
        {
            ApplyLegacyXAxisMaximum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        if (change.Property == YAxisMinimumProperty)
        {
            ApplyLegacyYAxisMinimum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        if (change.Property == YAxisMaximumProperty)
        {
            ApplyLegacyYAxisMaximum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        if (change.Property == ZAxisMinimumProperty)
        {
            ApplyLegacyZAxisMinimum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        if (change.Property == ZAxisMaximumProperty)
        {
            ApplyLegacyZAxisMaximum(change.NewValue is double value ? value : double.NaN);
            return;
        }

        // 关闭交互开关时立即清除对应的临时状态，避免残留高亮 / 准线。
        if (change.Property == IsPointHoverEnabledProperty)
        {
            _hoverPoint = null;
        }

        if (change.Property == IsCrosshairEnabledProperty)
        {
            _crosshairWorld = null;
        }

        InvalidateScene();
    }

    #region 旧版轴范围属性映射

    // 旧版最小 / 最大值属性仍可被绑定、样式和动画引用（StyledProperty），
    // 写入时按“当前较小 / 较大端”映射到新的轴端，保持 1.x 的兼容行为。
    private void ApplyLegacyXAxisMinimum(double value)
    {
        if (double.IsFinite(XAxisLeft) && double.IsFinite(XAxisRight) && XAxisLeft > XAxisRight)
        {
            SetCurrentValue(XAxisRightProperty, value);
        }
        else
        {
            SetCurrentValue(XAxisLeftProperty, value);
        }
    }

    private void ApplyLegacyXAxisMaximum(double value)
    {
        if (double.IsFinite(XAxisLeft) && double.IsFinite(XAxisRight) && XAxisLeft > XAxisRight)
        {
            SetCurrentValue(XAxisLeftProperty, value);
        }
        else
        {
            SetCurrentValue(XAxisRightProperty, value);
        }
    }

    private void ApplyLegacyYAxisMinimum(double value)
    {
        // Y 轴默认的较小端是右端（右下方），反向轴时改写在左端。
        if (double.IsFinite(YAxisLeft) && double.IsFinite(YAxisRight) && YAxisLeft <= YAxisRight)
        {
            SetCurrentValue(YAxisLeftProperty, value);
        }
        else
        {
            SetCurrentValue(YAxisRightProperty, value);
        }
    }

    private void ApplyLegacyYAxisMaximum(double value)
    {
        if (double.IsFinite(YAxisLeft) && double.IsFinite(YAxisRight) && YAxisLeft <= YAxisRight)
        {
            SetCurrentValue(YAxisRightProperty, value);
        }
        else
        {
            SetCurrentValue(YAxisLeftProperty, value);
        }
    }

    private void ApplyLegacyZAxisMinimum(double value)
    {
        if (double.IsFinite(ZAxisBottom) && double.IsFinite(ZAxisTop) && ZAxisBottom > ZAxisTop)
        {
            SetCurrentValue(ZAxisTopProperty, value);
        }
        else
        {
            SetCurrentValue(ZAxisBottomProperty, value);
        }
    }

    private void ApplyLegacyZAxisMaximum(double value)
    {
        if (double.IsFinite(ZAxisBottom) && double.IsFinite(ZAxisTop) && ZAxisBottom > ZAxisTop)
        {
            SetCurrentValue(ZAxisBottomProperty, value);
        }
        else
        {
            SetCurrentValue(ZAxisTopProperty, value);
        }
    }

    #endregion

    private void AttachCollection(PlotSeries3DCollection? oldValue, PlotSeries3DCollection? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.CollectionChanged -= OnSeriesCollectionChanged;
            foreach (var item in oldValue)
            {
                Unhook(item);
            }
        }

        _hookedSeries.Clear();

        if (newValue is not null)
        {
            newValue.CollectionChanged += OnSeriesCollectionChanged;
            foreach (var item in newValue)
            {
                Hook(item);
            }
        }
    }

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSubscriptions();
        ValidateInteractionState(raiseEvent: true);
        InvalidatePlot();
    }

    private void SyncSubscriptions()
    {
        var series = Series;

        List<PlotSeries3D>? removed = null;
        foreach (var item in _hookedSeries)
        {
            if (!series.Contains(item))
            {
                (removed ??= []).Add(item);
            }
        }

        if (removed is not null)
        {
            foreach (var item in removed)
            {
                Unhook(item);
                _hookedSeries.Remove(item);
            }
        }

        foreach (var item in series)
        {
            if (_hookedSeries.Add(item))
            {
                Hook(item);
            }
        }
    }

    private void Hook(PlotSeries3D series)
    {
        series.PropertyChanged += OnSeriesPropertyChanged;
        series.DataChanged += OnSeriesDataChanged;
    }

    private void Unhook(PlotSeries3D series)
    {
        series.PropertyChanged -= OnSeriesPropertyChanged;
        series.DataChanged -= OnSeriesDataChanged;
    }

    private void OnSeriesPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // IsVisible / 数据相关属性变化后立即校验悬停与选中状态，避免高亮残留在已隐藏的系列上。
        ValidateInteractionState(raiseEvent: true);
        InvalidatePlot();
    }

    private void OnSeriesDataChanged(object? sender, EventArgs e)
    {
        ValidateInteractionState(raiseEvent: true);
        InvalidatePlot();
    }

    #endregion
}
