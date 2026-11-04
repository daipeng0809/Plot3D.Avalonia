using Avalonia.Collections;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// <see cref="PlotSeries3D"/> 的集合。集合变更会自动触发控件刷新。
/// </summary>
public class PlotSeries3DCollection : AvaloniaList<PlotSeries3D>
{
    /// <summary>创建一个空集合。</summary>
    public PlotSeries3DCollection()
    {
    }

    /// <summary>使用指定的系列创建一个集合。</summary>
    public PlotSeries3DCollection(IEnumerable<PlotSeries3D> items)
        : base(items)
    {
    }
}
