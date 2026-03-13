using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RecordTime.Avalonia.ViewModels;

namespace RecordTime.Avalonia.Views;

public partial class AICoachPanel : UserControl
{
    // 按钮拖动状态
    private bool _isButtonDragging;
    private Point _buttonDragStart;
    private double _buttonStartLeft;
    private double _buttonStartTop;
    private Button? _floatingButton;

    // 面板拖动状态
    private bool _isPanelDragging;
    private Point _panelDragStart;
    private double _panelStartLeft;
    private double _panelStartTop;
    private Border? _panelBorder;

    private Canvas? _rootCanvas;

    public AICoachPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _rootCanvas = this.FindControl<Canvas>("RootCanvas");
        _floatingButton = this.FindControl<Button>("FloatingButton");
        _panelBorder = this.FindControl<Border>("AIPanelBorder");

        // 延迟设置初始位置，等待父控件布局完成
        if (_rootCanvas != null)
        {
            _rootCanvas.SizeChanged += OnCanvasSizeChanged;
            SetInitialPositions();
        }
    }

    private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SetInitialPositions();
    }

    private void SetInitialPositions()
    {
        if (_rootCanvas == null) return;

        var canvasWidth = _rootCanvas.Bounds.Width;
        var canvasHeight = _rootCanvas.Bounds.Height;

        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        // 设置悬浮按钮初始位置（右下角）
        if (_floatingButton != null)
        {
            var buttonLeft = Canvas.GetLeft(_floatingButton);
            var buttonTop = Canvas.GetTop(_floatingButton);

            // 只在首次设置或位置无效时设置
            if (double.IsNaN(buttonLeft) || buttonLeft <= 0)
            {
                Canvas.SetLeft(_floatingButton, canvasWidth - 80);
                Canvas.SetTop(_floatingButton, canvasHeight - 80);
            }
        }

        // 设置面板初始位置（右下角）
        if (_panelBorder != null)
        {
            var panelLeft = Canvas.GetLeft(_panelBorder);
            var panelTop = Canvas.GetTop(_panelBorder);

            // 只在首次设置或位置无效时设置
            if (double.IsNaN(panelLeft) || panelLeft <= 0)
            {
                var panelWidth = _panelBorder.Width > 0 ? _panelBorder.Width : 380;
                var panelHeight = 500; // 估计高度
                Canvas.SetLeft(_panelBorder, canvasWidth - panelWidth - 16);
                Canvas.SetTop(_panelBorder, canvasHeight - panelHeight - 16);
            }
        }
    }

    /// <summary>
    /// 处理输入框回车键
    /// </summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is AICoachViewModel vm)
        {
            vm.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }

    #region 悬浮按钮拖动

    private void OnButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_floatingButton == null || _rootCanvas == null) return;

        // 只响应左键
        if (!e.GetCurrentPoint(_rootCanvas).Properties.IsLeftButtonPressed) return;

        _isButtonDragging = true;
        _buttonDragStart = e.GetPosition(_rootCanvas);
        _buttonStartLeft = Canvas.GetLeft(_floatingButton);
        _buttonStartTop = Canvas.GetTop(_floatingButton);

        if (double.IsNaN(_buttonStartLeft)) _buttonStartLeft = 0;
        if (double.IsNaN(_buttonStartTop)) _buttonStartTop = 0;

        e.Pointer.Capture(_floatingButton);
        e.Handled = true;
    }

    private void OnButtonPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isButtonDragging || _floatingButton == null || _rootCanvas == null) return;

        var currentPoint = e.GetPosition(_rootCanvas);
        var deltaX = currentPoint.X - _buttonDragStart.X;
        var deltaY = currentPoint.Y - _buttonDragStart.Y;

        var newLeft = _buttonStartLeft + deltaX;
        var newTop = _buttonStartTop + deltaY;

        // 限制在 Canvas 范围内
        var maxLeft = _rootCanvas.Bounds.Width - _floatingButton.Bounds.Width;
        var maxTop = _rootCanvas.Bounds.Height - _floatingButton.Bounds.Height;

        newLeft = Math.Max(0, Math.Min(newLeft, maxLeft));
        newTop = Math.Max(0, Math.Min(newTop, maxTop));

        Canvas.SetLeft(_floatingButton, newLeft);
        Canvas.SetTop(_floatingButton, newTop);
        e.Handled = true;
    }

    private void OnButtonPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isButtonDragging)
        {
            _isButtonDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    #endregion

    #region 面板拖动

    private void OnPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_panelBorder == null || _rootCanvas == null) return;

        // 只响应左键
        if (!e.GetCurrentPoint(_rootCanvas).Properties.IsLeftButtonPressed) return;

        _isPanelDragging = true;
        _panelDragStart = e.GetPosition(_rootCanvas);
        _panelStartLeft = Canvas.GetLeft(_panelBorder);
        _panelStartTop = Canvas.GetTop(_panelBorder);

        if (double.IsNaN(_panelStartLeft)) _panelStartLeft = 0;
        if (double.IsNaN(_panelStartTop)) _panelStartTop = 0;

        e.Pointer.Capture((IInputElement)sender!);
        e.Handled = true;
    }

    private void OnPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanelDragging || _panelBorder == null || _rootCanvas == null) return;

        var currentPoint = e.GetPosition(_rootCanvas);
        var deltaX = currentPoint.X - _panelDragStart.X;
        var deltaY = currentPoint.Y - _panelDragStart.Y;

        var newLeft = _panelStartLeft + deltaX;
        var newTop = _panelStartTop + deltaY;

        // 限制在 Canvas 范围内
        var panelWidth = _panelBorder.Bounds.Width > 0 ? _panelBorder.Bounds.Width : 380;
        var panelHeight = _panelBorder.Bounds.Height > 0 ? _panelBorder.Bounds.Height : 500;
        var maxLeft = _rootCanvas.Bounds.Width - panelWidth;
        var maxTop = _rootCanvas.Bounds.Height - panelHeight;

        newLeft = Math.Max(0, Math.Min(newLeft, maxLeft));
        newTop = Math.Max(0, Math.Min(newTop, maxTop));

        Canvas.SetLeft(_panelBorder, newLeft);
        Canvas.SetTop(_panelBorder, newTop);
        e.Handled = true;
    }

    private void OnPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanelDragging)
        {
            _isPanelDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    #endregion
}
