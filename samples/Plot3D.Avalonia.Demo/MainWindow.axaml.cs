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
    private readonly ComboBox _themeCombo;
    private readonly ComboBox _projectionCombo;
    private readonly ComboBox _paletteCombo;
    private readonly RadioButton _lineMode;
    private readonly RadioButton _scatterMode;
    private readonly RadioButton _bothMode;
    private readonly Slider _tickSlider;

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
        _themeCombo = this.FindControl<ComboBox>("ThemeCombo")!;
        _projectionCombo = this.FindControl<ComboBox>("ProjectionCombo")!;
        _paletteCombo = this.FindControl<ComboBox>("PaletteCombo")!;
        _lineMode = this.FindControl<RadioButton>("LineMode")!;
        _scatterMode = this.FindControl<RadioButton>("ScatterMode")!;
        _bothMode = this.FindControl<RadioButton>("BothMode")!;
        _tickSlider = this.FindControl<Slider>("TickSlider")!;

        _ready = true;
        ApplyPalette(0);
        BuildSeries();
        ApplyStartupArguments();
    }

    /// <summary>支持 --dark / --perspective / --scatter 启动参数，便于快速预览不同外观。</summary>
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
        _plotCard.Background = new SolidColorBrush(cardColor);
        _sideCard.Background = new SolidColorBrush(cardColor);
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

    private void OnResetView(object? sender, RoutedEventArgs e) => _plot.ResetView();

    private void BuildSeries()
    {
        _plot.Series.Clear();

        for (var index = 0; index < 5; index++)
        {
            var points = CreateSpiralPoints(index);

            PlotSeries3D series = _mode switch
            {
                RenderMode.Scatter => new ScatterSeries3D
                {
                    Title = $"散点 {index + 1}",
                    MarkerSize = 3.5,
                    MarkerShape = MarkerShape.Circle,
                },
                RenderMode.LineWithMarkers => new LineSeries3D
                {
                    Title = $"曲线 {index + 1}",
                    StrokeThickness = 1.4,
                    MarkerSize = 3,
                    MarkerShape = MarkerShape.Circle,
                },
                _ => new LineSeries3D
                {
                    Title = $"曲线 {index + 1}",
                    StrokeThickness = 1.6,
                },
            };

            if (_randomColors)
            {
                series.ColorMode = SeriesColorMode.Random;
            }

            switch (series)
            {
                case LineSeries3D line:
                    line.SetPoints(points);
                    break;

                case ScatterSeries3D scatter:
                    scatter.SetPoints(points);
                    break;
            }

            _plot.Series.Add(series);
        }
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
}
