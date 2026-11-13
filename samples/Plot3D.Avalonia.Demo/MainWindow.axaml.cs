using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Demo;

public partial class MainWindow : Window
{
    private enum RenderMode
    {
        Line,
        Scatter,
        LineWithMarkers,
    }

    private readonly Plot3DView _plot;
    private readonly Border _plotCard;
    private readonly Border _sideCard;
    private readonly CheckBox _gridCheck;
    private readonly CheckBox _boxCheck;
    private readonly CheckBox _paneCheck;
    private readonly CheckBox _ticksCheck;
    private readonly CheckBox _axisTitleCheck;
    private readonly CheckBox _legendCheck;
    private readonly CheckBox _aspectCheck;
    private readonly CheckBox _hoverCheck;
    private readonly CheckBox _crosshairCheck;
    private readonly CheckBox _selectCheck;
    private readonly CheckBox _latestCheck;
    private readonly CheckBox _transparentCheck;
    private readonly CheckBox _msCheck;
    private readonly CheckBox _extremeCheck;
    private readonly CheckBox _reverseAxisCheck;
    private readonly CheckBox _legendOffsetCheck;
    private readonly CheckBox _legendAvoidCheck;
    private readonly CheckBox _horizontalReverseCheck;
    private readonly CheckBox _verticalReverseCheck;
    private readonly ComboBox _legendPositionCombo;
    private readonly ComboBox _themeCombo;
    private readonly ComboBox _projectionCombo;
    private readonly ComboBox _paletteCombo;
    private readonly ComboBox _axisFormatCombo;
    private readonly ComboBox _decimalsCombo;
    private readonly RadioButton _lineMode;
    private readonly RadioButton _scatterMode;
    private readonly RadioButton _bothMode;
    private readonly Slider _tickSlider;
    private readonly TextBlock _selectionText;

    private bool _ready;
    private RenderMode _mode = RenderMode.Line;
    private bool _randomColors;

    public MainWindow()
    {
        InitializeComponent();

        _plot = this.FindControl<Plot3DView>("Plot")!;
        _plotCard = this.FindControl<Border>("PlotCard")!;
        _sideCard = this.FindControl<Border>("SideCard")!;
        _gridCheck = this.FindControl<CheckBox>("GridCheck")!;
        _boxCheck = this.FindControl<CheckBox>("BoxCheck")!;
        _paneCheck = this.FindControl<CheckBox>("PaneCheck")!;
        _ticksCheck = this.FindControl<CheckBox>("TicksCheck")!;
        _axisTitleCheck = this.FindControl<CheckBox>("AxisTitleCheck")!;
        _legendCheck = this.FindControl<CheckBox>("LegendCheck")!;
        _aspectCheck = this.FindControl<CheckBox>("AspectCheck")!;
        _hoverCheck = this.FindControl<CheckBox>("HoverCheck")!;
        _crosshairCheck = this.FindControl<CheckBox>("CrosshairCheck")!;
        _selectCheck = this.FindControl<CheckBox>("SelectCheck")!;
        _latestCheck = this.FindControl<CheckBox>("LatestCheck")!;
        _transparentCheck = this.FindControl<CheckBox>("TransparentCheck")!;
        _msCheck = this.FindControl<CheckBox>("MsCheck")!;
        _extremeCheck = this.FindControl<CheckBox>("ExtremeCheck")!;
        _reverseAxisCheck = this.FindControl<CheckBox>("ReverseAxisCheck")!;
        _legendOffsetCheck = this.FindControl<CheckBox>("LegendOffsetCheck")!;
        _legendAvoidCheck = this.FindControl<CheckBox>("LegendAvoidCheck")!;
        _horizontalReverseCheck = this.FindControl<CheckBox>("HorizontalReverseCheck")!;
        _verticalReverseCheck = this.FindControl<CheckBox>("VerticalReverseCheck")!;
        _legendPositionCombo = this.FindControl<ComboBox>("LegendPositionCombo")!;
        _themeCombo = this.FindControl<ComboBox>("ThemeCombo")!;
        _projectionCombo = this.FindControl<ComboBox>("ProjectionCombo")!;
        _paletteCombo = this.FindControl<ComboBox>("PaletteCombo")!;
        _axisFormatCombo = this.FindControl<ComboBox>("AxisFormatCombo")!;
        _decimalsCombo = this.FindControl<ComboBox>("DecimalsCombo")!;
        _lineMode = this.FindControl<RadioButton>("LineMode")!;
        _scatterMode = this.FindControl<RadioButton>("ScatterMode")!;
        _bothMode = this.FindControl<RadioButton>("BothMode")!;
        _tickSlider = this.FindControl<Slider>("TickSlider")!;
        _selectionText = this.FindControl<TextBlock>("SelectionText")!;

        _plot.PointSelected += OnPointSelected;
        _plot.PointDeselected += OnPointDeselected;

        AddCustomContextMenuItems();

        _ready = true;
        ApplyPalette(0);
        ApplyInteractionSettings();
        BuildSeries();
        ApplyStartupArguments();
    }

    /// <summary>演示如何向控件追加自定义右键菜单项。</summary>
    private void AddCustomContextMenuItems()
    {
        var resetItem = new MenuItem { Header = "重置视角（自定义菜单项）" };
        resetItem.Click += (_, _) => _plot.ResetView();
        _plot.ContextMenuItems.Add(resetItem);
    }

    /// <summary>支持 --dark / --perspective / --scatter / --time 启动参数，便于快速预览不同外观。</summary>
    private void ApplyStartupArguments()
    {
        var args = Environment.GetCommandLineArgs();

        if (Array.Exists(args, a => string.Equals(a, "--dark", StringComparison.OrdinalIgnoreCase)))
        {
            _themeCombo.SelectedIndex = 1;
        }

        if (Array.Exists(args, a => string.Equals(a, "--perspective", StringComparison.OrdinalIgnoreCase)))
        {
            _projectionCombo.SelectedIndex = 1;
        }

        if (Array.Exists(args, a => string.Equals(a, "--scatter", StringComparison.OrdinalIgnoreCase)))
        {
            _scatterMode.IsChecked = true;
        }

        if (Array.Exists(args, a => string.Equals(a, "--time", StringComparison.OrdinalIgnoreCase)))
        {
            _axisFormatCombo.SelectedIndex = 3;
        }
    }

    private void OnToggle(object? sender, RoutedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.ShowGrid = _gridCheck.IsChecked == true;
        _plot.ShowBox = _boxCheck.IsChecked == true;
        _plot.ShowPane = _paneCheck.IsChecked == true;

        var ticks = _ticksCheck.IsChecked == true;
        _plot.ShowTicks = ticks;
        _plot.ShowTickLabels = ticks;

        _plot.ShowAxisTitles = _axisTitleCheck.IsChecked == true;
        _plot.ShowLegend = _legendCheck.IsChecked == true;
        _plot.IsAspectRatioLocked = _aspectCheck.IsChecked == true;

        _plot.LegendOffset = _legendOffsetCheck.IsChecked == true
            ? new Vector(12, 8)
            : default;
        _plot.IsLegendAvoidanceEnabled = _legendAvoidCheck.IsChecked == true;

        _plot.IsHorizontalRotationReversed = _horizontalReverseCheck.IsChecked == true;
        _plot.IsVerticalRotationReversed = _verticalReverseCheck.IsChecked == true;

        ApplyInteractionSettings();
        ApplyTransparentBackground();
    }

    private void OnLegendPositionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.LegendPosition = (LegendPosition)_legendPositionCombo.SelectedIndex;
    }

    private void OnReverseAxisChanged(object? sender, RoutedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        BuildSeries();
    }

    private void ApplyInteractionSettings()
    {
        _plot.IsPointHoverEnabled = _hoverCheck.IsChecked == true;
        _plot.IsCrosshairEnabled = _crosshairCheck.IsChecked == true;
        _plot.IsPointSelectionEnabled = _selectCheck.IsChecked == true;

        var highlightLatest = _latestCheck.IsChecked == true;
        foreach (var series in _plot.Series)
        {
            if (series is PointSeries3D points)
            {
                points.HighlightLatestPoint = highlightLatest;
            }
        }
    }

    private void ApplyTransparentBackground()
    {
        var transparent = _transparentCheck.IsChecked == true;
        _plot.IsBackgroundTransparent = transparent;

        if (!transparent)
        {
            var dark = _themeCombo.SelectedIndex == 1;
            _plotCard.Background = new SolidColorBrush(dark ? Color.Parse("#1E1E24") : Colors.White);
            return;
        }

        // 用一个渐变背景衬托“透明背景”，确认控件确实没有绘制自己的底色。
        var darkTheme = _themeCombo.SelectedIndex == 1;
        _plotCard.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(darkTheme ? Color.Parse("#12304F") : Color.Parse("#DDEBFF"), 0),
                new GradientStop(darkTheme ? Color.Parse("#3A1430") : Color.Parse("#FFE1F1"), 1),
            },
        };
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        var dark = _themeCombo.SelectedIndex == 1;
        _plot.ColorTheme = dark ? PlotTheme.Dark : PlotTheme.Light;

        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        var windowColor = dark ? Color.Parse("#12141A") : Color.Parse("#EEF1F5");
        var cardColor = dark ? Color.Parse("#1E1E24") : Colors.White;
        Background = new SolidColorBrush(windowColor);
        _sideCard.Background = new SolidColorBrush(cardColor);
        ApplyTransparentBackground();
    }

    private void OnProjectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.Projection = _projectionCombo.SelectedIndex == 1
            ? ProjectionMode.Perspective
            : ProjectionMode.Orthographic;
    }

    private void OnPaletteChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        ApplyPalette(_paletteCombo.SelectedIndex);
        _randomColors = false;

        foreach (var series in _plot.Series)
        {
            series.ColorMode = SeriesColorMode.Auto;
        }
    }

    private void ApplyPalette(int index)
    {
        _plot.Palette = index switch
        {
            1 => PlotPalette.Classic,
            2 => PlotPalette.Set1,
            3 => PlotPalette.Pastel,
            4 => PlotPalette.Vivid,
            _ => PlotPalette.Tab10,
        };
    }

    private void OnModeChanged(object? sender, RoutedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _mode = _lineMode.IsChecked == true
            ? RenderMode.Line
            : _scatterMode.IsChecked == true
                ? RenderMode.Scatter
                : RenderMode.LineWithMarkers;

        BuildSeries();
    }

    private void OnAxisFormatChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.XAxisValueFormat = _axisFormatCombo.SelectedIndex switch
        {
            1 => AxisValueFormat.Date,
            2 => AxisValueFormat.Time,
            3 => AxisValueFormat.DateTime,
            _ => AxisValueFormat.Numeric,
        };

        _plot.AxisTimeMilliseconds = _msCheck.IsChecked == true;
        BuildSeries();
    }

    private void OnMillisecondsChanged(object? sender, RoutedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.AxisTimeMilliseconds = _msCheck.IsChecked == true;
    }

    private void OnDecimalsChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        var decimals = _decimalsCombo.SelectedIndex switch
        {
            1 => 0d,
            2 => 1d,
            3 => 2d,
            4 => 3d,
            5 => 4d,
            6 => 6d,
            _ => double.NaN,
        };

        _plot.XAxisTickDecimals = decimals;
        _plot.YAxisTickDecimals = decimals;
        _plot.ZAxisTickDecimals = decimals;
    }

    private void OnPointSelected(object? sender, PlotPointEventArgs e)
    {
        var tag = e.Point.Tag?.ToString();
        _selectionText.Text = string.IsNullOrWhiteSpace(tag)
            ? $"已选中：{e.Point.Series.Title} 第 {e.Point.PointIndex} 点"
            : $"已选中：{e.Point.Series.Title} 第 {e.Point.PointIndex} 点\n{tag}";
    }

    private void OnPointDeselected(object? sender, PlotPointEventArgs e) =>
        _selectionText.Text = "未选中任何数据点";

    private void OnDeleteSelected(object? sender, RoutedEventArgs e) => _plot.DeleteSelectedPoint();

    private void OnTickCountChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        _plot.TickCount = (int)Math.Round(e.NewValue);
    }

    private void OnRandomColors(object? sender, RoutedEventArgs e)
    {
        _randomColors = true;

        foreach (var series in _plot.Series)
        {
            series.ColorMode = SeriesColorMode.Random;
        }
    }

    private void OnRegenerate(object? sender, RoutedEventArgs e) => BuildSeries();

    /// <summary>切换“量级悬殊示例”：锁定等长轴，并把三轴设为 0.1~0.2 / 40~41 / 0~20000。</summary>
    private void OnExtremeChanged(object? sender, RoutedEventArgs e)
    {
        if (!_ready)
        {
            return;
        }

        if (_extremeCheck.IsChecked == true)
        {
            _aspectCheck.IsChecked = true;
            _plot.IsAspectRatioLocked = true;
        }

        BuildSeries();
    }

    private void OnResetView(object? sender, RoutedEventArgs e) => _plot.ResetView();

    private void BuildSeries()
    {
        _plot.Series.Clear();
        _selectionText.Text = "未选中任何数据点";

        var extreme = _extremeCheck.IsChecked == true;
        var format = _plot.XAxisValueFormat;
        var timeMode = !extreme && format != AxisValueFormat.Numeric;

        // 时间轴以“天”为单位，与普通数值轴数量级相差极大；锁定等长轴后 X 轴才有可见长度。
        if (timeMode)
        {
            _aspectCheck.IsChecked = true;
            _plot.IsAspectRatioLocked = true;
        }

        ApplyAxisRanges(extreme, !timeMode && _reverseAxisCheck.IsChecked == true);

        var count = timeMode ? 3 : 5;

        _plot.XAxisTitle = timeMode ? "时间" : "X";
        _plot.PlotPadding = timeMode ? new Thickness(84, 40, 84, 40) : new Thickness(52, 34, 52, 34);

        for (var index = 0; index < count; index++)
        {
            var points = extreme
                ? CreateExtremePoints(index)
                : timeMode ? CreateTimePoints(index, format) : CreateSpiralPoints(index);
            var tags = extreme
                ? CreateExtremeTags(points, index)
                : timeMode ? CreateTimeTags(points, index, format) : CreateSpiralTags(points, index);

            PointSeries3D series = _mode switch
            {
                RenderMode.Scatter => new ScatterSeries3D
                {
                    Title = timeMode ? $"传感器 {index + 1}" : $"散点 {index + 1}",
                    MarkerSize = timeMode ? 5 : 3.5,
                    MarkerShape = MarkerShape.Circle,
                },
                RenderMode.LineWithMarkers => new LineSeries3D
                {
                    Title = timeMode ? $"传感器 {index + 1}" : $"曲线 {index + 1}",
                    StrokeThickness = 1.4,
                    MarkerSize = timeMode ? 4 : 3,
                    MarkerShape = MarkerShape.Circle,
                },
                _ => new LineSeries3D
                {
                    Title = timeMode ? $"传感器 {index + 1}" : $"曲线 {index + 1}",
                    StrokeThickness = 1.6,
                },
            };

            if (_randomColors)
            {
                series.ColorMode = SeriesColorMode.Random;
            }

            series.HighlightLatestPoint = _latestCheck.IsChecked == true;
            series.LatestPointColor = Color.Parse("#FF3B30");
            series.LatestPointSize = 0;
            series.SetPoints(points, tags);

            _plot.Series.Add(series);
        }
    }

    /// <summary>
    /// 设置轴两端的数值。名称中的“左 / 右（下 / 上）”指默认视角下屏幕上看到的轴两端：
    /// X / Z 默认左（下）端为较小值，Y 轴默认左端（左上方）为较大值。
    /// </summary>
    private void ApplyAxisRanges(bool extreme, bool reverse)
    {
        if (extreme)
        {
            // 量级悬殊示例：X 0.1~0.2、Y 40~41（左端较大）、Z 0~20000。
            _plot.XAxisLeft = 0.1;
            _plot.XAxisRight = 0.2;
            _plot.YAxisLeft = 41;
            _plot.YAxisRight = 40;
            _plot.ZAxisBottom = 0;
            _plot.ZAxisTop = 20000;
            return;
        }

        if (reverse)
        {
            // 反向示例：X 轴左端为 1（自右向左递减）、Z 轴底端为 2（自顶向下递减）。
            _plot.XAxisLeft = 1;
            _plot.XAxisRight = -1;
            _plot.YAxisLeft = double.NaN;
            _plot.YAxisRight = double.NaN;
            _plot.ZAxisBottom = 2;
            _plot.ZAxisTop = 0;
            return;
        }

        _plot.XAxisLeft = double.NaN;
        _plot.XAxisRight = double.NaN;
        _plot.YAxisLeft = double.NaN;
        _plot.YAxisRight = double.NaN;
        _plot.ZAxisBottom = double.NaN;
        _plot.ZAxisTop = double.NaN;
    }

    /// <summary>量级悬殊示例数据：X 0.1~0.2、Y 40~41、Z 0~20000。</summary>
    private static Point3D[] CreateExtremePoints(int index)
    {
        const int count = 480;
        var points = new Point3D[count];
        var phase = index * Math.PI * 2.0 / 5.0;

        for (var i = 0; i < count; i++)
        {
            var t = i / (double)(count - 1);
            var angle = t * Math.PI * 8.0 + phase;

            points[i] = new Point3D(
                0.1 + 0.1 * t,
                40.0 + t,
                10000.0 * (1.0 + Math.Sin(angle)));
        }

        return points;
    }

    private static string[] CreateExtremeTags(Point3D[] points, int index)
    {
        var tags = new string[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            tags[i] = $"曲线 {index + 1} · 点 #{i}\nX = {point.X:0.000}   Y = {point.Y:0.00}   Z = {point.Z:0}";
        }

        return tags;
    }

    private static Point3D[] CreateSpiralPoints(int index)
    {
        const int count = 720;
        var points = new Point3D[count];
        var phase = index * Math.PI * 2.0 / 5.0;

        for (var i = 0; i < count; i++)
        {
            var t = i / (double)(count - 1);
            var angle = t * Math.PI * 10.0 + phase;
            var radius = 0.05 + 0.95 * Math.Pow(t, 0.8);

            points[i] = new Point3D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle),
                2.0 * t);
        }

        return points;
    }

    private static string[] CreateSpiralTags(Point3D[] points, int index)
    {
        var tags = new string[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            tags[i] = $"曲线 {index + 1} · 点 #{i}\nX = {point.X:0.###}   Y = {point.Y:0.###}   Z = {point.Z:0.###}";
        }

        return tags;
    }

    /// <summary>生成时间轴演示数据：X 为 OADate，Y / Z 为测量值。</summary>
    private static Point3D[] CreateTimePoints(int index, AxisValueFormat format)
    {
        DateTime start = new(2026, 10, 11, 13, 0, 0, DateTimeKind.Unspecified);

        var (count, step) = format switch
        {
            AxisValueFormat.Date => (160, TimeSpan.FromMinutes(90)),      // 约 10 天
            AxisValueFormat.Time => (288, TimeSpan.FromMinutes(5)),       // 24 小时
            _ => (400, TimeSpan.FromMilliseconds(5)),                     // 2 秒，演示毫秒
        };

        var points = new Point3D[count];
        var phase = index * Math.PI * 2.0 / 3.0;

        for (var i = 0; i < count; i++)
        {
            var t = i / (double)(count - 1);
            var timestamp = start + step * i;

            points[i] = new Point3D(
                timestamp.ToOADate(),
                1.2 + 0.6 * Math.Sin(t * Math.PI * 6.0 + phase) + index * 0.35,
                0.4 + t * 1.8 + 0.15 * Math.Cos(t * Math.PI * 4.0 + phase));
        }

        return points;
    }

    private static string[] CreateTimeTags(Point3D[] points, int index, AxisValueFormat format)
    {
        var pattern = format switch
        {
            AxisValueFormat.Date => "yyyy-MM-dd",
            AxisValueFormat.Time => "HH:mm:ss.fff",
            _ => "yyyy-MM-dd HH:mm:ss.fff",
        };

        var tags = new string[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var timestamp = DateTime.FromOADate(points[i].X);
            tags[i] = $"传感器 {index + 1}\n{timestamp.ToString(pattern)}   Y = {points[i].Y:0.###}   Z = {points[i].Z:0.###}";
        }

        return tags;
    }
}
