using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Plot3D.Avalonia.Modeling;

namespace Plot3D.Avalonia.Controls;

/// <summary>
/// <see cref="Plot3DView"/> 的右键菜单与图像导出：默认提供“保存图像 (PNG)”，
/// 选中数据点后提供“删除选中点”，并支持追加自定义菜单项。
/// </summary>
public partial class Plot3DView
{
    /// <summary>是否启用右键菜单。</summary>
    public static readonly StyledProperty<bool> IsContextMenuEnabledProperty =
        AvaloniaProperty.Register<Plot3DView, bool>(nameof(IsContextMenuEnabled), true);

    private ContextMenu? _contextMenu;
    private bool _isExporting;

    /// <summary>当前是否正在导出图像；导出期间图例不做回位动画等额外渲染。</summary>
    internal bool IsExporting => _isExporting;

    /// <summary>构建右键菜单时触发，可在内置菜单项基础上增删或替换内容。</summary>
    public event EventHandler<PlotContextMenuEventArgs>? ContextMenuBuilding;

    /// <summary>自定义右键菜单项集合，追加在内置菜单项之后。</summary>
    public AvaloniaList<Control> ContextMenuItems { get; } = new();

    /// <summary>是否启用右键菜单。</summary>
    public bool IsContextMenuEnabled
    {
        get => GetValue(IsContextMenuEnabledProperty);
        set => SetValue(IsContextMenuEnabledProperty, value);
    }

    private void ShowPlotContextMenu()
    {
        if (!IsContextMenuEnabled)
        {
            return;
        }

        var items = new List<Control>();

        if (_selectedPoint is { })
        {
            var deleteItem = new MenuItem { Header = "删除选中点" };
            deleteItem.Click += OnDeleteSelectedPointClick;
            items.Add(deleteItem);
            items.Add(new Separator());
        }

        var saveItem = new MenuItem { Header = "保存图像 (PNG)" };
        saveItem.Click += OnSaveImageClick;
        items.Add(saveItem);

        foreach (var item in ContextMenuItems)
        {
            items.Add(item);
        }

        ContextMenuBuilding?.Invoke(this, new PlotContextMenuEventArgs(items, _selectedPoint));

        var menu = _contextMenu ??= new ContextMenu { Placement = PlacementMode.Pointer };
        menu.Items.Clear();
        foreach (var item in items)
        {
            menu.Items.Add(item);
        }

        menu.Open(this);
    }

    private void OnDeleteSelectedPointClick(object? sender, RoutedEventArgs e) => DeleteSelectedPoint();

    private async void OnSaveImageClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await SaveImageWithDialogAsync();
        }
        catch (Exception ex)
        {
            // async void 事件处理器不能把异常抛回 UI 线程，否则会终止进程。
            Debug.WriteLine($"保存图像失败: {ex}");
        }
    }

    /// <summary>
    /// 把控件当前内容渲染为 PNG 并写入指定文件。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="scale">渲染倍率（1 = 96 DPI，2 = 高清），有效范围 0.5..8。</param>
    /// <returns>保存成功返回 true；控件尺寸无效或写入失败时返回 false。</returns>
    public async Task<bool> SaveImageAsync(string filePath, double scale = 2.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var bitmap = RenderToBitmap(scale);
        if (bitmap is null)
        {
            return false;
        }

        await using var stream = File.Create(filePath);
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return true;
    }

    /// <summary>弹出文件保存对话框并保存 PNG；平台不支持或用户取消时返回 false。</summary>
    public async Task<bool> SaveImageWithDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var storage = topLevel?.StorageProvider;
        if (storage is null || !storage.CanSave)
        {
            return false;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存图像",
            SuggestedFileName = $"plot3d-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            DefaultExtension = "png",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("PNG 图像") { Patterns = ["*.png"] },
            ],
        });

        if (file is null)
        {
            return false;
        }

        using var bitmap = RenderToBitmap(2.0);
        if (bitmap is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return true;
    }

    private RenderTargetBitmap? RenderToBitmap(double scale)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (!(width > 1) || !(height > 1))
        {
            return null;
        }

        var safeScale = double.IsFinite(scale) ? Math.Clamp(scale, 0.5, 8) : 2;
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Round(width * safeScale)),
            Math.Max(1, (int)Math.Round(height * safeScale)));

        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * safeScale, 96 * safeScale));
        try
        {
            // 导出期间跳过交互浮层（十字准线、悬停/选中高亮、提示框），只输出图表本身。
            _isExporting = true;
            bitmap.Render(this);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
        finally
        {
            _isExporting = false;
        }
    }
}
