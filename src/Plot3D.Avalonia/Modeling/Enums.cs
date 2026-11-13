namespace Plot3D.Avalonia.Modeling;

/// <summary>系列的取色方式。</summary>
public enum SeriesColorMode
{
    /// <summary>
    /// 自动取色：若显式设置了 <see cref="PlotSeries3D.Color"/> 则使用该颜色，
    /// 否则依次从 <see cref="Controls.Plot3DView.Palette"/> 中取色。
    /// </summary>
    Auto = 0,

    /// <summary>始终使用 <see cref="PlotSeries3D.Color"/> 指定的颜色。</summary>
    Fixed = 1,

    /// <summary>随机颜色（每条系列只在首次绘制时生成一次，之后保持不变）。</summary>
    Random = 2,
}

/// <summary>线条样式。</summary>
public enum LineStyle
{
    /// <summary>实线。</summary>
    Solid = 0,

    /// <summary>虚线。</summary>
    Dash = 1,

    /// <summary>点线。</summary>
    Dot = 2,

    /// <summary>点划线。</summary>
    DashDot = 3,
}

/// <summary>散点标记形状。</summary>
public enum MarkerShape
{
    /// <summary>圆形（多边形逼近）。</summary>
    Circle = 0,

    /// <summary>正方形。</summary>
    Square = 1,

    /// <summary>菱形。</summary>
    Diamond = 2,

    /// <summary>三角形。</summary>
    Triangle = 3,
}

/// <summary>坐标投影方式。</summary>
public enum ProjectionMode
{
    /// <summary>正交投影（默认），无近大远小效果。</summary>
    Orthographic = 0,

    /// <summary>透视投影，具有近大远小效果。</summary>
    Perspective = 1,
}

/// <summary>控件的浅色 / 深色主题。</summary>
public enum PlotTheme
{
    /// <summary>浅色主题（白色背景、深灰文字）。</summary>
    Light = 0,

    /// <summary>深色主题。</summary>
    Dark = 1,
}

/// <summary>
/// 图例在控件中的位置，按“行优先”排列：顶部三个位置 → 中部三个位置 → 底部三个位置。
/// </summary>
public enum LegendPosition
{
    /// <summary>左上角（默认）。</summary>
    TopLeft = 0,

    /// <summary>顶部居中。</summary>
    TopCenter = 1,

    /// <summary>右上角。</summary>
    TopRight = 2,

    /// <summary>左侧居中。</summary>
    MiddleLeft = 3,

    /// <summary>正中间。</summary>
    Center = 4,

    /// <summary>右侧居中。</summary>
    MiddleRight = 5,

    /// <summary>左下角。</summary>
    BottomLeft = 6,

    /// <summary>底部居中。</summary>
    BottomCenter = 7,

    /// <summary>右下角。</summary>
    BottomRight = 8,
}

/// <summary>
/// 坐标轴数值的标签格式。时间类格式将轴数值按 OLE Automation 日期
/// （<see cref="DateTime.ToOADate"/>，单位为“天”）转换为日期 / 时间文本。
/// </summary>
public enum AxisValueFormat
{
    /// <summary>普通数值（默认）。</summary>
    Numeric = 0,

    /// <summary>日期，如 <c>2026-10-11</c>。</summary>
    Date = 1,

    /// <summary>时间，如 <c>13:22:05</c>；开启毫秒后为 <c>13:22:05.123</c>。</summary>
    Time = 2,

    /// <summary>日期 + 时间，如 <c>2026-10-11 13:22:05</c>。</summary>
    DateTime = 3,
}
