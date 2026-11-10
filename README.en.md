# Plot3D.Avalonia

[简体中文](https://github.com/daipeng0809/Plot3D.Avalonia/blob/master/README.md) | **English**

A lightweight 3D plotting control library for **Avalonia 12**, offering scientific-style 3D line and scatter charts with interactions and visuals inspired by mainstream scientific software (such as MATLAB® `plot3` / `scatter3`).

The control is drawn entirely with the Avalonia drawing APIs. It does not depend on SkiaSharp, does not use reflection and does not use runtime XAML, so it is safe for **NativeAOT / trimming** builds.

![Light theme](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-light.png)

---

## Table of contents

- [1. Features](#1-features)
- [2. Requirements](#2-requirements)
- [3. Installation](#3-installation)
- [4. Quick start](#4-quick-start)
- [5. Data series](#5-data-series)
- [6. Colors and palettes](#6-colors-and-palettes)
- [7. Camera, projection and interaction](#7-camera-projection-and-interaction)
- [8. Data point interaction and context menu](#8-data-point-interaction-and-context-menu)
- [9. Axis value formats and label layout](#9-axis-value-formats-and-label-layout)
- [10. Axes, grid and ticks](#10-axes-grid-and-ticks)
- [11. Themes and fonts](#11-themes-and-fonts)
- [12. Legend](#12-legend)
- [13. Property reference](#13-property-reference)
- [14. Method reference](#14-method-reference)
- [15. Performance guide](#15-performance-guide)
- [16. AOT and trimming](#16-aot-and-trimming)
- [17. Packaging and publishing to NuGet](#17-packaging-and-publishing-to-nuget)
- [18. Extending with custom series](#18-extending-with-custom-series)
- [19. FAQ](#19-faq)
- [20. Version and license](#20-version-and-license)

---

## 1. Features

| Area | Capability |
| --- | --- |
| Charts | 3D lines (`LineSeries3D`) and 3D scatter (`ScatterSeries3D`), freely combined, with optional latest-point highlight |
| Camera | Orbit camera (azimuth / elevation), orthographic and perspective projection, zoom, pan, double-click reset |
| Axes | Automatic "nice" ranges, manual ranges, automatic ticks and labels, axis titles, numeric / date / time formats (millisecond precision) |
| Point interaction | Hover highlight with custom tooltip, crosshair X/Y/Z readout, click selection (event returns the point object), context menu (save PNG / delete point / custom items) |
| Appearance | Solid or fully transparent background, grid / box / pane / ticks / axis-title switches, tick and title offset, gap and font size |
| Colors | Auto palette / fixed / random colors, 7 built-in palettes, opacity and line style |
| Input | Left-drag rotate, right/middle-drag pan, wheel zoom at cursor |
| Themes | Built-in light and dark themes; every color can be overridden |
| Engineering | Scene caching, batched geometry, text-layout caching, no reflection, AOT / trimming safe |

![Dark theme](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-dark.png)

---

## 2. Requirements

- .NET 8.0 / .NET 9.0 / .NET 10.0 (the library ships `net8.0`, `net9.0` and `net10.0` targets)
- Avalonia **12.1+**

> The target framework of the control is independent of the host application: a `net8.0` host uses the `net8.0` target, while `net9.0` / `net10.0` hosts pick the matching target.

---

## 3. Installation

### 3.1 Project reference (local development)

Add `src/Plot3D.Avalonia/Plot3D.Avalonia.csproj` to your solution and reference it from the host project:

```xml
<ItemGroup>
  <ProjectReference Include="..\3D控件\src\Plot3D.Avalonia\Plot3D.Avalonia.csproj" />
</ItemGroup>
```

### 3.2 NuGet package

```bash
dotnet add package Plot3D.Avalonia --version 1.1.2
```

Or manually:

```xml
<ItemGroup>
  <PackageReference Include="Plot3D.Avalonia" Version="1.1.2" />
</ItemGroup>
```

---

## 4. Quick start

### 4.1 XAML namespace

```xml
xmlns:plot3d="https://github.com/daipeng0809/Plot3D.Avalonia"
```

### 4.2 XAML + code

**MainWindow.axaml**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:plot3d="https://github.com/daipeng0809/Plot3D.Avalonia"
        x:Class="MyApp.MainWindow"
        Title="3D plot" Width="1000" Height="700">

  <Grid ColumnDefinitions="*,280">
    <plot3d:Plot3DView x:Name="Plot"
                       Grid.Column="0"
                       ShowGrid="True"
                       ShowLegend="True"
                       ColorTheme="Light"
                       XAxisTitle="X"
                       YAxisTitle="Y"
                       ZAxisTitle="Z" />

    <StackPanel Grid.Column="1" Margin="12" Spacing="8">
      <Button Content="Reset view" Click="OnResetView" />
    </StackPanel>
  </Grid>
</Window>
```

**MainWindow.axaml.cs**

```csharp
using System;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var plot = this.FindControl<Plot3DView>("Plot")!;
        var series = new LineSeries3D { Title = "Spiral", StrokeThickness = 1.6 };

        var points = new Point3D[720];
        for (var i = 0; i < points.Length; i++)
        {
            var t = i / (double)(points.Length - 1);
            var angle = t * Math.PI * 10.0;
            var radius = 0.05 + 0.95 * Math.Pow(t, 0.8);

            points[i] = new Point3D(radius * Math.Cos(angle), radius * Math.Sin(angle), 2.0 * t);
        }

        series.SetPoints(points);
        plot.Series.Add(series);
    }

    private void OnResetView(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.FindControl<Plot3DView>("Plot")!.ResetView();
    }
}
```

### 4.3 Pure code (no XAML)

```csharp
using Avalonia.Controls;
using Plot3D.Avalonia.Controls;
using Plot3D.Avalonia.Modeling;

var plot = new Plot3DView
{
    ShowGrid = true,
    ShowLegend = true,
    ColorTheme = PlotTheme.Dark,
    Margin = new Avalonia.Thickness(12),
};

var scatter = new ScatterSeries3D
{
    Title = "Point cloud",
    MarkerSize = 4,
    MarkerShape = MarkerShape.Circle,
};

scatter.SetPoints(points);
plot.Series.Add(scatter);
```

---

## 5. Data series

### 5.1 Lines: `LineSeries3D`

```csharp
var line = new LineSeries3D
{
    Title = "Temperature",
    StrokeThickness = 1.8,
    LineStyle = LineStyle.Solid,
    MarkerSize = 0,          // > 0 draws vertex markers
    MarkerShape = MarkerShape.Circle,
    ColorMode = SeriesColorMode.Auto,
};

// Replace all data
line.SetPoints(points);

// Append one point (triggers a refresh)
line.Add(new Point3D(1, 2, 3));

// Append many points
line.AddRange(points);

// Clear
line.Clear();
```

**NaN breaks**: a line is automatically split when a point contains `NaN` / `Infinity`:

```csharp
line.SetPoints(new[]
{
    new Point3D(0, 0, 0),
    new Point3D(1, 1, 1),
    new Point3D(double.NaN, double.NaN, double.NaN), // break
    new Point3D(2, 0, 0),
    new Point3D(3, 1, 1),
});
```

**High-performance bulk writes**: modify the `Points` list directly and notify once:

```csharp
line.Points.Clear();
foreach (var p in hugeBuffer)
{
    line.Points.Add(p);
}
line.InvalidateData();   // single refresh
```

### 5.2 Scatter: `ScatterSeries3D`

```csharp
var scatter = new ScatterSeries3D
{
    Title = "Samples",
    MarkerSize = 5,
    MarkerShape = MarkerShape.Diamond,  // Circle / Square / Diamond / Triangle
    Opacity = 0.85,
};

scatter.SetPoints(points);
```

The default `MarkerSize` for scatter series is `6`. All markers are merged into a single geometry and filled once, so tens of thousands of points stay fast.

![Scatter mode](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-scatter.png)

### 5.3 Combining series

```csharp
plot.Series.Add(new LineSeries3D(p1) { Title = "Curve A" });
plot.Series.Add(new LineSeries3D(p2) { Title = "Curve B" });
plot.Series.Add(new ScatterSeries3D(p3) { Title = "Scatter", MarkerSize = 4 });
```

`Series` is a `PlotSeries3DCollection` (derived from `AvaloniaList<PlotSeries3D>`) with XAML support, collection change notifications and data binding.

### 5.4 Common series properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Title` | `string?` | `null` | Legend name |
| `IsVisible` | `bool` | `true` | Whether the series is drawn and included in range calculation |
| `ColorMode` | `SeriesColorMode` | `Auto` | Color selection mode |
| `Color` | `Color` | `SteelBlue` | Fixed color |
| `Opacity` | `double` | `1.0` | Opacity (0..1) |
| `StrokeThickness` | `double` | `1.5` | Line width (DIP) |
| `LineStyle` | `LineStyle` | `Solid` | `Solid` / `Dash` / `Dot` / `DashDot` |
| `MarkerSize` | `double` | `0` (scatter `6`) | Marker size, 0 disables markers |
| `MarkerShape` | `MarkerShape` | `Circle` | Marker shape |
| `HighlightLatestPoint` | `bool` | `false` | Highlight the newest point (`LineSeries3D` / `ScatterSeries3D`) |
| `LatestPointColor` | `Color` | `#FF3B30` | Newest-point color |
| `LatestPointSize` | `double` | `0` | Newest-point size, 0 = automatic |
| `LatestPointShape` | `MarkerShape` | `Circle` | Newest-point shape |

---

## 6. Colors and palettes

### 6.1 Three color modes

```csharp
// 1) Auto: colors come from the palette in series order; an explicitly set Color wins
series.ColorMode = SeriesColorMode.Auto;

// 2) Fixed color
series.ColorMode = SeriesColorMode.Fixed;
series.Color = Colors.Crimson;

// 3) Random: generated once per series on first render and cached
series.ColorMode = SeriesColorMode.Random;
```

> In `Auto` mode, if `series.Color` was explicitly assigned, that color is used instead of the palette. Use `series.ClearValue(PlotSeries3D.ColorProperty)` to force the palette again.

### 6.2 Built-in palettes

| Palette | Style |
| --- | --- |
| `PlotPalette.Tab10` | Matplotlib tab10 (default) |
| `PlotPalette.Tab20` | Matplotlib tab20 |
| `PlotPalette.Classic` | Classic 7-color scientific default |
| `PlotPalette.Set1` | ColorBrewer Set1 |
| `PlotPalette.Pastel` | Soft pastel colors |
| `PlotPalette.Vivid` | High contrast |
| `PlotPalette.Dark` | Bright colors for dark backgrounds |

```csharp
plot.Palette = PlotPalette.Classic;
```

> Palettes only contain color values, no third-party code. `Tab10` / `Tab20` are the Matplotlib defaults and `Set1` comes from ColorBrewer; all values are taken from public information.

Custom palette:

```csharp
plot.Palette = new[]
{
    Color.Parse("#2563EB"),
    Color.Parse("#F97316"),
    Color.Parse("#10B981"),
};
```

### 6.3 Opacity

`Opacity` is multiplied with the alpha channel of the color:

```csharp
scatter.Opacity = 0.35;
```

---

## 7. Camera, projection and interaction

### 7.1 Azimuth and elevation

The camera semantics match the `view(az, el)` convention of mainstream scientific software:

- **`Azimuth`**: rotation around the Z axis. `0` places the camera on the **-Y** side; positive values rotate counter-clockwise (viewed from above). Default `-37.5`.
- **`Elevation`**: angle above the XY plane, range `-90..90`. Default `30`.

```csharp
plot.Azimuth = -37.5;
plot.Elevation = 30;
```

### 7.2 Projection

```csharp
plot.Projection = ProjectionMode.Orthographic; // default
plot.Projection = ProjectionMode.Perspective;  // near objects appear larger
```

### 7.3 Zoom and pan

```csharp
plot.Zoom = 1.5;      // zoom factor (0.02 .. 100)
plot.PanX = 0;        // read-only, current pan offset
plot.ResetZoom();     // reset zoom and pan
plot.ResetCamera();   // reset azimuth and elevation
plot.ResetView();     // reset everything
```

### 7.4 Mouse / touch operations

| Operation | Behavior |
| --- | --- |
| Left drag | Rotate (azimuth + elevation) |
| Right / middle drag | Pan |
| Wheel | Zoom around the cursor |
| Double-click | Reset view |
| Hover a point | Highlight it and show custom info next to the cursor (toggleable) |
| Move inside the plot area | Show a crosshair with X/Y/Z readout (toggleable) |
| Left-click a point | Select it and raise an event with the point object (toggleable) |
| Right-click | Open the context menu (save PNG / delete selected point / custom items, toggleable) |

Interactions can be disabled:

```csharp
plot.IsRotationEnabled = false;
plot.IsPanEnabled = false;
plot.IsZoomEnabled = false;
plot.ResetOnDoubleClick = false;

plot.RotationSensitivity = 0.5;  // degrees per pixel
plot.ZoomSensitivity = 0.15;     // exponential step per wheel notch
```

---

## 8. Data point interaction and context menu

### 8.1 Attaching business data to points (Tag)

`LineSeries3D` / `ScatterSeries3D` derive from `PointSeries3D`, which keeps a per-point `Tag` object.
Hover tooltips, selection events and the context menu all expose that object:

```csharp
var series = new ScatterSeries3D { Title = "Temperature", MarkerSize = 5 };

// 1) Points and tags together
var points = new[] { new Point3D(1, 2, 3), new Point3D(2, 4, 5) };
var tags = new object?[]
{
    $"Sensor A · {DateTime.Now:HH:mm:ss} · 23.5 ℃",
    $"Sensor A · {DateTime.Now:HH:mm:ss} · 24.1 ℃",
};
series.SetPoints(points, tags);

// 2) Append a single point with a tag
series.Add(new Point3D(3, 6, 7), "Sensor A · 25.0 ℃");

// 3) Maintain the Tags list directly (index-aligned with Points)
series.Tags.Add("Extra info");
series.InvalidateData();
```

| Member | Description |
| --- | --- |
| `Points` | Underlying `List<Point3D>` |
| `Tags` | Underlying `List<object?>`; missing entries are null (tooltip falls back to coordinates) |
| `SetPoints(points, tags)` | Replace points and tags together |
| `Add(point, tag)` | Append a point with a tag |
| `AddRange(points, tags)` | Append points and tags |
| `RemoveAt(index)` | Remove a point (and its tag); returns true on success |
| `GetTag(index)` / `SetTag(index, tag)` | Read / write a point tag |

> Override `ToString()` on your tag type to control the default tooltip text. No reflection or serialization is involved.

### 8.2 Hover highlight and tooltip

```csharp
plot.IsPointHoverEnabled = true;      // master switch
plot.IsHoverToolTipEnabled = true;    // show tooltip
plot.HoverRadius = 10;                // hit radius in pixels
plot.HoverHighlightColor = Color.Parse("#FF9F0A");
plot.HoverHighlightSize = 0;          // 0 = automatic (marker size + 4)
```

Tooltip text resolution order:

1. `HoverTextFormatter` callback, when it returns a non-empty string;
2. `Tag.ToString()` of the hovered point;
3. `X / Y / Z` text formatted with the axis value formats.

```csharp
plot.HoverTextFormatter = info =>
    $"{info.Series.Title}\nPoint #{info.PointIndex}\n" +
    $"X = {plot.FormatXAxisValue(info.Position.X)}\n" +
    $"Y = {plot.FormatYAxisValue(info.Position.Y)}\n" +
    $"Z = {plot.FormatZAxisValue(info.Position.Z)}";
```

Tooltip styling:

```csharp
plot.ToolTipBackground = new SolidColorBrush(Color.FromArgb(235, 32, 32, 40));
plot.ToolTipBorderBrush = new SolidColorBrush(Color.Parse("#50505A"));
plot.ToolTipForeground = Brushes.White;
```

> Hit testing projects every visible point; tens of thousands of points still respond in real time. For extremely large datasets, downsample in your business layer.

### 8.3 Crosshair and coordinate readout

Move the mouse inside the plot area to show three dashed guide lines (towards the three far walls) plus an X / Y / Z readout box:

```csharp
plot.IsCrosshairEnabled = true;       // master switch
plot.ShowCrosshairLabels = true;      // show the readout box
plot.CrosshairBrush = new SolidColorBrush(Color.Parse("#7A7A88"));
```

The readout uses the same numeric / date / time formats as the axes (see [section 9](#9-axis-value-formats-and-label-layout)).
The crosshair casts a view ray from the mouse position and intersects it with the axis bounding box:
the hit always lies on the cursor ray, so its projection matches the cursor exactly and no per-axis clamping
is involved. When the ray misses the box, or the hit is too far from the view plane, the crosshair and readout are hidden.

### 8.4 Click selection

```csharp
plot.IsPointSelectionEnabled = true;
plot.SelectionRadius = 12;                                  // click hit radius
plot.SelectionHighlightColor = Color.Parse("#0A84FF");       // different from hover color
plot.SelectionHighlightSize = 0;                             // 0 = automatic

plot.PointSelected += (_, e) =>
{
    var info = e.Point;           // hit result
    var series = info.Series;     // owning series
    var index = info.PointIndex;  // index inside the series
    var position = info.Position; // 3D position
    var tag = info.Tag;           // tag supplied when the point was added
};

plot.PointDeselected += (_, e) =>
{
    // Raised when clicking empty space, deleting the point, or when data changes
};

// Programmatic selection / clearing / deletion
plot.SelectPoint(series, 10);
plot.ClearSelection();
plot.DeleteSelectedPoint();
```

Clicking empty space clears the selection. Hover (amber) and selection (blue) use different colors and shapes and can be told apart at a glance.

### 8.5 Context menu

A right-click without dragging opens the context menu. Built-in items:

- **Save image (PNG)**: opens the system save dialog and exports the current view;
- **Delete selected point**: only shown when a point is selected; raises `PointDeselected`.

Add custom items:

```csharp
var exportCsv = new MenuItem { Header = "Export data (CSV)" };
exportCsv.Click += (_, _) => ExportCsv();
plot.ContextMenuItems.Add(exportCsv);
```

Build items dynamically per click:

```csharp
plot.ContextMenuBuilding += (_, e) =>
{
    if (e.HitPoint is { } hit)
    {
        var item = new MenuItem { Header = $"Locate {hit.Series.Title}[{hit.PointIndex}]" };
        item.Click += (_, _) => SelectAndZoom(hit);
        e.Items.Add(item);
    }
};
```

Right-clicking a point selects it before the menu opens.

### 8.6 PNG export and transparent background

```csharp
// Write a file directly (scale = 2 renders at 2x resolution)
await plot.SaveImageAsync("chart.png", scale: 2.0);

// Show the system save dialog
await plot.SaveImageWithDialogAsync();
```

> The exported PNG only contains the chart itself and never includes hover / selection highlights, the crosshair or tooltips.

To overlay the control on other visuals, disable its own background:

```csharp
plot.IsBackgroundTransparent = true;
```

When enabled, the control no longer fills its background. With `ShowPane = true`, the panes are also only filled when `PaneBrush` is explicitly set.

### 8.7 Latest-point highlight (direction indicator)

```csharp
var line = new LineSeries3D
{
    HighlightLatestPoint = true,          // draw the newest valid point differently
    LatestPointColor = Color.Parse("#FF3B30"),
    LatestPointSize = 0,                  // 0 = automatic (1.8x marker size, at least 7)
    LatestPointShape = MarkerShape.Circle,
};
```

The style applies to both lines and scatter series and skips trailing NaN points, which makes growth direction obvious in real-time scenarios.

---

## 9. Axis value formats and label layout

### 9.1 Date / time axes

Time formats expect axis values to be **OLE Automation dates** (`DateTime.ToOADate()`, unit: days):

```csharp
var timestamp = new DateTime(2026, 10, 11, 13, 22, 5, 123);
series.Add(new Point3D(timestamp.ToOADate(), value, depth), $"Sampled at {timestamp:HH:mm:ss.fff}");

plot.XAxisValueFormat = AxisValueFormat.DateTime;  // 2026-10-11 13:22:05.123
plot.AxisTimeMilliseconds = true;                  // include milliseconds
```

| `AxisValueFormat` | Example | Description |
| --- | --- | --- |
| `Numeric` | `1.25` | Default numeric format |
| `Date` | `2026-10-11` | Date only |
| `Time` | `13:22:05` / `13:22:05.123` | Time only; `.fff` when milliseconds are enabled |
| `DateTime` | `2026-10-11 13:22:05` | Date + time |

Time axes automatically pick "nice" steps such as 1 ms / 5 ms / 1 s / 15 s / 1 min / 1 h / 1 day,
and automatically include milliseconds when the step is below one second. X / Y / Z can use different formats:

```csharp
plot.XAxisValueFormat = AxisValueFormat.DateTime;
plot.YAxisValueFormat = AxisValueFormat.Numeric;
plot.ZAxisValueFormat = AxisValueFormat.Time;
```

> To avoid duplicated labels, the `Date` format adds the time when the data spans less than a day, and the
> `Time` format adds the date when the span exceeds one day. Week ticks align to Mondays; month / year ticks
> align to the first day of the month / January 1 (multi-year steps are anchored at January 2000).

Formatted text is also available for tooltips and custom labels:

```csharp
var text = plot.FormatXAxisValue(46277.55699...);   // formatted with the X-axis format
```

> Increase `PlotPadding` when labels become long so they are not clipped.

### 9.2 Tick and axis-title layout

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TickLength` | `double` | `4` | Tick mark length (pixels) |
| `TickLabelGap` | `double` | `3` | Gap between tick mark and label (may be negative) |
| `TickLabelOffset` | `Vector` | `0,0` | Global offset applied to every tick label |
| `TickLabelFontSize` | `double` | `NaN` | Tick label font size; `NaN` uses `FontSize` |
| `AxisTitleGap` | `double` | `0` | Extra gap between axis title and tick labels |
| `AxisTitleOffset` | `Vector` | `0,0` | Global offset applied to every axis title |
| `AxisTitleFontSize` | `double` | `NaN` | Axis title font size; `NaN` uses `FontSize` |

```csharp
plot.TickLength = 6;
plot.TickLabelGap = 5;
plot.TickLabelOffset = new Vector(0, 2);
plot.TickLabelFontSize = 11;
plot.AxisTitleGap = 6;
plot.AxisTitleOffset = new Vector(0, -2);
plot.AxisTitleFontSize = 13;
```

Multi-line or very long labels still require enough `PlotPadding`.

---

## 10. Axes, grid and ticks

### 10.1 Automatic ranges

By default the range is computed from the bounding box of all visible series and aligned to "nice" ticks
(1 / 2 / 2.5 / 5 × 10ⁿ). With no data a `0..1` box is shown; when all points coincide, a visible range is expanded automatically.

### 10.2 Manual ranges

One side can be fixed while the other stays automatic:

```csharp
plot.XAxisMinimum = -1;
plot.XAxisMaximum = 1;

plot.ZAxisMinimum = 0;
plot.ZAxisMaximum = 2;

// Back to automatic
plot.XAxisMinimum = double.NaN;
```

### 10.3 Tick count

```csharp
plot.TickCount = 6;   // desired ticks per axis (including both ends), valid range 2..20
```

Label decimals are chosen from the step; very large / small values switch to scientific notation.

### 10.4 Visibility switches

```csharp
plot.ShowGrid = true;         // background grid
plot.ShowBox = true;          // bounding box (12 edges)
plot.ShowPane = false;        // far wall panes
plot.ShowTicks = true;        // tick marks
plot.ShowTickLabels = true;   // tick labels
plot.ShowAxisTitles = true;   // axis titles

plot.XAxisTitle = "X (m)";
plot.YAxisTitle = "Y (m)";
plot.ZAxisTitle = "Z (m)";
```

### 10.5 Padding

Tick labels and axis titles are drawn outside the box; reserve space with `PlotPadding`:

```csharp
plot.PlotPadding = new Thickness(56, 40, 56, 40); // left, top, right, bottom
```

Increase it when fonts are large or labels are long.

---

## 11. Themes and fonts

### 11.1 Built-in themes

```csharp
plot.ColorTheme = PlotTheme.Light;  // white background, dark text
plot.ColorTheme = PlotTheme.Dark;   // dark background, light text
```

Themes only affect colors that were **not explicitly set**.

### 11.2 Overriding colors

```csharp
plot.Background = new SolidColorBrush(Color.Parse("#0F172A"));
plot.Foreground = Brushes.White;
plot.AxisBrush = new SolidColorBrush(Color.Parse("#94A3B8"));
plot.GridBrush = new SolidColorBrush(Color.Parse("#334155"));
plot.PaneBrush = new SolidColorBrush(Color.Parse("#1E293B"));
plot.LegendBackground = new SolidColorBrush(Color.FromArgb(210, 15, 23, 42));

// Fully transparent background (no base fill at all)
plot.IsBackgroundTransparent = true;
```

### 11.3 Fonts

```csharp
plot.FontFamily = new FontFamily("avares://MyApp/Assets/Fonts/AlibabaPuHuiTi.ttf#Alibaba PuHuiTi 3.0");
plot.FontSize = 13;
```

Fonts affect tick labels, axis titles and the legend.

---

## 12. Legend

```csharp
plot.ShowLegend = true;
series.Title = "Curve A";
```

- Only series with a non-empty `Title` and `IsVisible = true` appear in the legend.
- The legend is pinned to the top-left corner; its background comes from `LegendBackground` (theme color by default).

---

## 13. Property reference

### 13.1 `Plot3DView`

#### Data and colors

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Series` | `PlotSeries3DCollection` | empty | Series collection |
| `Palette` | `IReadOnlyList<Color>?` | `PlotPalette.Tab10` | Auto color palette |
| `Background` | `IBrush?` | theme | Background brush |
| `Foreground` | `IBrush?` | theme | Tick / title / legend text |
| `AxisBrush` | `IBrush?` | theme | Box and tick marks |
| `GridBrush` | `IBrush?` | theme | Grid lines and legend border |
| `PaneBrush` | `IBrush?` | theme | Far wall fill |
| `LegendBackground` | `IBrush?` | theme | Legend background |
| `ColorTheme` | `PlotTheme` | `Light` | Light / dark theme |

#### Visibility

| Property | Type | Default |
| --- | --- | --- |
| `ShowBox` | `bool` | `true` |
| `ShowGrid` | `bool` | `true` |
| `ShowPane` | `bool` | `false` |
| `ShowTicks` | `bool` | `true` |
| `ShowTickLabels` | `bool` | `true` |
| `ShowAxisTitles` | `bool` | `true` |
| `ShowLegend` | `bool` | `false` |

#### Axes

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `XAxisTitle` / `YAxisTitle` / `ZAxisTitle` | `string?` | `"X"` / `"Y"` / `"Z"` | Axis titles; empty hides them |
| `XAxisMinimum` / `XAxisMaximum` | `double` | `NaN` | X range, `NaN` = automatic |
| `YAxisMinimum` / `YAxisMaximum` | `double` | `NaN` | Y range |
| `ZAxisMinimum` / `ZAxisMaximum` | `double` | `NaN` | Z range |
| `TickCount` | `int` | `6` | Desired tick count (2..20) |
| `PlotPadding` | `Thickness` | `56,40,56,40` | Plot area padding |

#### Camera and input

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Azimuth` | `double` | `-37.5` | Azimuth (degrees) |
| `Elevation` | `double` | `30` | Elevation (degrees, -89.9..89.9) |
| `Zoom` | `double` | `1.0` | Zoom factor |
| `Projection` | `ProjectionMode` | `Orthographic` | Projection mode |
| `IsRotationEnabled` | `bool` | `true` | Enable drag rotation |
| `IsPanEnabled` | `bool` | `true` | Enable drag pan |
| `IsZoomEnabled` | `bool` | `true` | Enable wheel zoom |
| `ResetOnDoubleClick` | `bool` | `true` | Double-click resets the view |
| `RotationSensitivity` | `double` | `0.5` | Degrees per pixel |
| `ZoomSensitivity` | `double` | `0.15` | Wheel step |
| `PanX` / `PanY` | `double` | read-only | Current pan offset (pixels) |

#### Point interaction

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `IsPointHoverEnabled` | `bool` | `true` | Enable hover highlight and tooltip |
| `HoverRadius` | `double` | `10` | Hover hit radius (pixels) |
| `HoverHighlightColor` | `Color` | `#FF9F0A` | Hover color |
| `HoverHighlightSize` | `double` | `0` | Hover marker size, 0 = automatic |
| `IsHoverToolTipEnabled` | `bool` | `true` | Show the tooltip |
| `ToolTipBackground` / `ToolTipBorderBrush` / `ToolTipForeground` | `IBrush?` | theme | Tooltip colors |
| `HoverTextFormatter` | `Func<PlotPointInfo, string?>?` | `null` | Custom tooltip text; null falls back to tag / coordinates |
| `HoveredPoint` | `PlotPointInfo?` | read-only | Currently hovered point |
| `IsPointSelectionEnabled` | `bool` | `true` | Enable click selection |
| `SelectionRadius` | `double` | `12` | Click hit radius (pixels) |
| `SelectionHighlightColor` | `Color` | `#0A84FF` | Selection color |
| `SelectionHighlightSize` | `double` | `0` | Selection marker size, 0 = automatic |
| `SelectedPoint` | `PlotPointInfo?` | read-only | Currently selected point |
| `IsCrosshairEnabled` | `bool` | `true` | Show the crosshair |
| `ShowCrosshairLabels` | `bool` | `true` | Show the crosshair readout |
| `CrosshairBrush` | `IBrush?` | axis color | Crosshair color |
| `IsContextMenuEnabled` | `bool` | `true` | Enable the context menu |
| `ContextMenuItems` | `AvaloniaList<Control>` | empty | Additional context-menu items |

#### Axis formats and label layout

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `XAxisValueFormat` / `YAxisValueFormat` / `ZAxisValueFormat` | `AxisValueFormat` | `Numeric` | Numeric / date / time / date + time |
| `AxisTimeMilliseconds` | `bool` | `false` | Include milliseconds in time formats |
| `TickLength` | `double` | `4` | Tick length (pixels) |
| `TickLabelGap` | `double` | `3` | Gap between tick and label (pixels) |
| `TickLabelOffset` | `Vector` | `0,0` | Global tick label offset |
| `TickLabelFontSize` | `double` | `NaN` | Tick label font size, `NaN` uses `FontSize` |
| `AxisTitleGap` | `double` | `0` | Extra axis title gap (pixels) |
| `AxisTitleOffset` | `Vector` | `0,0` | Global axis title offset |
| `AxisTitleFontSize` | `double` | `NaN` | Axis title font size, `NaN` uses `FontSize` |
| `IsBackgroundTransparent` | `bool` | `false` | Fully transparent background |

#### Fonts

| Property | Type | Default |
| --- | --- | --- |
| `FontFamily` | `FontFamily` | `FontFamily.Default` |
| `FontSize` | `double` | `12` |

---

## 14. Method reference

### 14.1 `Plot3DView`

| Method | Description |
| --- | --- |
| `ResetView()` | Reset azimuth, elevation, zoom and pan |
| `ResetCamera()` | Reset azimuth and elevation only |
| `ResetZoom()` | Reset zoom and pan only |
| `InvalidatePlot()` | Recompute automatic ranges and redraw (after editing `Points` directly) |
| `TryHitTest(position, radius, out info)` | Return the nearest point within the radius |
| `SelectPoint(series, index)` | Select a point by series and index |
| `ClearSelection()` | Clear the selection |
| `DeleteSelectedPoint()` | Delete the selected point |
| `SaveImageAsync(path, scale)` | Render the current view to a PNG file |
| `SaveImageWithDialogAsync()` | Show the save dialog and export a PNG |
| `FormatXAxisValue` / `FormatYAxisValue` / `FormatZAxisValue` | Format a value with the matching axis format |

Point-related events:

| Event | Description |
| --- | --- |
| `PointSelected` | A point was selected by click or code; the arguments carry a `PlotPointInfo` |
| `PointDeselected` | The selection was cleared, deleted or invalidated by data changes |
| `ContextMenuBuilding` | Raised while building the context menu; add or remove items per click |

### 14.2 `PointSeries3D` (`LineSeries3D` / `ScatterSeries3D`)

| Method / property | Description |
| --- | --- |
| `SetPoints(IEnumerable<Point3D>)` | Replace all data and refresh |
| `SetPoints(IEnumerable<Point3D>, IEnumerable<object?>?)` | Replace data and per-point tags |
| `SetPoints(ReadOnlySpan<Point3D>)` | Replace all data (allocation-free overload) |
| `Add(Point3D)` / `Add(Point3D, object?)` | Append one point (optionally with a tag) |
| `AddRange(IEnumerable<Point3D>)` / `AddRange(points, tags)` | Append many points / tags |
| `RemoveAt(int)` | Remove a point (and its tag) |
| `Clear()` | Clear all data |
| `GetTag(int)` / `SetTag(int, object?)` | Read / write a point tag |
| `InvalidateData()` | Notify after editing `Points` directly |
| `TryFindNearestPoint(projection, position, out index, out distance)` | Find the nearest point in screen space |
| `GetBounds()` | Return the data bounding box (`BoundingBox3D`) |
| `HighlightLatestPoint` / `LatestPointColor` / `LatestPointSize` / `LatestPointShape` | Latest-point highlight style |
| `Points` / `Tags` / `Count` | Point list, tag list and point count |

`Points` exposes the underlying `List<Point3D>` for maximum performance; call `InvalidateData()` after bulk edits.

---

## 15. Performance guide

### 15.1 Built-in optimizations

- **Scene caching**: projection and ticks are recomputed only when camera, size, data or related properties change.
- **Batched geometry**: an entire line is a single `StreamGeometry`; all scatter markers are merged into one geometry and filled once.
- **Screen-space decimation**: consecutive points closer than 0.05 DIP are skipped.
- **Small marker shortcut**: markers ≤ 2.5 DIP use quads instead of polygons.
- **Text caching**: tick / title `TextLayout` objects are rebuilt only when font, size or foreground changes.
- **Brush caching**: theme brushes are rebuilt only when color properties change.
- **Screen-space hit cache**: each series caches the projected screen positions for the current projection,
  so pointer moves only compare distances instead of re-projecting every point (about 1.5 ms per query with 100k points).
- **No reflection / no LINQ on hot paths**: render loops use `Span<T>` and indexed iteration.

### 15.2 Recommendations

| Scenario | Recommendation |
| --- | --- |
| Real-time data (several updates per second) | Reuse `series.Points`, then call `InvalidateData()` once |
| Tens of thousands of points | Prefer `ScatterSeries3D` (better geometry batching) |
| Very long polylines (> 200k points) | Downsample in the business layer or in screen space |
| Maximum frame rate | Turn off `ShowGrid` / `ShowTicks` |
| Frequently switching data | Avoid `Series.Clear()`; reuse series and call `SetPoints` |

### 15.3 Measuring

```csharp
var sw = Stopwatch.StartNew();
series.SetPoints(points);   // triggers one refresh
plot.InvalidatePlot();
sw.Stop();
Console.WriteLine($"Update took {sw.Elapsed.TotalMilliseconds:F2} ms");
```

---

## 16. AOT and trimming

The library enables:

```xml
<IsAotCompatible>true</IsAotCompatible>
<IsTrimmable>true</IsTrimmable>
<EnableAotAnalyzer>true</EnableAotAnalyzer>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
```

All drawing uses strongly typed `AvaloniaProperty` registrations and code-based rendering: no `TypeConverter` reflection, no runtime XAML and no dynamic bindings, so no extra configuration is required for NativeAOT or trimming.

Publishing a NativeAOT host:

```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true
```

Suggested host csproj settings:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

> If the host uses this control from XAML, make sure Avalonia 12 XAML compilation works (compiled bindings are enabled by default). This library contains no `.axaml` resources.

---

## 17. Packaging and publishing to NuGet

### 17.1 Local packaging

```bash
dotnet pack src/Plot3D.Avalonia/Plot3D.Avalonia.csproj -c Release -o ./artifacts
```

Output:

```
artifacts/
├── Plot3D.Avalonia.1.1.2.nupkg    -- main package
└── Plot3D.Avalonia.1.1.2.snupkg   -- symbol package (pushed together with the main package)
```

`SymbolPackageFormat` is `snupkg`, so no `.symbols.nupkg` is produced.

The package contains:

- `lib/net8.0/Plot3D.Avalonia.dll`
- `lib/net9.0/Plot3D.Avalonia.dll`
- `lib/net10.0/Plot3D.Avalonia.dll`
- `lib/*/Plot3D.Avalonia.xml` (full XML documentation)
- `README.md`
- `README.en.md`
- `icon.png`

### 17.2 Testing the package locally

```bash
dotnet nuget add source ./artifacts --name Plot3DLocal
dotnet add package Plot3D.Avalonia --version 1.1.2 --source Plot3DLocal
```

### 17.3 Publishing to nuget.org

The repository ships `.github/workflows/publish.yml`.

**Option 1: GitHub Actions with trusted publishing (no stored secrets)**

1. Create a policy on the nuget.org **Trusted Publishing** page:
   - Package Owner: your account
   - CI/CD Provider: `GitHub Actions`
   - Repository Owner / Repository: your GitHub user / repository
   - Workflow File: `publish.yml` (**file name only**, without `.github/workflows/`)
2. Trigger a release:
   - Push a tag: `git tag v1.1.2` → `git push origin v1.1.2` (the version comes from the tag)
   - Or run `Publish to NuGet` manually and optionally override the version

The workflow exchanges a GitHub OIDC token for a temporary NuGet API key (valid for **1 hour**), so no long-lived secrets are stored.

**Option 2: manual push**

```bash
# 1. Create an API key at https://www.nuget.org (API Keys page)
# 2. Push
dotnet nuget push ./artifacts/Plot3D.Avalonia.1.1.2.nupkg \
  --api-key <YOUR_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Before publishing, verify:

```xml
<Authors>your name or team</Authors>
<PackageProjectUrl>project home</PackageProjectUrl>
<RepositoryUrl>git repository</RepositoryUrl>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<Version>1.1.2</Version>
```

> Published versions cannot be deleted or overwritten, only unlisted. Fixes require a new version.

### 17.4 Versioning

Semantic versioning:

- `1.1.x`: bug fixes
- `1.x.0`: backwards-compatible features
- `2.0.0`: breaking changes

---

## 18. Extending with custom series

Only lines and scatter are built in; inherit `PlotSeries3D` to add surfaces, bars, vector fields, etc.

### 18.1 Extension points

```csharp
public abstract class PlotSeries3D : AvaloniaObject
{
    // Required: data bounds used for automatic axis ranges
    public abstract BoundingBox3D GetBounds();

    // Required: draw the series
    protected internal abstract void Render(SeriesRenderContext context);

    // Optional: notify the control about data changes
    protected void RaiseDataChanged();
}
```

`SeriesRenderContext` provides:

| Member | Description |
| --- | --- |
| `DrawingContext` | Current Avalonia drawing surface |
| `Projection` | 3D to 2D projector (`TryProject` / `Project` / `TryUnproject` / `TryIntersectBounds`) |
| `Color` | Resolved series color |
| `Opacity` | Series opacity |
| `ViewBounds` | Control bounds |
| `CreateBrush()` | Brush using the series color |
| `CreatePen(thickness, style)` | Pen using the series color |

### 18.2 Example: 3D bar series

```csharp
using Avalonia;
using Avalonia.Media;
using Plot3D.Avalonia.Modeling;
using Plot3D.Avalonia.Rendering;

public sealed class BarSeries3D : PlotSeries3D
{
    private readonly List<(Point3D Base, double Size)> _bars = new();

    public IReadOnlyList<(Point3D Base, double Size)> Bars => _bars;

    public void AddBar(Point3D basePoint, double size)
    {
        _bars.Add((basePoint, size));
        RaiseDataChanged();
    }

    public override BoundingBox3D GetBounds()
    {
        var bounds = BoundingBox3D.Empty;
        foreach (var (point, size) in _bars)
        {
            bounds = bounds
                .Union(point.X - size, point.Y - size, point.Z)
                .Union(point.X + size, point.Y + size, point.Z + size);
        }

        return bounds;
    }

    protected override void Render(SeriesRenderContext context)
    {
        var dc = context.DrawingContext;
        var brush = context.CreateBrush();
        var pen = context.CreatePen(0.8, LineStyle.Solid);

        foreach (var (basePoint, size) in _bars)
        {
            var bottom = new Point3D(basePoint.X, basePoint.Y, basePoint.Z);
            var top = new Point3D(basePoint.X, basePoint.Y, basePoint.Z + size);

            if (!context.TryProject(bottom, out var p0) || !context.TryProject(top, out var p1))
            {
                continue;
            }

            var a = Project(context, basePoint.X - size, basePoint.Y - size, basePoint.Z + size);
            var b = Project(context, basePoint.X + size, basePoint.Y - size, basePoint.Z + size);
            var c = Project(context, basePoint.X + size, basePoint.Y + size, basePoint.Z + size);
            var d = Project(context, basePoint.X - size, basePoint.Y + size, basePoint.Z + size);

            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(a, true);
                g.LineTo(b);
                g.LineTo(c);
                g.LineTo(d);
                g.EndFigure(true);
            }

            dc.DrawGeometry(brush, pen, geometry);
            dc.DrawLine(pen, p0, p1);
        }
    }

    private static Point Project(SeriesRenderContext context, double x, double y, double z)
    {
        context.Projection.TryProject(x, y, z, out var px, out var py);
        return new Point(px, py);
    }
}
```

Usage:

```csharp
var bars = new BarSeries3D { Title = "Bars", ColorMode = SeriesColorMode.Fixed, Color = Colors.SteelBlue };
bars.AddBar(new Point3D(0, 0, 0), 0.4);
bars.AddBar(new Point3D(1, 0, 0), 0.6);
plot.Series.Add(bars);
```

> From another assembly, override `Render` with `protected override` (`protected internal` behaves as `protected` externally).

### 18.3 Tips

- Merge similar primitives into one `StreamGeometry`.
- Avoid allocating large arrays in `Render`; cache with fields and invalidate on `DataChanged`.
- Always call `RaiseDataChanged()` after data changes, otherwise automatic ranges will not update.

---

## 19. FAQ

**Q1: Axis labels are clipped or overlap. What can I do?**

Adjust `PlotPadding` (more space) or `TickCount` (fewer ticks). Increase padding when the font is large.

**Q2: How do I hide the axes and show only data?**

```csharp
plot.ShowBox = false;
plot.ShowGrid = false;
plot.ShowTicks = false;
plot.ShowAxisTitles = false;
```

**Q3: How do I bind data in MVVM?**

Bind `Plot3DView.Series` to a `PlotSeries3DCollection` on the view model:

```xml
<plot3d:Plot3DView Series="{Binding Series}" ShowLegend="True" />
```

`PlotSeries3DCollection` derives from `AvaloniaList<T>` and supports collection notifications.

**Q4: How do I declare series in XAML?**

Declare series and appearance in XAML, set points in code:

```xml
<plot3d:Plot3DView>
  <plot3d:Plot3DView.Series>
    <plot3d:PlotSeries3DCollection>
      <plot3d:LineSeries3D x:Name="SeriesA" Title="Curve 1" StrokeThickness="1.5" />
      <plot3d:ScatterSeries3D x:Name="SeriesB" Title="Scatter" MarkerSize="4" />
    </plot3d:PlotSeries3DCollection>
  </plot3d:Plot3DView.Series>
</plot3d:Plot3DView>
```

```csharp
SeriesA.SetPoints(pointsA);
SeriesB.SetPoints(pointsB);
```

> `Point3D` is a readonly struct with a three-argument constructor and is not suitable for per-point XAML; use `SetPoints` for large datasets.

**Q5: How do I export an image?**

The control ships PNG export:

```csharp
await plot.SaveImageAsync("chart.png", scale: 2.0);   // save directly
await plot.SaveImageWithDialogAsync();                // show the save dialog
```

You can also use Avalonia's `RenderTargetBitmap` yourself:

```csharp
using Avalonia;
using Avalonia.Media.Imaging;

var dpi = 2.0;
var size = new Size(1200, 800);
var bitmap = new RenderTargetBitmap(
    new PixelSize((int)(size.Width * dpi), (int)(size.Height * dpi)),
    new Vector(96 * dpi, 96 * dpi));

plot.Measure(size);
plot.Arrange(new Rect(size));
bitmap.Render(plot);
bitmap.Save("chart.png");
```

**Q6: Why is my line broken?**

`NaN` / `Infinity` values intentionally break the line. Check your data source.

**Q7: Random colors differ between runs. How can I make them stable?**

Random colors come from `Random.Shared` and are cached per series. Use `SeriesColorMode.Fixed` or a custom palette for reproducible colors.

**Q8: Is touch supported?**

Yes. Avalonia maps single-finger touch to left-drag (rotate). Multi-finger gestures are not handled specially.

**Q9: Can I update data from a background thread?**

`SetPoints` calls `InvalidateVisual`, so it must run on the UI thread. Build arrays in the background and post them via `Dispatcher.UIThread.Post`.

**Q10: The control shows nothing.**

Check:

1. The parent gives a non-zero size (the control defaults to 360×280; set `Width` / `Height`).
2. At least one `IsVisible = true` series with data exists; otherwise an empty `0..1` box is shown.
3. Background and foreground are not the same color.

**Q11: How do I show business information in the hover tooltip?**

Add a `Tag` when adding points (see [8.1](#81-attaching-business-data-to-points-tag)) or set `HoverTextFormatter`.
The tooltip content is entirely controlled by the host; the control never uses reflection or serialization.

**Q12: What values should a time axis receive?**

Use `DateTime.ToOADate()` (unit: days) and set `XAxisValueFormat` / `YAxisValueFormat` / `ZAxisValueFormat` to
`Date`, `Time` or `DateTime`. Set `AxisTimeMilliseconds = true` to include milliseconds (steps below one second always include them).

**Q13: Does hover hit testing slow down large datasets?**

Hit testing projects every visible point; tens of thousands of points still respond in real time. For hundreds of thousands,
downsample in your business layer or enable `IsPointHoverEnabled` / `IsCrosshairEnabled` only when needed.

---

## 20. Version and license

### 1.1.2

- Switched README screenshots to the jsDelivr CDN (`cdn.jsdelivr.net`) so images still load on networks where GitHub raw is unreachable (no code changes).

### 1.1.1

- Fixed the language switch links and screenshots on the NuGet package page by replacing relative paths with absolute GitHub URLs (no code changes).

### 1.1.0

- Added point interaction: hover highlight with custom tooltips (per-point Tag), crosshair X/Y/Z readout, click selection with `PointSelected` / `PointDeselected` events.
- Added context menu: built-in "Save image (PNG)" and "Delete selected point", customizable via `ContextMenuItems` and `ContextMenuBuilding`.
- Added PNG export: `SaveImageAsync` / `SaveImageWithDialogAsync`.
- Added fully transparent background (`IsBackgroundTransparent`).
- Added date / time axis formats (`AxisValueFormat`, millisecond precision) with time-friendly ticks.
- Added tick / axis title layout options: `TickLength`, `TickLabelGap`, `TickLabelOffset`, `TickLabelFontSize`, `AxisTitleGap`, `AxisTitleOffset`, `AxisTitleFontSize`.
- Added latest-point highlight style: `HighlightLatestPoint` / `LatestPointColor` / `LatestPointSize` / `LatestPointShape`.
- Extracted the common `PointSeries3D` base class with per-point tags and `RemoveAt` and related APIs.
- Updated the demo application and documentation; added this English README.

### 1.0.0

- First release: 3D lines, 3D scatter, orbit camera, orthographic / perspective projection, automatic ticks, grid, panes, legend, light / dark themes, 7 palettes, AOT / trimming support.

### License

MIT License. Free for commercial use; please keep the copyright notice.

### Trademarks

MATLAB® is a registered trademark of The MathWorks, Inc.

MATLAB is mentioned only to describe the visual and interaction style of this control. This project is not affiliated with,
authorized or endorsed by The MathWorks, Inc. It is an independent implementation, does not contain, call or derive from any
MathWorks product code, and does not provide MATLAB language, data or file-format compatibility.

Matplotlib and ColorBrewer are projects/works of their respective owners; this project only uses their public color values.

### Repository layout

```
3D控件/
├── Plot3D.Avalonia.sln
├── README.md                   Chinese documentation
├── README.en.md                English documentation
├── docs/                       Screenshots and icon
├── src/Plot3D.Avalonia/        Control library (NuGet package)
│   ├── Controls/               Plot3DView (properties / rendering / input / context menu)
│   ├── Modeling/               Point3D, series, tags, palettes, enums
│   ├── Rendering/              Projection, ticks, frame, legend, interaction overlay
│   └── Properties/             XmlnsDefinition
└── samples/Plot3D.Avalonia.Demo/   Demo application
```

### Running the demo

```bash
dotnet run --project samples/Plot3D.Avalonia.Demo
```

Optional startup arguments:

```bash
dotnet run --project samples/Plot3D.Avalonia.Demo -- --dark --perspective --scatter
dotnet run --project samples/Plot3D.Avalonia.Demo -- --time
```
