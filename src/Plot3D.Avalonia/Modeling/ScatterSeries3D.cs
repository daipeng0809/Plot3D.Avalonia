using Avalonia;
using Plot3D.Avalonia.Rendering;

namespace Plot3D.Avalonia.Modeling;

/// <summary>
/// 三维散点系列。所有标记点合并到同一个几何体中一次性填充，可高效绘制数万个点。
/// </summary>
public class ScatterSeries3D : PointSeries3D
{
    static ScatterSeries3D()
    {
        MarkerSizeProperty.OverrideDefaultValue<ScatterSeries3D>(6);
    }

    /// <summary>创建一个空的三维散点系列。</summary>
    public ScatterSeries3D()
    {
    }

    /// <summary>使用指定的点创建一个三维散点系列。</summary>
    public ScatterSeries3D(params Point3D[] points)
    {
        if (points is { Length: > 0 })
        {
            SetPoints(points);
        }
    }

    /// <inheritdoc />
    protected internal override void Render(SeriesRenderContext context)
    {
        MarkerRenderer.Draw(context, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Points), MarkerSize, MarkerShape);
        RenderLatestPoint(context);
    }
}
