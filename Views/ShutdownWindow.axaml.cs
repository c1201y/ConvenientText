using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ConvenientText.Views;

public partial class ShutdownWindow : Window
{
    private readonly DispatcherTimer _countdownTimer;
    private readonly DispatcherTimer _autoMinimizeTimer;
    private DateTime _shutdownTime;
    private bool _isShuttingDown;
    private bool _isMinimized;
    private readonly TextBlock _timeLabel;
    private readonly TextBlock _statusLabel;
    private readonly TextBlock _autoMinLabel;
    private Grid? _floatingBtn;

    public ShutdownWindow(int totalSeconds)
    {
        Title = "计时关机";
        Width = 380;
        Height = 200;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;

        var bg = new SolidColorBrush(Color.FromArgb(240, 30, 30, 30));
        var fg = Brushes.White;
        var accentColor = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        var dimFg = new SolidColorBrush(Color.FromRgb(180, 180, 180));

        _statusLabel = new TextBlock
        {
            Text = "关机倒计时",
            Foreground = dimFg,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        _timeLabel = new TextBlock
        {
            Text = "00:00:00",
            FontSize = 36,
            FontWeight = FontWeights.Bold,
            Foreground = fg,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 8),
            FontFamily = new FontFamily("Segoe UI")
        };

        _autoMinLabel = new TextBlock
        {
            Text = "5 秒后自动最小化",
            Foreground = dimFg,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12),
            Opacity = 0.7
        };

        var statusIcon = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M12,2 C6.5,2 2,6.5 2,12 C2,17.5 6.5,22 12,22 C17.5,22 22,17.5 22,12 C22,6.5 17.5,2 12,2 Z M17,13 L11,13 L11,7 L13,7 L13,11.5 L18,11.5 L18,13.5 Z"),
            Fill = accentColor,
            Width = 16, Height = 16,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        statusRow.Children.Add(statusIcon);
        statusRow.Children.Add(_statusLabel);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        buttonRow.Children.Add(MakeButton("立即关机", accentColor, fg, (_, _) => DoShutdown()));
        buttonRow.Children.Add(MakeButton("取消关机", new SolidColorBrush(Color.FromRgb(60, 60, 60)), fg, (_, _) => DoCancel()));
        buttonRow.Children.Add(MakeButton("最小化", new SolidColorBrush(Color.FromRgb(60, 60, 60)), fg, (_, _) => MinimizeToFloating()));

        var mainStack = new StackPanel();
        mainStack.Children.Add(statusRow);
        mainStack.Children.Add(_timeLabel);
        mainStack.Children.Add(_autoMinLabel);
        mainStack.Children.Add(buttonRow);

        Content = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24, 18, 24, 18),
            Child = mainStack
        };

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) => UpdateCountdown();

        _autoMinimizeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _autoMinimizeTimer.Tick += (_, _) =>
        {
            _autoMinimizeTimer.Stop();
            MinimizeToFloating();
        };

        if (totalSeconds > 0)
            StartCountdown(totalSeconds);
    }

    private static Button MakeButton(string content, Brush bg, Brush fg, RoutedEventHandler onClick)
    {
        var normalBg = bg;
        var hoverBg = new SolidColorBrush(
            bg is SolidColorBrush s
                ? Color.FromArgb(255,
                    (byte)Math.Min(255, s.Color.R + 25),
                    (byte)Math.Min(255, s.Color.G + 25),
                    (byte)Math.Min(255, s.Color.B + 25))
                : Colors.Gray);

        var btn = new Button
        {
            Content = content,
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Background = normalBg,
            Foreground = fg,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(4, 0, 4, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = normalBg;
        btn.Click += onClick;
        return btn;
    }

    public void StartCountdown(int totalSeconds)
    {
        RunShutdown(totalSeconds);
        _shutdownTime = DateTime.Now.AddSeconds(totalSeconds);
        _isShuttingDown = true;
        _countdownTimer.Start();
        _autoMinimizeTimer.Start();
        _statusLabel.Text = "关机已设定，倒计时中...";
    }

    private void UpdateCountdown()
    {
        var remaining = _shutdownTime - DateTime.Now;
        if (remaining.TotalSeconds <= 0)
        {
            _countdownTimer.Stop();
            _timeLabel.Text = "00:00:00";
            return;
        }
        var text = remaining.ToString(@"hh\:mm\:ss");
        _timeLabel.Text = text;
        if (_isMinimized && _floatingBtn?.Children[0] is Border card &&
            card.Child is StackPanel row && row.Children.Count > 1 &&
            row.Children[1] is TextBlock t)
            t.Text = text;
    }

    private void DoShutdown()
    {
        var result = MessageBox.Show("确定要立即关机吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            RunShutdown(0);
            _isShuttingDown = false;
            _countdownTimer.Stop();
            _autoMinimizeTimer.Stop();
            Close();
        }
    }

    private void DoCancel()
    {
        if (_isShuttingDown)
        {
            CancelShutdown();
            _isShuttingDown = false;
            _countdownTimer.Stop();
            _autoMinimizeTimer.Stop();
            _timeLabel.Text = "00:00:00";
            _statusLabel.Text = "已取消关机";
            _autoMinLabel.Text = "";
        }
        else
        {
            Close();
        }
    }

    private void MinimizeToFloating()
    {
        if (_isMinimized) return;
        _isMinimized = true;
        Hide();

        var normalBrush = new SolidColorBrush(Color.FromArgb(220, 50, 50, 50));
        var hoverBrush = new SolidColorBrush(Color.FromArgb(220, 80, 80, 80));
        var borderBrush = new SolidColorBrush(Color.FromArgb(100, 0, 120, 212));

        var countdownText = new TextBlock
        {
            Text = _timeLabel.Text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI")
        };

        var icon = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M12,2 C6.5,2 2,6.5 2,12 C2,17.5 6.5,22 12,22 C17.5,22 22,17.5 22,12 C22,6.5 17.5,2 12,2 Z M17,13 L11,13 L11,7 L13,7 L13,11.5 L18,11.5 L18,13.5 Z"),
            Fill = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            Width = 16, Height = 16,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        var cardBg = new Border
        {
            Background = normalBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 6, 12, 6),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(icon);
        row.Children.Add(countdownText);

        cardBg.Child = row;

        _floatingBtn = new Grid
        {
            Width = 120,
            Height = 36,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        _floatingBtn.Children.Add(cardBg);

        _floatingBtn.MouseEnter += (_, _) => cardBg.Background = hoverBrush;
        _floatingBtn.MouseLeave += (_, _) => cardBg.Background = normalBrush;
        _floatingBtn.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
                RestoreFromFloating();
        };

        var host = new Window
        {
            Width = 120,
            Height = 36,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            Content = _floatingBtn,
            Left = SystemParameters.PrimaryScreenWidth - 140,
            Top = SystemParameters.PrimaryScreenHeight - 80
        };

        cardBg.MouseLeftButtonDown += (_, _) =>
        {
            try { host.DragMove(); } catch { }
        };
        cardBg.Cursor = System.Windows.Input.Cursors.SizeAll;

        host.Closing += (_, _) =>
        {
            _isMinimized = false;
            Show();
            Activate();
        };

        host.Show();
    }

    private void RestoreFromFloating()
    {
        _isMinimized = false;
        Show();
        Activate();
        if (_floatingBtn?.Parent is Window host)
            host.Close();
    }

    private static void RunShutdown(int seconds)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = $"/s /t {seconds} /c \"ClassIsland 计时关机\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
    }

    private static void CancelShutdown()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/a",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        _countdownTimer.Stop();
        _autoMinimizeTimer.Stop();
        base.OnClosed(e);
    }
}
