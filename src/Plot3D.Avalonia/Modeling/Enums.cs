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
