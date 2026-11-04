using System.Collections.ObjectModel;
using Avalonia.Media;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 内置调色板。所有调色板都是只读的，可直接赋值给 <see cref="Controls.Plot3DView.Palette"/>。
/// </summary>
/// <remarks>
/// 调色板仅包含颜色数值，不涉及第三方代码。其中 <see cref="Tab10"/> / <see cref="Tab20"/> 为
/// Matplotlib 默认配色，<see cref="Set1"/> 来自 ColorBrewer，<see cref="Classic"/> 为科学计算软件常见的
/// 经典 7 色；各色值均属事实性数据，此处按公开信息整理。
/// </remarks>
public static class PlotPalette
{
    private static readonly Color[] Tab10Colors =
    [
        Color.Parse("#1F77B4"), Color.Parse("#FF7F0E"), Color.Parse("#2CA02C"), Color.Parse("#D62728"),
        Color.Parse("#9467BD"), Color.Parse("#8C564B"), Color.Parse("#E377C2"), Color.Parse("#7F7F7F"),
        Color.Parse("#BCBD22"), Color.Parse("#17BECF"),
    ];

    private static readonly Color[] Tab20Colors =
    [
        Color.Parse("#1F77B4"), Color.Parse("#AEC7E8"), Color.Parse("#FF7F0E"), Color.Parse("#FFBB78"),
        Color.Parse("#2CA02C"), Color.Parse("#98DF8A"), Color.Parse("#D62728"), Color.Parse("#FF9896"),
        Color.Parse("#9467BD"), Color.Parse("#C5B0D5"), Color.Parse("#8C564B"), Color.Parse("#C49C94"),
        Color.Parse("#E377C2"), Color.Parse("#F7B6D2"), Color.Parse("#7F7F7F"), Color.Parse("#C7C7C7"),
        Color.Parse("#BCBD22"), Color.Parse("#DBDB8D"), Color.Parse("#17BECF"), Color.Parse("#9EDAE5"),
    ];

    private static readonly Color[] ClassicColors =
    [
        Color.Parse("#0072BD"), Color.Parse("#D95319"), Color.Parse("#EDB120"), Color.Parse("#7E2F8E"),
        Color.Parse("#77AC30"), Color.Parse("#4DBEEE"), Color.Parse("#A2142F"),
    ];

    private static readonly Color[] Set1Colors =
    [
        Color.Parse("#E41A1C"), Color.Parse("#377EB8"), Color.Parse("#4DAF4A"), Color.Parse("#984EA3"),
        Color.Parse("#FF7F00"), Color.Parse("#FFFF33"), Color.Parse("#A65628"), Color.Parse("#F781BF"),
        Color.Parse("#999999"),
    ];

    private static readonly Color[] PastelColors =
    [
        Color.Parse("#8DD3C7"), Color.Parse("#FDB462"), Color.Parse("#BEBADA"), Color.Parse("#FB8072"),
        Color.Parse("#80B1D3"), Color.Parse("#B3DE69"), Color.Parse("#FCCDE5"), Color.Parse("#BC80BD"),
        Color.Parse("#FFED6F"), Color.Parse("#A6D9F7"),
    ];

    private static readonly Color[] VividColors =
    [
        Color.Parse("#4E79A7"), Color.Parse("#F28E2B"), Color.Parse("#E15759"), Color.Parse("#76B7B2"),
        Color.Parse("#59A14F"), Color.Parse("#EDC948"), Color.Parse("#B07AA1"), Color.Parse("#FF9DA7"),
        Color.Parse("#9C755F"), Color.Parse("#BAB0AC"),
    ];

    private static readonly Color[] DarkColors =
    [
        Color.Parse("#4FC3F7"), Color.Parse("#FFB74D"), Color.Parse("#81C784"), Color.Parse("#E57373"),
        Color.Parse("#BA68C8"), Color.Parse("#4DD0E1"), Color.Parse("#FFD54F"), Color.Parse("#A1887F"),
        Color.Parse("#90A4AE"), Color.Parse("#F06292"),
    ];

    /// <summary>Matplotlib tab10 调色板（默认）。</summary>
    public static IReadOnlyList<Color> Tab10 { get; } = new ReadOnlyCollection<Color>(Tab10Colors);

    /// <summary>Matplotlib tab20 调色板。</summary>
    public static IReadOnlyList<Color> Tab20 { get; } = new ReadOnlyCollection<Color>(Tab20Colors);

    /// <summary>经典 7 色配色（蓝、橙、黄、紫、绿、青、暗红），科学计算软件常见默认配色。</summary>
    public static IReadOnlyList<Color> Classic { get; } = new ReadOnlyCollection<Color>(ClassicColors);

    /// <summary>ColorBrewer Set1 配色。</summary>
    public static IReadOnlyList<Color> Set1 { get; } = new ReadOnlyCollection<Color>(Set1Colors);

    /// <summary>柔和浅色配色，适合浅色背景。</summary>
    public static IReadOnlyList<Color> Pastel { get; } = new ReadOnlyCollection<Color>(PastelColors);

    /// <summary>高对比度配色，适合深色背景。</summary>
    public static IReadOnlyList<Color> Vivid { get; } = new ReadOnlyCollection<Color>(VividColors);

    /// <summary>深色背景专用亮色配色。</summary>
    public static IReadOnlyList<Color> Dark { get; } = new ReadOnlyCollection<Color>(DarkColors);
}
