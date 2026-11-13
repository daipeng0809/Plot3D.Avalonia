# Plot3D.Avalonia

[简体中文](https://github.com/daipeng0809/Plot3D.Avalonia/blob/master/README.md) | [English](https://github.com/daipeng0809/Plot3D.Avalonia/blob/master/README.en.md)

面向 **Avalonia 12** 的轻量级三维绘图控件库，提供科学计算风格的三维折线图与散点图，交互与视觉参考主流科学计算软件（如 MATLAB® 的 `plot3` / `scatter3`）。

控件完全基于 Avalonia 绘图 API 自绘，不依赖 SkiaSharp、不依赖反射、不使用运行时 XAML，可直接用于 **NativeAOT / 裁剪（Trimming）** 发布。

![浅色主题](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-light.png)

---

## 目录

- [1. 特性](#1-特性)
- [2. 环境要求](#2-环境要求)
- [3. 安装](#3-安装)
- [4. 快速开始](#4-快速开始)
- [5. 数据系列](#5-数据系列)
- [6. 颜色与调色板](#6-颜色与调色板)
- [7. 相机、投影与交互](#7-相机投影与交互)
- [8. 数据点交互与右键菜单](#8-数据点交互与右键菜单)
- [9. 坐标轴数值格式与标签排版](#9-坐标轴数值格式与标签排版)
- [10. 坐标轴、网格与刻度](#10-坐标轴网格与刻度)
- [11. 主题与字体](#11-主题与字体)
- [12. 图例](#12-图例)
- [13. 属性参考](#13-属性参考)
- [14. 方法参考](#14-方法参考)
- [15. 性能指南](#15-性能指南)
- [16. AOT 与裁剪](#16-aot-与裁剪)
- [17. 打包与发布 NuGet](#17-打包与发布-nuget)
- [18. 扩展自定义图形](#18-扩展自定义图形)
- [19. 常见问题](#19-常见问题)
- [20. 版本与许可](#20-版本与许可)

---

## 1. 特性

| 分类 | 能力 |
| --- | --- |
| 图形 | 三维折线（`LineSeries3D`）、三维散点（`ScatterSeries3D`），可组合绘制，支持最新点高亮 |
| 相机 | 轨道相机（方位角 / 仰角）、正交投影与透视投影、缩放、平移、双击复位 |
| 坐标系 | 自动取整的坐标范围、可指定每个轴两端的数值（X / Y 为左 / 右，Z 为下 / 上，支持反向轴）、数据超出时自动延伸、等长轴锁定、自动刻度与标签、可固定刻度小数位、轴标题、数值 / 日期 / 时间格式（支持毫秒） |
| 点交互 | 悬停高亮 + 自定义提示、十字准线 X/Y/Z 坐标读数、左键点选（事件返回点对象）、右键菜单（保存 PNG / 删除点 / 自定义菜单项） |
| 外观 | 背景色或完全透明背景、网格开关、坐标框开关、背面面板开关、刻度开关、轴标题开关、刻度与标题的偏移 / 间距 / 字号、图例位置（四角 / 四边 / 居中）与偏移、图例自动避让文字 |
| 配色 | 自动调色板 / 固定颜色 / 随机颜色，内置 7 套调色板，支持透明度与线型 |
| 交互 | 左键拖动旋转（左右 / 上下方向可反转）、右键/中键拖动平移、滚轮以光标为中心缩放 |
| 主题 | 内置浅色 / 深色主题，所有颜色均可单独覆盖 |
| 工程 | 场景缓存、批量几何绘制、文字布局缓存、无需反射，AOT / 裁剪安全 |

![深色主题](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-dark.png)

---

## 2. 环境要求

- .NET 8.0 / .NET 9.0 / .NET 10.0（库同时提供 `net8.0`、`net9.0`、`net10.0` 目标）
- Avalonia **12.1+**

> 控件目标框架与宿主应用无关：`net8.0` 宿主使用 `net8.0` 目标，`net9.0` / `net10.0` 宿主会优先匹配同名目标。

---

## 3. 安装

### 3.1 引用项目（本地开发）

直接把 `src/Plot3D.Avalonia/Plot3D.Avalonia.csproj` 加入你的解决方案，然后在宿主项目里添加项目引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\3D控件\src\Plot3D.Avalonia\Plot3D.Avalonia.csproj" />
</ItemGroup>
```

### 3.2 引用 NuGet 包（发布后）

```bash
dotnet add package Plot3D.Avalonia --version 1.3.0
```

或手工在 csproj 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Plot3D.Avalonia" Version="1.3.0" />
</ItemGroup>
```

---

## 4. 快速开始

### 4.1 引入 XAML 命名空间

控件库注册了专用 XML 命名空间：

```xml
xmlns:plot3d="https://github.com/daipeng0809/Plot3D.Avalonia"
```

### 4.2 XAML + 代码

**MainWindow.axaml**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:plot3d="https://github.com/daipeng0809/Plot3D.Avalonia"
        x:Class="MyApp.MainWindow"
        Title="三维绘图" Width="1000" Height="700">

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
      <Button Content="重置视角" Click="OnResetView" />
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
        PlotSeries();

        var plot = this.FindControl<Plot3DView>("Plot")!;
        // ……
    }

    private void PlotSeries()
    {
        var plot = this.FindControl<Plot3DView>("Plot")!;
        var series = new LineSeries3D { Title = "螺旋线", StrokeThickness = 1.6 };

        var points = new Point3D[720];
        for (var i = 0; i < points.Length; i++)
        {
            var t = i / (double)(points.Length - 1);
            var angle = t * Math.PI * 10.0;
            var radius = 0.05 + 0.95 * Math.Pow(t, 0.8);

            points[i] = new Point3D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle),
                2.0 * t);
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

### 4.3 纯代码创建（无 XAML）

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
    Title = "点云",
    MarkerSize = 4,
    MarkerShape = MarkerShape.Circle,
};

scatter.SetPoints(points);
plot.Series.Add(scatter);
```

---

## 5. 数据系列

### 5.1 折线：`LineSeries3D`

```csharp
var line = new LineSeries3D
{
    Title = "温度曲线",
    StrokeThickness = 1.8,
    LineStyle = LineStyle.Solid,
    MarkerSize = 0,          // > 0 时同时绘制顶点标记
    MarkerShape = MarkerShape.Circle,
    ColorMode = SeriesColorMode.Auto,
};

// 一次替换全部数据
line.SetPoints(points);

// 逐点追加（每次都会触发刷新，适合低频更新）
line.Add(new Point3D(1, 2, 3));

// 批量追加
line.AddRange(points);

// 清空
line.Clear();
```

**NaN 断线**：折线遇到 `NaN` / `Infinity` 坐标时会自动断开，可用于绘制分段曲线：

```csharp
line.SetPoints(new[]
{
    new Point3D(0, 0, 0),
    new Point3D(1, 1, 1),
    new Point3D(double.NaN, double.NaN, double.NaN), // 断开
    new Point3D(2, 0, 0),
    new Point3D(3, 1, 1),
});
```

**高性能批量写入**：直接操作 `Points` 列表后手动通知，可避免逐点触发刷新：

```csharp
line.Points.Clear();
foreach (var p in hugeBuffer)
{
    line.Points.Add(p);
}
line.InvalidateData();   // 只刷新一次
```

### 5.2 散点：`ScatterSeries3D`

```csharp
var scatter = new ScatterSeries3D
{
    Title = "采样点",
    MarkerSize = 5,
    MarkerShape = MarkerShape.Diamond,  // Circle / Square / Diamond / Triangle
    Opacity = 0.85,
};

scatter.SetPoints(points);
```

散点默认 `MarkerSize = 6`。所有点会合并进一个几何体一次性填充，因此数万点也能保持流畅。

![散点模式](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-scatter.png)

### 5.3 多系列组合

```csharp
plot.Series.Add(new LineSeries3D(p1) { Title = "曲线 A" });
plot.Series.Add(new LineSeries3D(p2) { Title = "曲线 B" });
plot.Series.Add(new ScatterSeries3D(p3) { Title = "散点", MarkerSize = 4 });
```

`Series` 是 `PlotSeries3DCollection`（继承自 `AvaloniaList<PlotSeries3D>`），支持 XAML 声明、集合变更通知与数据绑定。

### 5.4 系列通用属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Title` | `string?` | `null` | 图例名称 |
| `IsVisible` | `bool` | `true` | 是否参与绘制与范围计算 |
| `ColorMode` | `SeriesColorMode` | `Auto` | 取色方式 |
| `Color` | `Color` | `SteelBlue` | 固定颜色 |
| `Opacity` | `double` | `1.0` | 透明度（0..1） |
| `StrokeThickness` | `double` | `1.5` | 线宽（DIP） |
| `LineStyle` | `LineStyle` | `Solid` | `Solid` / `Dash` / `Dot` / `DashDot` |
| `MarkerSize` | `double` | `0`（散点 `6`） | 标记尺寸，0 表示不画点 |
| `MarkerShape` | `MarkerShape` | `Circle` | 标记形状 |
| `HighlightLatestPoint` | `bool` | `false` | 是否突出显示最新点（仅 `LineSeries3D` / `ScatterSeries3D`） |
| `LatestPointColor` | `Color` | `#FF3B30` | 最新点颜色 |
| `LatestPointSize` | `double` | `0` | 最新点尺寸，0 表示自动 |
| `LatestPointShape` | `MarkerShape` | `Circle` | 最新点形状 |

---

## 6. 颜色与调色板

### 6.1 三种取色方式

```csharp
// 1) 自动：按系列顺序从调色板取色；若显式设置过 Color 则优先使用 Color
series.ColorMode = SeriesColorMode.Auto;

// 2) 固定颜色
series.ColorMode = SeriesColorMode.Fixed;
series.Color = Colors.Crimson;

// 3) 随机颜色：每条系列只在首次绘制时生成一次，之后保持不变
series.ColorMode = SeriesColorMode.Random;
```

> `Auto` 模式下若显式赋值过 `series.Color`，则使用该颜色而不是调色板。需要强制走调色板时使用 `series.ClearValue(PlotSeries3D.ColorProperty)`。

### 6.2 内置调色板

| 调色板 | 风格 |
| --- | --- |
| `PlotPalette.Tab10` | Matplotlib tab10（默认） |
| `PlotPalette.Tab20` | Matplotlib tab20 |
| `PlotPalette.Classic` | 经典 7 色（科学计算软件常见默认配色） |
| `PlotPalette.Set1` | ColorBrewer Set1 |
| `PlotPalette.Pastel` | 柔和浅色 |
| `PlotPalette.Vivid` | 高对比 |
| `PlotPalette.Dark` | 深色背景专用亮色 |

```csharp
plot.Palette = PlotPalette.Classic;
```

> 以上调色板只包含颜色数值，不包含任何第三方代码。`Tab10` / `Tab20` 为 Matplotlib 默认配色，`Set1` 来自 ColorBrewer，色值均按公开信息整理。

也可以自定义任意调色板：

```csharp
plot.Palette = new[]
{
    Color.Parse("#2563EB"),
    Color.Parse("#F97316"),
    Color.Parse("#10B981"),
};
```

### 6.3 透明度

`Opacity` 会与颜色自身的 Alpha 相乘，实现半透明叠加效果：

```csharp
scatter.Opacity = 0.35;
```

---

## 7. 相机、投影与交互

### 7.1 方位角与仰角

相机语义与主流科学计算软件的 `view(az, el)` 一致：

- **方位角 `Azimuth`**：绕 Z 轴旋转，`0` 时相机位于 **-Y** 方向，正值逆时针（俯视）旋转；默认 `-37.5`。
- **仰角 `Elevation`**：与 XY 平面的夹角，范围 `-90..90`；默认 `30`。

```csharp
plot.Azimuth = -37.5;
plot.Elevation = 30;
```

### 7.2 投影方式

```csharp
plot.Projection = ProjectionMode.Orthographic; // 正交（默认）
plot.Projection = ProjectionMode.Perspective;  // 透视，有近大远小
```

### 7.3 缩放与平移

```csharp
plot.Zoom = 1.5;      // 缩放倍数（0.02 .. 100）
plot.PanX = 0;        // 只读，反映当前平移量
plot.ResetZoom();     // 复位缩放与平移
plot.ResetCamera();   // 复位方位角与仰角
plot.ResetView();     // 全部复位
```

### 7.4 鼠标 / 触摸操作

| 操作 | 行为 |
| --- | --- |
| 左键拖动 | 旋转（方位角 + 仰角，方向可通过 `IsHorizontalRotationReversed` / `IsVerticalRotationReversed` 反转） |
| 右键 / 中键拖动 | 平移 |
| 滚轮 | 以光标位置为中心缩放 |
| 双击 | 重置视角 |
| 悬停数据点 | 高亮该点并在鼠标旁显示自定义信息（可开关） |
| 在绘图区移动 | 显示十字准线与鼠标位置的三维坐标读数（可开关） |
| 左键单击数据点 | 选中该点并通过事件返回点对象（可开关） |
| 右键单击 | 打开上下文菜单（保存 PNG / 删除选中点 / 自定义项，可开关） |

交互可通过属性关闭：

```csharp
plot.IsRotationEnabled = false;
plot.IsPanEnabled = false;
plot.IsZoomEnabled = false;
plot.ResetOnDoubleClick = false;

plot.RotationSensitivity = 0.5;  // 度 / 像素
plot.ZoomSensitivity = 0.15;     // 每格滚轮的指数增量

// 拖动方向反转：默认向左拖动即向左旋转，开启后改为向右旋转
plot.IsHorizontalRotationReversed = true;   // 左右反转
plot.IsVerticalRotationReversed = true;     // 上下反转
```

---

## 8. 数据点交互与右键菜单

### 8.1 为数据点附加业务信息（Tag）

`LineSeries3D` / `ScatterSeries3D` 都派生自 `PointSeries3D`，可以在添加点时同步写入任意业务对象（Tag），
悬停提示、点选事件和右键菜单都能拿到该对象：

```csharp
var series = new ScatterSeries3D { Title = "温度采样", MarkerSize = 5 };

// 方式一：点与标签成对传入
var points = new[] { new Point3D(1, 2, 3), new Point3D(2, 4, 5) };
var tags = new object?[]
{
    $"传感器 A · {DateTime.Now:HH:mm:ss} · 23.5 ℃",
    $"传感器 A · {DateTime.Now:HH:mm:ss} · 24.1 ℃",
};
series.SetPoints(points, tags);

// 方式二：单点追加
series.Add(new Point3D(3, 6, 7), "传感器 A · 25.0 ℃");

// 方式三：直接维护 Tags 列表（与 Points 按索引对应）
series.Tags.Add("补充说明");
series.InvalidateData();
```

| 成员 | 说明 |
| --- | --- |
| `Points` | 底层 `List<Point3D>`，与标签按索引一一对应 |
| `Tags` | 底层 `List<object?>`，缺失项为 null（提示自动回退为坐标文本） |
| `SetPoints(points, tags)` | 同时替换点与标签 |
| `Add(point, tag)` | 追加一个带标签的点 |
| `AddRange(points, tags)` | 批量追加点与标签 |
| `RemoveAt(index)` | 删除指定点（同时删除标签），成功返回 true |
| `GetTag(index)` / `SetTag(index, tag)` | 读取 / 写入指定点的标签 |

> Tag 只需重写 `ToString()` 即可控制默认提示内容，无需反射或序列化。

### 8.2 悬停高亮与提示

鼠标移动到数据点附近时，控件会高亮距离最近的可见点，并在鼠标旁显示提示框：

```csharp
plot.IsPointHoverEnabled = true;      // 总开关
plot.IsHoverToolTipEnabled = true;    // 是否显示提示框
plot.HoverRadius = 10;                // 命中半径（像素）
plot.HoverHighlightColor = Color.Parse("#FF9F0A");
plot.HoverHighlightSize = 0;          // 0 = 自动（普通标记尺寸 + 4）
```

提示文本的取值顺序：

1. `HoverTextFormatter` 回调（返回非空字符串时生效）；
2. 数据点的 `Tag.ToString()`；
3. 当前轴格式下的 `X / Y / Z` 坐标文本。

```csharp
plot.HoverTextFormatter = info =>
    $"{info.Series.Title}\n第 {info.PointIndex} 点\n" +
    $"X = {plot.FormatXAxisValue(info.Position.X)}\n" +
    $"Y = {plot.FormatYAxisValue(info.Position.Y)}\n" +
    $"Z = {plot.FormatZAxisValue(info.Position.Z)}";
```

提示框外观可单独覆盖：

```csharp
plot.ToolTipBackground = new SolidColorBrush(Color.FromArgb(235, 32, 32, 40));
plot.ToolTipBorderBrush = new SolidColorBrush(Color.Parse("#50505A"));
plot.ToolTipForeground = Brushes.White;
```

> 命中检测会遍历所有可见点的屏幕投影，数万点规模仍可在鼠标移动时实时完成；极点规模的超大数据建议在业务层降采样。

### 8.3 十字准线与坐标读数

鼠标在绘图区移动时，可以显示过鼠标位置的三维指示线（分别指向三个远墙）以及 X / Y / Z 读数框：

```csharp
plot.IsCrosshairEnabled = true;       // 总开关
plot.ShowCrosshairLabels = true;      // 是否显示读数框
plot.CrosshairBrush = new SolidColorBrush(Color.Parse("#7A7A88"));
```

读数使用与坐标轴相同的数值 / 日期 / 时间格式（见 [第 9 节](#9-坐标轴数值格式与标签排版)）。
准线由鼠标位置构造视线射线，与坐标框 AABB 求交：交点始终位于光标射线上，投影与光标重合，
不会因逐轴裁剪而偏移；射线未命中坐标框或交点偏离视图平面过远时，准线与读数自动隐藏。

### 8.4 点击选中数据点

```csharp
plot.IsPointSelectionEnabled = true;
plot.SelectionRadius = 12;                                  // 点击命中半径
plot.SelectionHighlightColor = Color.Parse("#0A84FF");       // 与悬停高亮颜色区分
plot.SelectionHighlightSize = 0;                             // 0 = 自动

plot.PointSelected += (_, e) =>
{
    var info = e.Point;          // 命中结果
    var series = info.Series;    // 所在系列
    var index = info.PointIndex; // 点在系列中的索引
    var position = info.Position;// 三维坐标
    var tag = info.Tag;          // 添加点时同步写入的业务对象
};

plot.PointDeselected += (_, e) =>
{
    // 点击空白处、删除点或数据变化导致选中失效时触发
};

// 代码主动选中 / 取消 / 删除
plot.SelectPoint(series, 10);
plot.ClearSelection();
plot.DeleteSelectedPoint();
```

点击空白处会清除当前选中；鼠标悬停高亮（琥珀色）与选中高亮（蓝色）使用不同颜色与样式，可同时辨认。

### 8.5 右键菜单

右键单击（未拖动）会打开上下文菜单，内置项：

- **保存图像 (PNG)**：弹出系统保存对话框并导出当前视图；
- **删除选中点**：仅在存在选中点时出现，删除后触发 `PointDeselected`。

追加自定义菜单项：

```csharp
var exportCsv = new MenuItem { Header = "导出数据 (CSV)" };
exportCsv.Click += (_, _) => ExportCsv();
plot.ContextMenuItems.Add(exportCsv);
```

按点击位置动态构建菜单（例如右键命中某个点时提供专属操作）：

```csharp
plot.ContextMenuBuilding += (_, e) =>
{
    if (e.HitPoint is { } hit)
    {
        var item = new MenuItem { Header = $"定位到 {hit.Series.Title}[{hit.PointIndex}]" };
        item.Click += (_, _) => SelectAndZoom(hit);
        e.Items.Add(item);
    }
};
```

右键单击数据点时会先自动选中该点，再打开菜单。

### 8.6 导出 PNG 与透明背景

```csharp
// 直接写入文件（scale = 2 表示 2 倍高清渲染）
await plot.SaveImageAsync("chart.png", scale: 2.0);

// 弹出保存对话框
await plot.SaveImageWithDialogAsync();
```

> 导出的 PNG 只包含图表本身，不包含悬停 / 选中高亮、十字准线或提示框等交互浮层。

如果需要把控件叠加在其它控件或图片上，可关闭背景填充：

```csharp
plot.IsBackgroundTransparent = true;
```

开启后控件不再绘制自己的背景色；`ShowPane = true` 时墙面也只在显式设置 `PaneBrush` 后才会填充。

### 8.7 最新点高亮（识别曲线方向）

```csharp
var line = new LineSeries3D
{
    HighlightLatestPoint = true,          // 用不同样式显示最新的一个有效点
    LatestPointColor = Color.Parse("#FF3B30"),
    LatestPointSize = 0,                  // 0 = 自动（普通标记的 1.8 倍，且不小于 7）
    LatestPointShape = MarkerShape.Circle,
};
```

该样式对折线与散点均生效，且会跳过末尾的 NaN 点，便于实时数据场景中快速识别数据增长方向。

---

## 9. 坐标轴数值格式与标签排版

### 9.1 日期 / 时间坐标轴

时间类格式要求轴数值为 **OLE Automation 日期**（`DateTime.ToOADate()`，单位：天）：

```csharp
var timestamp = new DateTime(2026, 10, 11, 13, 22, 5, 123);
series.Add(new Point3D(timestamp.ToOADate(), value, depth), $"采样时间 {timestamp:HH:mm:ss.fff}");

plot.XAxisValueFormat = AxisValueFormat.DateTime;  // 2026-10-11 13:22:05.123
plot.AxisTimeMilliseconds = true;                  // 时间格式输出毫秒
```

| `AxisValueFormat` | 标签示例 | 说明 |
| --- | --- | --- |
| `Numeric` | `1.25` | 默认数值格式 |
| `Date` | `2026-10-11` | 仅日期 |
| `Time` | `13:22:05` / `13:22:05.123` | 仅时间，开启毫秒后带 `.fff` |
| `DateTime` | `2026-10-11 13:22:05` | 日期 + 时间 |

时间轴的刻度会自动选择 1 ms / 5 ms / 1 s / 15 s / 1 min / 1 h / 1 天等“整齐”的步长，
并在步长小于 1 秒时自动输出毫秒；X / Y / Z 可分别设置格式：

```csharp
plot.XAxisValueFormat = AxisValueFormat.DateTime;
plot.YAxisValueFormat = AxisValueFormat.Numeric;
plot.ZAxisValueFormat = AxisValueFormat.Time;
```

> 为避免标签重复：`Date` 格式在数据跨度不足一天时会自动附带时间，`Time` 格式在跨度超过一天时会自动附带日期。
> 周步长对齐到周一，月 / 年步长对齐到月初 / 1 月 1 日（多年步长以 2000 年 1 月为锚点）。

读取格式化文本（悬停提示、自定义标注等场景）：

```csharp
var text = plot.FormatXAxisValue(46277.556999...);   // 按 X 轴格式输出
```

> 标签变长时请适当增大 `PlotPadding`，避免文字被裁剪。

### 9.2 刻度与轴标题排版

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TickLength` | `double` | `4` | 刻度线长度（像素） |
| `TickLabelGap` | `double` | `3` | 刻度文本与刻度线之间的距离，可为负值 |
| `TickLabelOffset` | `Vector` | `0,0` | 所有刻度文本的整体偏移（屏幕坐标） |
| `TickLabelFontSize` | `double` | `NaN` | 刻度文本字号；`NaN` 表示使用 `FontSize` |
| `AxisTitleGap` | `double` | `0` | 轴标题与刻度文本之间的额外间距 |
| `AxisTitleOffset` | `Vector` | `0,0` | 所有轴标题的整体偏移 |
| `AxisTitleFontSize` | `double` | `NaN` | 轴标题字号；`NaN` 表示使用 `FontSize` |

```csharp
plot.TickLength = 6;
plot.TickLabelGap = 5;
plot.TickLabelOffset = new Vector(0, 2);
plot.TickLabelFontSize = 11;
plot.AxisTitleGap = 6;
plot.AxisTitleOffset = new Vector(0, -2);
plot.AxisTitleFontSize = 13;
```

多行或超长刻度文本仍通过 `PlotPadding` 预留空间。

> 刻度文本会自动避让：不同坐标轴在角点处的标签沿各自的向外方向轻微外移；同一轴上过密的标签（轴太短）会自动省略，避免文字叠字。
> 刻度文本与轴标题始终绘制在数据曲线之上，不会被数据遮挡。

---

## 10. 坐标轴、网格与刻度

### 10.1 自动范围

默认根据所有可见系列的数据包围盒自动计算范围，并对齐到“好看”的刻度（1 / 2 / 2.5 / 5 × 10ⁿ）。
空数据时显示 `0..1` 的默认坐标框；所有点重合时会自动扩展出可见范围。

### 10.2 指定轴两端（左 / 右、下 / 上）

每个坐标轴用“屏幕上看到的两端”来指定数值，`NaN` 表示自动：

| 轴 | 属性 | 含义 |
| --- | --- | --- |
| X | `XAxisLeft` / `XAxisRight` | X 轴线在默认视角下的左 / 右两端 |
| Y | `YAxisLeft` / `YAxisRight` | Y 轴线在默认视角下的左（左上）/ 右（右下）两端 |
| Z | `ZAxisBottom` / `ZAxisTop` | Z 轴线的底 / 顶两端 |

```csharp
plot.XAxisLeft = -1;
plot.XAxisRight = 1;

plot.ZAxisBottom = 0;
plot.ZAxisTop = 2;

// 只设置一端也可以，另一端继续自动计算
plot.YAxisLeft = 41;

// 恢复自动
plot.XAxisLeft = double.NaN;
```

**反向坐标轴**：把较大的值写到左端（Z 轴写到下端）即得到反向轴，刻度仍按数值生成，但在屏幕上递减显示：

```csharp
plot.XAxisLeft = 10;   // X 轴左端 10
plot.XAxisRight = 0;   // 右端 0，刻度自左向右为 10 → 0

plot.ZAxisBottom = 2;  // Z 轴底端 2
plot.ZAxisTop = 0;     // 顶端 0，刻度自下向上为 2 → 0
```

> - “左 / 右、下 / 上”以默认相机（`Azimuth = -37.5`、`Elevation = 30`）的显示方向为准；旋转相机后同一个轴端可能出现在屏幕另一侧，但属性始终代表同一个轴端。
> - 默认视角下 Y 轴自右下方向左上方延伸，因此 Y 轴左端默认显示较大值；`YAxisLeft` 小于 `YAxisRight` 即反向 Y 轴。
> - 设定值只作为“期望范围”：如果数据超出该端，会自动延伸并重新取整，图形不会跑到坐标框外。
> - 旧版的最小 / 最大值属性（`XAxisMinimum`、`YAxisMaximum` 等）仍可使用，已标记为 `Obsolete` 并自动映射到对应轴端；它们仍是 `StyledProperty`，可继续用于数据绑定、样式与动画，建议后续逐步迁移。映射为单向（旧属性 → 新轴端），直接修改新轴端不会回写旧属性。

### 10.3 刻度数量

```csharp
plot.TickCount = 6;   // 每个轴期望的刻度数量（含两端），有效范围 2..20
```

刻度文本会根据步长自动选择小数位，极大 / 极小数值自动切换为科学计数法；
需要固定小数位时使用 `XAxisTickDecimals` 等属性（见 [10.6](#106-刻度小数位)）。

### 10.4 显示开关

```csharp
plot.ShowGrid = true;         // 背景网格线
plot.ShowBox = true;          // 坐标框（12 条棱）
plot.ShowPane = false;        // 背面墙填充
plot.ShowTicks = true;        // 刻度线
plot.ShowTickLabels = true;   // 刻度文本
plot.ShowAxisTitles = true;   // 轴标题

plot.XAxisTitle = "X (m)";
plot.YAxisTitle = "Y (m)";
plot.ZAxisTitle = "Z (m)";
```

### 10.5 留白

刻度标签与轴标题绘制在坐标框外侧，通过 `PlotPadding` 预留空间：

```csharp
plot.PlotPadding = new Thickness(56, 40, 56, 40); // 左、上、右、下
```

当字号较大或标签很长时，适当增大留白。

### 10.6 刻度小数位

```csharp
plot.XAxisTickDecimals = 3;    // 0.100 / 0.120 / ...
plot.YAxisTickDecimals = 1;    // 40.0 / 40.2 / ...
plot.ZAxisTickDecimals = 0;    // 只保留整数：0 / 5000 / ...
plot.XAxisTickDecimals = double.NaN;  // 恢复按步长自动（默认）
```

- 取值 0..8，`0` 表示只保留整数，`NaN` 表示按步长自动选择；
- 只影响标签文本，不会改变刻度数值本身；
- 仅对数值轴生效，日期 / 时间轴仍使用对应的时间格式。

### 10.7 等长轴锁定（正方体比例）

各轴数量级相差悬殊时（例如 X 为 `0.1..0.2`、Y 为 `40..41`、Z 为 `0..20000`），按真实比例绘制会让坐标框退化成一条线。
开启 `IsAspectRatioLocked` 后，三个轴会被分别缩放到相同的显示长度，坐标框显示为正方体：

```csharp
plot.IsAspectRatioLocked = true;
```

- 只改变各轴的显示长度，刻度数值与标签仍按各轴真实范围排列；
- 与轴两端设置、日期时间轴、正交 / 透视投影与反向轴均兼容；
- 默认 `false`，保持各轴真实比例。

![等长轴锁定](https://cdn.jsdelivr.net/gh/daipeng0809/Plot3D.Avalonia@master/docs/screenshot-aspect-lock.png)

---

## 11. 主题与字体

### 11.1 内置主题

```csharp
plot.ColorTheme = PlotTheme.Light;  // 白色背景、深灰文字
plot.ColorTheme = PlotTheme.Dark;   // 深色背景、浅色文字
```

主题只影响**未显式设置**的颜色。

### 11.2 单独覆盖颜色

```csharp
plot.Background = new SolidColorBrush(Color.Parse("#0F172A"));
plot.Foreground = Brushes.White;
plot.AxisBrush = new SolidColorBrush(Color.Parse("#94A3B8"));
plot.GridBrush = new SolidColorBrush(Color.Parse("#334155"));
plot.PaneBrush = new SolidColorBrush(Color.Parse("#1E293B"));
plot.LegendBackground = new SolidColorBrush(Color.FromArgb(210, 15, 23, 42));

// 完全透明背景（叠在其他内容上，不填充任何底色）
plot.IsBackgroundTransparent = true;
```

### 11.3 字体

```csharp
plot.FontFamily = new FontFamily("avares://MyApp/Assets/Fonts/AlibabaPuHuiTi.ttf#Alibaba PuHuiTi 3.0");
plot.FontSize = 13;
```

字体只影响刻度、轴标题与图例文本。

---

## 12. 图例

```csharp
plot.ShowLegend = true;
series.Title = "曲线 A";
```

- 只有设置了非空 `Title` 且 `IsVisible = true` 的系列才会出现在图例中。
- 图例位置由 `LegendPosition` 控制（默认左上角），支持四角、四边与居中 9 个位置：

| `LegendPosition` | 位置 |
| --- | --- |
| `TopLeft` / `TopCenter` / `TopRight` | 上排 |
| `MiddleLeft` / `Center` / `MiddleRight` | 中排 |
| `BottomLeft` / `BottomCenter` / `BottomRight` | 下排 |

```csharp
plot.LegendPosition = LegendPosition.BottomRight;
plot.LegendOffset = new Vector(8, -4);   // 相对所选位置的像素偏移
```

- 背景色由 `LegendBackground` 控制（未设置时使用主题色）。
- `LegendOffset` 为屏幕坐标方向的像素偏移（X 向右、Y 向下），结果会自动限制在控件范围内。
- 默认开启图例自动避让（`IsLegendAvoidanceEnabled = true`）：图例若会压住刻度文本或轴标题，会从上一帧位置按最小位移推出，空间过窄时自动压缩行距（行高按实测文本高度计算），仍放不下时选择重叠最少的位置；图例尽可能保持在控件内，若图例本身大于控件则会被裁切。拖动旋转 / 缩放时位置保持稳定，障碍消失后会平滑回到配置位置。需要严格按 `LegendPosition` 摆放时可关闭：

```csharp
plot.IsLegendAvoidanceEnabled = false;
```

---

## 13. 属性参考

### 13.1 `Plot3DView`

#### 数据与配色

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Series` | `PlotSeries3DCollection` | 空集合 | 数据系列集合 |
| `Palette` | `IReadOnlyList<Color>?` | `PlotPalette.Tab10` | 自动取色调色板 |
| `Background` | `IBrush?` | 主题背景 | 背景画刷 |
| `Foreground` | `IBrush?` | 主题文字色 | 刻度 / 标题 / 图例文字 |
| `AxisBrush` | `IBrush?` | 主题坐标轴色 | 坐标框与刻度线 |
| `GridBrush` | `IBrush?` | 主题网格色 | 网格线与图例边框 |
| `PaneBrush` | `IBrush?` | 主题面板色 | 背面墙填充 |
| `LegendBackground` | `IBrush?` | 半透明主题色 | 图例背景 |
| `LegendPosition` | `LegendPosition` | `TopLeft` | 图例位置（四角 / 四边 / 居中） |
| `LegendOffset` | `Vector` | `0,0` | 图例相对所选位置的像素偏移 |
| `IsLegendAvoidanceEnabled` | `bool` | `true` | 图例自动避让刻度文本与轴标题 |
| `ColorTheme` | `PlotTheme` | `Light` | 浅色 / 深色主题 |

#### 显示开关

| 属性 | 类型 | 默认值 |
| --- | --- | --- |
| `ShowBox` | `bool` | `true` |
| `ShowGrid` | `bool` | `true` |
| `ShowPane` | `bool` | `false` |
| `ShowTicks` | `bool` | `true` |
| `ShowTickLabels` | `bool` | `true` |
| `ShowAxisTitles` | `bool` | `true` |
| `ShowLegend` | `bool` | `false` |

#### 坐标轴

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `XAxisTitle` / `YAxisTitle` / `ZAxisTitle` | `string?` | `"X"` / `"Y"` / `"Z"` | 轴标题，设为空字符串可隐藏 |
| `XAxisLeft` / `XAxisRight` | `double` | `NaN` | X 轴左 / 右两端数值，`NaN` 表示自动；左端大于右端即反向轴 |
| `YAxisLeft` / `YAxisRight` | `double` | `NaN` | Y 轴左（左上）/ 右（右下）两端数值；默认左端为较大值 |
| `ZAxisBottom` / `ZAxisTop` | `double` | `NaN` | Z 轴底 / 顶两端数值；底端大于顶端即反向轴 |
| `IsAspectRatioLocked` | `bool` | `false` | 三轴等长显示（正方体比例），刻度仍按真实数值排列 |
| `TickCount` | `int` | `6` | 期望刻度数量（2..20） |
| `PlotPadding` | `Thickness` | `56,40,56,40` | 绘图区留白 |

#### 相机与交互

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Azimuth` | `double` | `-37.5` | 方位角（度） |
| `Elevation` | `double` | `30` | 仰角（度，-89.9..89.9） |
| `Zoom` | `double` | `1.0` | 缩放倍数 |
| `Projection` | `ProjectionMode` | `Orthographic` | 投影方式 |
| `IsRotationEnabled` | `bool` | `true` | 是否允许拖动旋转 |
| `IsHorizontalRotationReversed` | `bool` | `false` | 是否反转左右拖动时的旋转方向 |
| `IsVerticalRotationReversed` | `bool` | `false` | 是否反转上下拖动时的旋转方向 |
| `IsPanEnabled` | `bool` | `true` | 是否允许拖动平移 |
| `IsZoomEnabled` | `bool` | `true` | 是否允许滚轮缩放 |
| `ResetOnDoubleClick` | `bool` | `true` | 双击是否复位 |
| `RotationSensitivity` | `double` | `0.5` | 旋转灵敏度（度 / 像素） |
| `ZoomSensitivity` | `double` | `0.15` | 缩放灵敏度 |
| `PanX` / `PanY` | `double` | 只读 | 当前平移量（像素） |

#### 数据点交互

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `IsPointHoverEnabled` | `bool` | `true` | 是否启用悬停高亮与提示 |
| `HoverRadius` | `double` | `10` | 悬停命中半径（像素） |
| `HoverHighlightColor` | `Color` | `#FF9F0A` | 悬停高亮颜色 |
| `HoverHighlightSize` | `double` | `0` | 悬停高亮尺寸，0 表示自动 |
| `IsHoverToolTipEnabled` | `bool` | `true` | 是否显示悬停提示框 |
| `ToolTipBackground` / `ToolTipBorderBrush` / `ToolTipForeground` | `IBrush?` | 主题色 | 提示框配色 |
| `HoverTextFormatter` | `Func<PlotPointInfo, string?>?` | `null` | 自定义提示文本，返回 null 时回退到 Tag / 坐标 |
| `HoveredPoint` | `PlotPointInfo?` | 只读 | 当前悬停命中的数据点 |
| `IsPointSelectionEnabled` | `bool` | `true` | 是否启用点击选中 |
| `SelectionRadius` | `double` | `12` | 点击命中半径（像素） |
| `SelectionHighlightColor` | `Color` | `#0A84FF` | 选中高亮颜色 |
| `SelectionHighlightSize` | `double` | `0` | 选中高亮尺寸，0 表示自动 |
| `SelectedPoint` | `PlotPointInfo?` | 只读 | 当前选中的数据点 |
| `IsCrosshairEnabled` | `bool` | `true` | 是否显示十字准线 |
| `ShowCrosshairLabels` | `bool` | `true` | 是否显示十字准线的坐标读数框 |
| `CrosshairBrush` | `IBrush?` | 主题坐标轴色 | 十字准线颜色 |
| `IsContextMenuEnabled` | `bool` | `true` | 是否启用右键菜单 |
| `ContextMenuItems` | `AvaloniaList<Control>` | 空集合 | 追加的自定义右键菜单项 |

#### 坐标轴格式与排版

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `XAxisValueFormat` / `YAxisValueFormat` / `ZAxisValueFormat` | `AxisValueFormat` | `Numeric` | 数值 / 日期 / 时间 / 日期 + 时间 |
| `AxisTimeMilliseconds` | `bool` | `false` | 时间格式是否输出毫秒 |
| `XAxisTickDecimals` / `YAxisTickDecimals` / `ZAxisTickDecimals` | `double` | `NaN` | 刻度标签固定小数位，`0` = 只保留整数，`NaN` = 自动 |
| `TickLength` | `double` | `4` | 刻度线长度（像素） |
| `TickLabelGap` | `double` | `3` | 刻度文本距刻度线的距离（像素） |
| `TickLabelOffset` | `Vector` | `0,0` | 刻度文本整体偏移 |
| `TickLabelFontSize` | `double` | `NaN` | 刻度文本字号，`NaN` 表示使用 `FontSize` |
| `AxisTitleGap` | `double` | `0` | 轴标题额外间距（像素） |
| `AxisTitleOffset` | `Vector` | `0,0` | 轴标题整体偏移 |
| `AxisTitleFontSize` | `double` | `NaN` | 轴标题字号，`NaN` 表示使用 `FontSize` |
| `IsBackgroundTransparent` | `bool` | `false` | 是否使用完全透明背景 |

#### 字体

| 属性 | 类型 | 默认值 |
| --- | --- | --- |
| `FontFamily` | `FontFamily` | `FontFamily.Default` |
| `FontSize` | `double` | `12` |

---

## 14. 方法参考

### 14.1 `Plot3DView`

| 方法 | 说明 |
| --- | --- |
| `ResetView()` | 复位方位角、仰角、缩放与平移 |
| `ResetCamera()` | 只复位方位角与仰角 |
| `ResetZoom()` | 只复位缩放与平移 |
| `InvalidatePlot()` | 强制重新计算自动范围并重绘（直接修改 `Points` 后调用） |
| `TryHitTest(position, radius, out info)` | 命中测试，返回半径内最近的数据点 |
| `SelectPoint(series, index)` | 按系列与索引选中数据点 |
| `ClearSelection()` | 清除选中 |
| `DeleteSelectedPoint()` | 删除选中的点 |
| `SaveImageAsync(path, scale)` | 把当前视图渲染为 PNG 写入文件 |
| `SaveImageWithDialogAsync()` | 弹出系统保存对话框并导出 PNG |
| `FormatXAxisValue` / `FormatYAxisValue` / `FormatZAxisValue` | 按对应轴的数值格式输出文本 |

与数据点相关的事件：

| 事件 | 说明 |
| --- | --- |
| `PointSelected` | 点击（或代码）选中数据点，参数包含 `PlotPointInfo` |
| `PointDeselected` | 选中被取消、删除或数据变化导致失效 |
| `ContextMenuBuilding` | 右键菜单构建时触发，可按点击位置动态增删菜单项 |

### 14.2 `PointSeries3D`（`LineSeries3D` / `ScatterSeries3D`）

| 方法 / 属性 | 说明 |
| --- | --- |
| `SetPoints(IEnumerable<Point3D>)` | 替换全部数据并刷新 |
| `SetPoints(IEnumerable<Point3D>, IEnumerable<object?>?)` | 同时替换数据与逐点标签 |
| `SetPoints(ReadOnlySpan<Point3D>)` | 替换全部数据（零分配重载） |
| `Add(Point3D)` / `Add(Point3D, object?)` | 追加一个点（可带标签）并刷新 |
| `AddRange(IEnumerable<Point3D>)` / `AddRange(points, tags)` | 批量追加点与标签 |
| `RemoveAt(int)` | 删除指定点（含标签） |
| `Clear()` | 清空数据 |
| `GetTag(int)` / `SetTag(int, object?)` | 读取 / 写入逐点标签 |
| `InvalidateData()` | 直接修改 `Points` 后手动触发刷新 |
| `TryFindNearestPoint(projection, position, out index, out distance)` | 在屏幕空间查找最近点 |
| `GetBounds()` | 返回数据包围盒（`BoundingBox3D`） |
| `HighlightLatestPoint` / `LatestPointColor` / `LatestPointSize` / `LatestPointShape` | 最新点高亮样式 |
| `Points` / `Tags` / `Count` | 底层点列表、标签列表与点数 |

属性 `Points` 返回底层 `List<Point3D>`，允许直接读写以换取最高性能；批量修改后记得调用 `InvalidateData()`。

---

## 15. 性能指南

### 15.1 已内置的优化

- **场景缓存**：只有当相机、尺寸、数据或相关属性变化时才重新计算投影与刻度，纯重绘不会重复计算。
- **批量几何**：整条折线合并为一个 `StreamGeometry` 一次描绘；所有散点合并为一个几何体一次填充，避免数万次 `DrawLine` / `DrawEllipse` 调用。
- **屏幕抽稀**：相邻点在屏幕上的距离小于 0.05 DIP 时自动跳过，大幅减少几何数据量。
- **小标记优化**：尺寸 ≤ 2.5 DIP 的标记使用四边形而非多边形，减少路径节点。
- **文字缓存**：刻度与标题的 `TextLayout` 只在字体、字号或前景色变化时重建。
- **画刷缓存**：主题画刷只在颜色属性变化时重建。
- **命中屏幕坐标缓存**：每个系列按当前投影缓存全部点的屏幕坐标，鼠标移动时只做距离比较；
  首次投影后不再重复投影全部点（十万点级实测命中查询约 1.5 ms/次）。
- **无反射 / 无 LINQ 热路径**：渲染循环使用 `Span<T>` 与索引遍历。

### 15.2 使用建议

| 场景 | 建议 |
| --- | --- |
| 实时数据（每秒多次更新） | 复用 `series.Points`，更新后调用一次 `InvalidateData()` |
| 数万点以上 | 优先使用 `ScatterSeries3D`（几何合并更彻底） |
| 超长折线（> 20 万点） | 在业务层降采样，或按屏幕像素抽稀后再传入 |
| 追求极致帧率 | 关闭 `ShowGrid` / `ShowTicks`，减少绘制批次 |
| 频繁切换数据 | 避免反复 `Series.Clear()`，复用系列对象并调用 `SetPoints` |

### 15.3 测量示例

```csharp
var sw = Stopwatch.StartNew();
series.SetPoints(points);   // 触发一次刷新
plot.InvalidatePlot();
sw.Stop();
Console.WriteLine($"数据更新耗时 {sw.Elapsed.TotalMilliseconds:F2} ms");
```

---

## 16. AOT 与裁剪

控件库本身已开启：

```xml
<IsAotCompatible>true</IsAotCompatible>
<IsTrimmable>true</IsTrimmable>
<EnableAotAnalyzer>true</EnableAotAnalyzer>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
```

控件全部使用强类型 `AvaloniaProperty` 注册与代码绘制，没有 `TypeConverter` 反射、没有运行时 XAML、没有动态绑定，因此在 NativeAOT 与裁剪下无需额外配置。

宿主应用发布 NativeAOT：

```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true
```

宿主 csproj 建议：

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

> 若宿主在 XAML 中使用本控件，请确保 Avalonia 12 的 XAML 编译正常（默认 `AvaloniaUseCompiledBindingsByDefault=true`）；本库自身不包含任何 `.axaml` 资源。

---

## 17. 打包与发布 NuGet

### 17.1 本地打包

```bash
dotnet pack src/Plot3D.Avalonia/Plot3D.Avalonia.csproj -c Release -o ./artifacts
```

输出：

```
artifacts/
├── Plot3D.Avalonia.1.3.0.nupkg    —— 主包
└── Plot3D.Avalonia.1.3.0.snupkg   —— 符号包（供 nuget.org 符号服务器使用，发布时随主包一并推送）
```

`SymbolPackageFormat` 为 `snupkg`，因此只产出 `.snupkg`，不会再额外生成 `.symbols.nupkg`。

包内包含：

- `lib/net8.0/Plot3D.Avalonia.dll`
- `lib/net9.0/Plot3D.Avalonia.dll`
- `lib/net10.0/Plot3D.Avalonia.dll`
- `lib/*/Plot3D.Avalonia.xml`（完整 XML 文档）
- `README.md`
- `README.en.md`
- `icon.png`

### 17.2 本地测试包

```bash
dotnet nuget add source ./artifacts --name Plot3DLocal
dotnet add package Plot3D.Avalonia --version 1.3.0 --source Plot3DLocal
```

### 17.3 发布到 nuget.org

仓库内置了发布流水线 `.github/workflows/publish.yml`，推荐用它发布。

**方式一：GitHub Actions 自动发布（受信任发布，无需保管任何密钥）**

1. 在 nuget.org 的 **Trusted Publishing** 页面创建策略：
   - Package Owner：你的账号
   - CI/CD Provider：`GitHub Actions`
   - Repository Owner / Repository：GitHub 用户名 / 仓库名
   - Workflow File：`publish.yml`（**只填文件名**，不含 `.github/workflows/` 路径）
2. 触发发布，二选一：
   - 推送标签：`git tag v1.3.0` → `git push origin v1.3.0`，版本号自动取自标签
   - 在 Actions 页面手动运行 `Publish to NuGet`，可通过输入框临时覆盖版本号

流水线内部用 `NuGet/login@v1` 把 GitHub OIDC token 换成有效期 **1 小时**的临时 API Key，仓库和 Secrets 里都不保存长期密钥。

**方式二：本地手动推送**

```bash
# 1. 在 https://www.nuget.org 的 API Keys 页面创建长期密钥
# 2. 推送
dotnet nuget push ./artifacts/Plot3D.Avalonia.1.3.0.nupkg \
  --api-key <YOUR_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

发布前建议确认 `Plot3D.Avalonia.csproj` 中的：

```xml
<Authors>你的名字或团队</Authors>
<PackageProjectUrl>项目主页</PackageProjectUrl>
<RepositoryUrl>Git 仓库地址</RepositoryUrl>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<Version>1.3.0</Version>
```

> 版本号一经发布不可删除或覆盖，只能 unlist。需要修复时必须升版本号重新发布。

### 17.4 版本号约定

遵循语义化版本：

- `1.0.x`：缺陷修复
- `1.x.0`：向后兼容的新功能
- `2.0.0`：不兼容的 API 变更

---

## 18. 扩展自定义图形

内置只有折线与散点，后续可通过继承 `PlotSeries3D` 扩展曲面、柱状、矢量场等图形。

### 18.1 扩展点

```csharp
public abstract class PlotSeries3D : AvaloniaObject
{
    // 必须实现：返回数据包围盒，用于自动计算坐标轴范围
    public abstract BoundingBox3D GetBounds();

    // 必须实现：把图形绘制到控件
    protected internal abstract void Render(SeriesRenderContext context);

    // 可选：数据变化时通知控件
    protected void RaiseDataChanged();
}
```

`SeriesRenderContext` 提供：

| 成员 | 说明 |
| --- | --- |
| `DrawingContext` | 当前 Avalonia 绘图表面 |
| `Projection` | 三维到二维投影器（`TryProject` / `Project` / `TryUnproject` / `TryIntersectBounds`） |
| `Color` | 已解析的系列颜色（含取色模式） |
| `Opacity` | 系列透明度 |
| `ViewBounds` | 控件绘制区域 |
| `CreateBrush()` | 用系列颜色创建画刷 |
| `CreatePen(thickness, style)` | 用系列颜色创建画笔 |

### 18.2 示例：三维柱状系列

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

            // 顶面四角
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

使用：

```csharp
var bars = new BarSeries3D { Title = "柱状", ColorMode = SeriesColorMode.Fixed, Color = Colors.SteelBlue };
bars.AddBar(new Point3D(0, 0, 0), 0.4);
bars.AddBar(new Point3D(1, 0, 0), 0.6);
plot.Series.Add(bars);
```

> 外部程序集重写 `Render` 时使用 `protected override`（`protected internal` 在外部程序集等价于 `protected`）。

### 18.3 建议

- 尽量把同类图元合并到一个 `StreamGeometry` 中一次性绘制。
- 避免在 `Render` 中分配大数组；如需缓存，使用字段并在 `DataChanged` 时失效。
- 及时在数据变化后调用 `RaiseDataChanged()`，否则自动范围不会更新。

---

## 19. 常见问题

**Q1：为什么坐标轴标签的重叠 / 留白不理想？**

调整 `PlotPadding`（增大四周留白）或 `TickCount`（减少刻度数）。字号较大时建议同时增大 `PlotPadding`。

**Q2：如何完全隐藏坐标轴，只显示数据？**

```csharp
plot.ShowBox = false;
plot.ShowGrid = false;
plot.ShowTicks = false;
plot.ShowAxisTitles = false;
```

**Q3：如何在 MVVM 中绑定数据？**

将 `Plot3DView.Series` 绑定到 ViewModel 中的 `PlotSeries3DCollection`：

```xml
<plot3d:Plot3DView Series="{Binding Series}" ShowLegend="True" />
```

`PlotSeries3DCollection` 继承自 `AvaloniaList<T>`，支持集合变更通知。

**Q4：如何在 XAML 中声明系列？**

建议在 XAML 中只声明系列与外观，点数据在代码中设置：

```xml
<plot3d:Plot3DView>
  <plot3d:Plot3DView.Series>
    <plot3d:PlotSeries3DCollection>
      <plot3d:LineSeries3D x:Name="SeriesA" Title="曲线 1" StrokeThickness="1.5" />
      <plot3d:ScatterSeries3D x:Name="SeriesB" Title="散点" MarkerSize="4" />
    </plot3d:PlotSeries3DCollection>
  </plot3d:Plot3DView.Series>
</plot3d:Plot3DView>
```

```csharp
SeriesA.SetPoints(pointsA);
SeriesB.SetPoints(pointsB);
```

> `Point3D` 是只读结构体（只有三参数构造函数），不适合在 XAML 中逐点声明；大量点请使用 `SetPoints`。

**Q5：如何导出图片？**

控件内置了 PNG 导出：

```csharp
await plot.SaveImageAsync("chart.png", scale: 2.0);   // 直接保存
await plot.SaveImageWithDialogAsync();                // 弹出保存对话框
```

也可以使用 Avalonia 的 `RenderTargetBitmap` 自行渲染：

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

**Q6：为什么折线出现断裂？**

数据中包含 `NaN` / `Infinity` 时会主动断线，这是有意设计。检查数据源即可。

**Q7：随机颜色每次运行都不一样，如何固定？**

随机颜色由 `Random.Shared` 生成并在系列内缓存。若需要可复现的颜色，请改用 `SeriesColorMode.Fixed` 或自定义调色板。

**Q8：支持触摸屏吗？**

支持。Avalonia 会把单指触摸映射为左键拖动（旋转），双指手势未做特殊处理。

**Q9：可以在非 UI 线程更新数据吗？**

`SetPoints` 会触发 `InvalidateVisual`，必须在 UI 线程调用。后台线程可先构建数组，再通过 `Dispatcher.UIThread.Post` 提交。

**Q10：控件没有显示？**

确认：

1. 父容器给了非零尺寸（控件默认期望 360×280，可设置 `Width` / `Height`）。
2. `Series` 中至少有一个 `IsVisible = true` 且非空的系列，否则只显示默认 `0..1` 空坐标框。
3. 背景与前景色没有设置成相同颜色。

**Q11：如何让悬停提示显示业务信息？**

在添加点时同步写入 Tag（见 [8.1](#81-为数据点附加业务信息tag)），或者设置 `HoverTextFormatter` 回调。
提示内容完全由宿主决定，控件不做反射或序列化。

**Q12：时间轴数值应该传什么？**

传入 `DateTime.ToOADate()` 的结果（单位：天），并把对应轴的 `XAxisValueFormat` / `YAxisValueFormat` / `ZAxisValueFormat`
设为 `Date`、`Time` 或 `DateTime`。需要毫秒时设置 `AxisTimeMilliseconds = true`（步长小于 1 秒时会自动输出毫秒）。

**Q13：开启悬停后大数据量会卡顿吗？**

命中检测会遍历可见点的屏幕投影，数万点仍可实时响应；若数据量达到数十万级，建议在业务层降采样，
或仅在需要时开启 `IsPointHoverEnabled` / `IsCrosshairEnabled`。

---

## 20. 版本与许可

### 1.3.0

- 坐标轴范围改用“屏幕上看到的两端”命名：`XAxisLeft` / `XAxisRight`、`YAxisLeft` / `YAxisRight`、`ZAxisBottom` / `ZAxisTop`；任意一端都可以是大值或小值，反向轴会自动按递减顺序分配刻度。旧的 `XAxisMinimum` 等属性保留为 `Obsolete` 映射。
- 新增拖动旋转方向反转：`IsHorizontalRotationReversed` / `IsVerticalRotationReversed`。
- 新增图例位置（`LegendPosition`，四角 / 四边 / 居中 9 个位置）与图例偏移（`LegendOffset`）。
- 反向轴适配十字准线射线求交、背面面板与等长轴锁定。
- 新增刻度标签自动避让：跨轴角点标签沿轴向外方向轻微外移，同轴过密时自动省略；刻度文本与轴标题改为在数据之后绘制，不再被曲线遮挡。
- 新增图例自动避让（`IsLegendAvoidanceEnabled`，默认开启）：图例会自动避开刻度文本与轴标题，小尺寸下自动压缩行距并限制在控件内；避让采用“粘滞 + 最小位移推出 + 空隙回位”，旋转 / 缩放时不会抖动，示例新增对应开关。
- 旧轴范围属性（`XAxisMinimum` 等）保留为 `StyledProperty`，可通过绑定、样式和动画驱动，数值变化自动映射到新的左 / 右（下 / 上）轴端。
- 示例程序的时间轴模式自动锁定等长轴，避免时间轴（以“天”为单位）与其它轴量级悬殊时塌缩；新增“图例位置”“图例偏移”“左右 / 上下拖动反转”“坐标轴反向示例”选项。

### 1.2.0

- 新增等长轴锁定（`IsAspectRatioLocked`）：三轴数量级悬殊时坐标框仍显示为正方体，刻度数值保持各轴真实范围。
- 新增刻度小数位设置（`XAxisTickDecimals` / `YAxisTickDecimals` / `ZAxisTickDecimals`），支持固定 0..8 位小数或只保留整数。
- 手动设置的最小 / 最大值小于数据范围时，该轴自动延伸并重新取整，图形不再超出坐标框。
- 十字准线与屏幕坐标反算适配等长轴锁定（在归一化空间中做射线求交）。
- 示例程序新增“锁定正方体比例”与“刻度小数位”选项。

### 1.1.2

- 将 README 截图改用 jsDelivr CDN（`cdn.jsdelivr.net`），避免部分网络无法访问 GitHub raw 导致图片不显示（无代码变更）。

### 1.1.1

- 修复 NuGet 包页面 README 的中英文切换链接与截图无法显示的问题：相对路径改为绝对 GitHub 链接（无代码变更）。

### 1.1.0

- 新增数据点交互：悬停高亮与自定义提示（逐点 Tag）、十字准线 X/Y/Z 坐标读数、左键点选与 `PointSelected` / `PointDeselected` 事件。
- 新增右键菜单：默认“保存图像 (PNG)”“删除选中点”，支持 `ContextMenuItems` 与 `ContextMenuBuilding` 自定义。
- 新增 PNG 导出：`SaveImageAsync` / `SaveImageWithDialogAsync`。
- 新增完全透明背景（`IsBackgroundTransparent`）。
- 新增坐标轴日期 / 时间格式（`AxisValueFormat`，支持毫秒）与时间友好刻度。
- 新增刻度 / 轴标题排版参数：`TickLength`、`TickLabelGap`、`TickLabelOffset`、`TickLabelFontSize`、`AxisTitleGap`、`AxisTitleOffset`、`AxisTitleFontSize`。
- 新增最新点高亮样式：`HighlightLatestPoint` / `LatestPointColor` / `LatestPointSize` / `LatestPointShape`。
- `LineSeries3D` / `ScatterSeries3D` 提取公共基类 `PointSeries3D`，新增逐点标签与 `RemoveAt` 等 API。
- 示例程序与文档同步更新，新增英文文档 `README.en.md`。

### 1.0.0

- 首个版本：三维折线、三维散点、轨道相机、正交 / 透视投影、自动刻度、网格、面板、图例、浅色 / 深色主题、7 套调色板、AOT / 裁剪支持。

### 许可证

MIT License。可自由用于商业项目，请保留版权声明。

### 商标声明

MATLAB® is a registered trademark of The MathWorks, Inc.

本文档中提及 MATLAB 仅用于客观描述本控件的视觉与交互风格，不表示任何形式的关联、授权或背书。本控件与 The MathWorks, Inc. 无任何隶属关系，也未获得其认可或赞助。本项目为独立实现，不包含、不调用、不派生自任何 MathWorks 产品代码，亦不提供 MATLAB 语言、数据格式或文件格式的兼容能力。

Matplotlib 与 ColorBrewer 分别为其各自权利人的项目/作品，本项目仅使用其公开的配色数值。

### 目录结构

```
3D控件/
├── Plot3D.Avalonia.sln
├── README.md                   中文文档
├── README.en.md                英文文档
├── docs/                       截图与图标
├── src/Plot3D.Avalonia/        控件库（NuGet 包）
│   ├── Controls/               Plot3DView（属性 / 渲染 / 交互 / 右键菜单）
│   ├── Modeling/               Point3D、Series、标签、调色板、枚举
│   ├── Rendering/              投影、刻度、坐标框、图例、交互叠加层
│   └── Properties/             XmlnsDefinition
└── samples/Plot3D.Avalonia.Demo/   演示程序
```

### 运行演示

```bash
dotnet run --project samples/Plot3D.Avalonia.Demo
```

可选启动参数：

```bash
dotnet run --project samples/Plot3D.Avalonia.Demo -- --dark --perspective --scatter
dotnet run --project samples/Plot3D.Avalonia.Demo -- --time
```
