using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using ConvenientText.Models;

namespace ConvenientText.Views
{
    public partial class EditTextWindow : Window
    {
        private readonly TextDataModel _dataModel;
        private readonly TextBox _inputBox;
        private readonly ComboBox _colorCombo;
        private readonly Slider _fontSizeSlider;
        private readonly TextBlock _sizeLabel;

        private readonly List<ColorOption> _colorOptions = new()
        {
            new("白色", Colors.White),
            new("红色", Colors.Red),
            new("黄色", Colors.Yellow),
            new("绿色", Colors.Green),
            new("蓝色", Colors.Blue),
            new("粉色", Colors.DeepPink),
            new("浅灰", Colors.LightGray)
        };

        public EditTextWindow(TextDataModel dataModel)
        {
            _dataModel = dataModel;

            Title = "文本编辑";
            Width = 520;
            Height = 160;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = true;
            WindowStyle = WindowStyle.SingleBorderWindow;
            Background = Brushes.Transparent;

            var isDark = IsSystemDarkMode();
            var foregroundBrush = isDark ? Brushes.White : Brushes.Black;
            var inputBackground = isDark ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : Brushes.White;
            var buttonBackground = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            var buttonHoverBackground = new SolidColorBrush(Color.FromRgb(84, 84, 84));

            _inputBox = new TextBox
            {
                Text = _dataModel.DisplayText,
                Margin = new Thickness(0, 0, 10, 0),
                Background = inputBackground,
                Foreground = foregroundBrush,
                BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(80, 80, 80)) : new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };

            _colorCombo = new ComboBox
            {
                Width = 90,
                DisplayMemberPath = "Name",
                ItemsSource = _colorOptions,
                Background = inputBackground,
                Foreground = foregroundBrush,
                BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(80, 80, 80)) : new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            _colorCombo.SelectedItem = _colorOptions.Find(item => item.Color == _dataModel.TextColor) ?? _colorOptions[0];
// 颜色预览方块
var colorPreview = new System.Windows.Shapes.Rectangle
{
    Width = 20,
    Height = 20,
    Margin = new Thickness(6, 0, 0, 0),
    Stroke = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
    StrokeThickness = 1,
    VerticalAlignment = VerticalAlignment.Center
};

// 更新预览颜色
void UpdateColorPreview()
{
    if (_colorCombo.SelectedItem is ColorOption selected)
        colorPreview.Fill = new SolidColorBrush(selected.Color);
}
UpdateColorPreview();

_colorCombo.SelectionChanged += (_, _) => UpdateColorPreview();

            _fontSizeSlider = new Slider
            {
                Minimum = 10,
                Maximum = 48,
                Value = _dataModel.FontSize,
                TickFrequency = 2,
                IsSnapToTickEnabled = true,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center
            };

            _sizeLabel = new TextBlock
            {
                Text = ((int)_dataModel.FontSize).ToString(),
                Width = 35,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = foregroundBrush
            };

            _fontSizeSlider.ValueChanged += (_, _) =>
            {
                _sizeLabel.Text = ((int)_fontSizeSlider.Value).ToString();
                _sizeLabel.Foreground = foregroundBrush;
            };

            var confirmBtn = new Button
            {
                Content = "确定",
                Width = 80,
                Background = buttonBackground,
                Foreground = foregroundBrush,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                Margin = new Thickness(8, 20, 0, 0)
            };
            confirmBtn.MouseEnter += (_, _) => confirmBtn.Background = buttonHoverBackground;
            confirmBtn.MouseLeave += (_, _) => confirmBtn.Background = buttonBackground;
            confirmBtn.Click += OnConfirmClick;

            var cancelBtn = new Button
            {
                Content = "取消",
                Width = 80,
                Background = buttonBackground,
                Foreground = foregroundBrush,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                Margin = new Thickness(8, 20, 0, 0)
            };
            cancelBtn.MouseEnter += (_, _) => cancelBtn.Background = buttonHoverBackground;
            cancelBtn.MouseLeave += (_, _) => cancelBtn.Background = buttonBackground;
            cancelBtn.Click += OnCancelClick;

            var row1 = new DockPanel { LastChildFill = true, Margin = new Thickness(8, 20, 0, 15) };
            var sizePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            sizePanel.Children.Add(new TextBlock { Text = "字号", Foreground = foregroundBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            sizePanel.Children.Add(_fontSizeSlider);
            sizePanel.Children.Add(_sizeLabel);
            DockPanel.SetDock(sizePanel, Dock.Right);
            row1.Children.Add(sizePanel);

            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            colorPanel.Children.Add(new TextBlock { Text = "颜色", Foreground = foregroundBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            colorPanel.Children.Add(_colorCombo);
            colorPanel.Children.Add(colorPreview);
            DockPanel.SetDock(colorPanel, Dock.Right);
            row1.Children.Add(colorPanel);
            
            var contentLabel = new TextBlock
            {
             Text = "内容",
             Foreground = foregroundBrush,
             VerticalAlignment = VerticalAlignment.Center,
             Margin = new Thickness(0, 0, 6, 0)
           };
            row1.Children.Add(contentLabel);
            row1.Children.Add(_inputBox);

            var row2 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row2.Children.Add(cancelBtn);
            row2.Children.Add(confirmBtn);

            var dock = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(row1, Dock.Top);
            dock.Children.Add(row1);
            DockPanel.SetDock(row2, Dock.Bottom);
            dock.Children.Add(row2);
            Content = dock;
            Background = inputBackground;
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                const string personalizeKey = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
                var value = Microsoft.Win32.Registry.GetValue(personalizeKey, "AppsUseLightTheme", 1);
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            _dataModel.DisplayText = _inputBox.Text ?? string.Empty;
            _dataModel.TextColor = (_colorCombo.SelectedItem as ColorOption)?.Color ?? Colors.White;
            _dataModel.FontSize = _fontSizeSlider.Value;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class ColorOption
        {
            public ColorOption(string name, Color color)
            {
                Name = name;
                Color = color;
            }

            public string Name { get; }
            public Color Color { get; }
            public override string ToString() => Name;
        }
    }
}
