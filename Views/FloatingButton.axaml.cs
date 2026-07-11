using System.IO;
using ConvenientText.Models;
using ConvenientText.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ConvenientText.Views;

public partial class FloatingButton : Window
{
    private readonly TextDataModel _dataModel;
    private readonly DataStorageService _storage;
    private bool _isDragging;
    private Point _dragStart;

    public FloatingButton(TextDataModel dataModel, DataStorageService storage)
    {
        _dataModel = dataModel;
        _storage = storage;

        // 窗口基础设置
        Width = 54;
        Height = 54;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = false;

        // 根容器：Grid 负责圆形裁剪和鼠标事件
        var rootGrid = new Grid
        {
            Width = 54,
            Height = 54,
            Clip = new EllipseGeometry(new Point(27, 27), 27, 27) // 关键：圆形裁剪
        };

        // 圆形背景（带悬停效果）
        var normalBrush = new SolidColorBrush(Color.FromArgb(220, 68, 68, 68));
        var hoverBrush = new SolidColorBrush(Color.FromArgb(220, 102, 102, 102));

        var backgroundEllipse = new Ellipse
        {
            Fill = normalBrush,
            Width = 54,
            Height = 54
        };

        // 图标文字
        var iconText = new TextBlock
        {
            Text = "✎",
            FontSize = 24,
            FontFamily = new FontFamily("Segoe UI Symbol"),
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false // 让鼠标事件穿透到 Grid
        };

        rootGrid.Children.Add(backgroundEllipse);
        rootGrid.Children.Add(iconText);

        // ---- 鼠标事件（拖动 + 单击）----
        rootGrid.PreviewMouseLeftButtonDown += (sender, e) =>
        {
            _isDragging = false;
            _dragStart = e.GetPosition(this);
        };

        rootGrid.PreviewMouseMove += (sender, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                var current = e.GetPosition(this);
                if ((current - _dragStart).Length > 6)
                {
                    _isDragging = true;
                    try { DragMove(); } catch { /* 忽略异常 */ }
                }
            }
        };

        rootGrid.PreviewMouseLeftButtonUp += (sender, e) =>
        {
            if (!_isDragging) // 未拖动 => 单击
            {
                try
                {
                    var editWindow = new EditTextWindow(_dataModel) { Topmost = true };
                    editWindow.Show();
                    _storage.Save(_dataModel);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开编辑窗口失败：{ex.Message}", "ConvenientText",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            _isDragging = false;
        };

        // 悬停效果
        rootGrid.MouseEnter += (_, _) => backgroundEllipse.Fill = hoverBrush;
        rootGrid.MouseLeave += (_, _) => backgroundEllipse.Fill = normalBrush;  
        Content = rootGrid;
        LoadWindowPosition();
        Closing += (_, _) => SaveWindowPosition();
    }
    private void LoadWindowPosition()
{
    string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConvenientText", "pos.json");
    if (System.IO.File.Exists(file))
    {
        try
        {
            string json = System.IO.File.ReadAllText(file);
            var pos = System.Text.Json.JsonSerializer.Deserialize<PositionData>(json);
            if (pos != null)
            {
                double screenW = SystemParameters.WorkArea.Width;
                double screenH = SystemParameters.WorkArea.Height;
                Left = Math.Max(0, Math.Min(pos.Left, screenW - Width));
                Top = Math.Max(0, Math.Min(pos.Top, screenH - Height));
                return;
            }
        }
        catch { }
    }
    Left = SystemParameters.WorkArea.Width - Width - 20;
    Top = SystemParameters.WorkArea.Height - Height - 20;
}

private void SaveWindowPosition()
{
    try
    {
        string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ConvenientText");
        System.IO.Directory.CreateDirectory(dir);
        string file = System.IO.Path.Combine(dir, "pos.json");
        var data = new PositionData { Left = this.Left, Top = this.Top };
        string json = System.Text.Json.JsonSerializer.Serialize(data);
        System.IO.File.WriteAllText(file, json);
    }
    catch { }
}

private class PositionData
{
    public double Left { get; set; }
    public double Top { get; set; }
}
}