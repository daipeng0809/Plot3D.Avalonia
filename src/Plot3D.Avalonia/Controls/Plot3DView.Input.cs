using Avalonia;
using Avalonia.Input;

namespace Plot3D.Avalonia.Controls;

/// <summary>
/// <see cref="Plot3DView"/> 的鼠标 / 触摸交互：
/// 左键拖动旋转、右键或中键拖动平移、滚轮以光标为中心缩放、双击重置视角。
/// </summary>
public partial class Plot3DView
{
    private enum DragMode
    {
        None,
        Rotate,
        Pan,
    }

    private DragMode _dragMode;
    private Point _lastPointerPosition;

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        var properties = point.Properties;
        DragMode mode;

        if (properties.IsLeftButtonPressed && IsRotationEnabled)
        {
            mode = DragMode.Rotate;
        }
        else if ((properties.IsRightButtonPressed || properties.IsMiddleButtonPressed) && IsPanEnabled)
        {
            mode = DragMode.Pan;
        }
        else
        {
            return;
        }

        _dragMode = mode;
        _lastPointerPosition = point.Position;
        e.Pointer.Capture(this);
        Cursor = new Cursor(mode == DragMode.Rotate ? StandardCursorType.SizeAll : StandardCursorType.Hand);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_dragMode == DragMode.None)
        {
            return;
        }

        var position = e.GetCurrentPoint(this).Position;
        var deltaX = position.X - _lastPointerPosition.X;
        var deltaY = position.Y - _lastPointerPosition.Y;
        _lastPointerPosition = position;

        if (Math.Abs(deltaX) < 1e-9 && Math.Abs(deltaY) < 1e-9)
        {
            return;
        }

        if (_dragMode == DragMode.Rotate)
        {
            var sensitivity = double.IsFinite(RotationSensitivity) ? Math.Clamp(RotationSensitivity, 0.01, 5) : 0.5;
            var azimuth = double.IsFinite(Azimuth) ? Azimuth : DefaultAzimuth;
            var elevation = double.IsFinite(Elevation) ? Elevation : DefaultElevation;

            SetCurrentValue(AzimuthProperty, azimuth + deltaX * sensitivity);
            SetCurrentValue(ElevationProperty, Math.Clamp(elevation + deltaY * sensitivity, -89.9, 89.9));
        }
        else
        {
            _panX += deltaX;
            _panY += deltaY;
            InvalidateScene();
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_dragMode == DragMode.None)
        {
            return;
        }

        e.Pointer.Capture(null);
        EndDrag();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDrag();
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (!IsZoomEnabled || _dragMode != DragMode.None)
        {
            return;
        }

        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 1e-9 || !double.IsFinite(delta))
        {
            return;
        }

        var sensitivity = double.IsFinite(ZoomSensitivity) ? Math.Clamp(ZoomSensitivity, 0.01, 1) : 0.15;
        var current = double.IsFinite(Zoom) ? Math.Clamp(Zoom, 0.02, 100) : 1;
        var target = Math.Clamp(current * Math.Pow(1 + sensitivity, delta), 0.02, 100);
        if (Math.Abs(target - current) < 1e-9)
        {
            e.Handled = true;
            return;
        }

        // 以光标位置为中心缩放：调整平移量，使光标下方的世界点保持不动。
        var mouse = e.GetPosition(this);
        var ratio = target / current;
        var padding = PlotPadding;
        var centerX = padding.Left + Math.Max(8, Bounds.Width - padding.Left - padding.Right) * 0.5;
        var centerY = padding.Top + Math.Max(8, Bounds.Height - padding.Top - padding.Bottom) * 0.5;

        _panX = (mouse.X - centerX) * (1 - ratio) + ratio * _panX;
        _panY = (mouse.Y - centerY) * (1 - ratio) + ratio * _panY;

        SetCurrentValue(ZoomProperty, target);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnDoubleTapped(TappedEventArgs e)
    {
        base.OnDoubleTapped(e);

        if (!ResetOnDoubleClick)
        {
            return;
        }

        ResetView();
        e.Handled = true;
    }

    private void EndDrag()
    {
        if (_dragMode == DragMode.None)
        {
            return;
        }

        _dragMode = DragMode.None;
        Cursor = Cursor.Default;
    }

    /// <summary>重置视角到默认方位角与仰角（保留缩放与平移）。</summary>
    public void ResetCamera()
    {
        SetCurrentValue(AzimuthProperty, DefaultAzimuth);
        SetCurrentValue(ElevationProperty, DefaultElevation);
        InvalidateScene();
    }

    /// <summary>重置缩放与平移。</summary>
    public void ResetZoom()
    {
        _panX = 0;
        _panY = 0;
        SetCurrentValue(ZoomProperty, 1.0);
        InvalidateScene();
    }

    /// <summary>重置所有视角参数：方位角、仰角、缩放与平移。</summary>
    public void ResetView()
    {
        _panX = 0;
        _panY = 0;
        SetCurrentValue(AzimuthProperty, DefaultAzimuth);
        SetCurrentValue(ElevationProperty, DefaultElevation);
        SetCurrentValue(ZoomProperty, 1.0);
        InvalidateScene();
    }
}
