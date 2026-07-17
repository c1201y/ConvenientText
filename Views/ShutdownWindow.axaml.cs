using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ConvenientText.Models;

namespace ConvenientText.Views;

public partial class ShutdownWindow : Window
{
    private readonly DispatcherTimer _countdownTimer = null!;
    private readonly ShutdownSettings _settings = null!;
    private DateTime _shutdownTime;
    private bool _isShuttingDown;
    private bool _isHidden;
    private int _remaining;
    private TextBlock _titleLabel = null!;
    private TextBlock _subtitleLabel = null!;
    private Border _dialogBorder = null!;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private static bool _isRunning;

    public ShutdownWindow(ShutdownSettings settings)
    {
        if (_isRunning) return;
        _isRunning = true;

        _settings = settings;
        _remaining = settings.TotalSeconds;
        if (_remaining <= 0) _remaining = 1800;

        SetupWindow();
        SetupUI();
        SetupTray();

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) => UpdateCountdown();

        StartCountdown();
    }

    private static string FormatTime(int seconds)
    {
        if (seconds >= 60)
        {
            int minutes = seconds / 60;
            int remSeconds = seconds % 60;
            if (remSeconds == 0)
                return $"{minutes} 分钟";
            return $"{minutes} 分 {remSeconds} 秒";
        }
        return $"{seconds} 秒";
    }

    private void SetupWindow()
    {
        Title = "计时关机";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void SetupUI()
    {
        var bg = new SolidColorBrush(Color.FromArgb(240, 30, 30, 30));
        var fg = Brushes.White;
        var accentColor = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        var dimFg = new SolidColorBrush(Color.FromRgb(180, 180, 180));
        var btnBg = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var btnHoverBg = new SolidColorBrush(Color.FromRgb(84, 84, 84));

        _titleLabel = new TextBlock
        {
            Text = "即将关机",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = fg,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _subtitleLabel = new TextBlock
        {
            Text = GetSubtitleText(),
            FontSize = 14,
            Foreground = fg,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var delayText = FormatTime(_settings.Delay);

        var acknowledgeBtn = CreateButton("已阅", FluentIconKind.CheckMark, accentColor, fg, true);
        acknowledgeBtn.Click += (_, _) => OnAcknowledgeClick();

        var shutdownBtn = CreateButton("立即关机", FluentIconKind.Power, btnBg, fg, false);
        shutdownBtn.Click += (_, _) => OnShutdownNowClick();

        var delayBtn = CreateButton($"延迟{delayText}", FluentIconKind.Clock, btnBg, fg, false);
        delayBtn.Click += (_, _) => OnDelayClick();

        var cancelBtn = CreateButton("取消关机计划", FluentIconKind.Cancel, btnBg, fg, false);
        cancelBtn.Click += (_, _) => OnCancelClick();

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttonPanel.Children.Add(acknowledgeBtn);
        buttonPanel.Children.Add(MakeSpacer(12));
        buttonPanel.Children.Add(shutdownBtn);
        buttonPanel.Children.Add(MakeSpacer(8));
        buttonPanel.Children.Add(delayBtn);
        if (!_settings.HideCancel)
        {
            buttonPanel.Children.Add(MakeSpacer(8));
            buttonPanel.Children.Add(cancelBtn);
        }

        var dialogContent = new StackPanel
        {
            Margin = new Thickness(24, 20, 24, 20)
        };
        dialogContent.Children.Add(_titleLabel);
        dialogContent.Children.Add(_subtitleLabel);
        dialogContent.Children.Add(buttonPanel);

        _dialogBorder = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(12),
            Child = dialogContent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 580,
            MaxWidth = SystemParameters.WorkArea.Width * 0.8
        };

        var overlay = new Grid { Background = Brushes.Transparent };
        overlay.Children.Add(_dialogBorder);
        Content = overlay;
    }

    private static StackPanel MakeSpacer(double width)
    {
        return new StackPanel { Width = width };
    }

    private Button CreateButton(string text, FluentIconKind iconKind, Brush bg, Brush fg, bool isPrimary)
    {
        var normalBg = bg;
        var hoverBg = new SolidColorBrush(
            bg is SolidColorBrush s
                ? Color.FromArgb(255,
                    (byte)Math.Min(255, s.Color.R + 25),
                    (byte)Math.Min(255, s.Color.G + 25),
                    (byte)Math.Min(255, s.Color.B + 25))
                : Colors.Gray);

        var iconChar = iconKind switch
        {
            FluentIconKind.CheckMark => "\uE73E",
            FluentIconKind.Power => "\uE7E8",
            FluentIconKind.Clock => "\uE787",
            FluentIconKind.Cancel => "\uE711",
            _ => ""
        };

        var iconBlock = new TextBlock
        {
            Text = iconChar,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center
        };

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        contentPanel.Children.Add(iconBlock);
        contentPanel.Children.Add(textBlock);

        var btn = new Button
        {
            Content = contentPanel,
            Height = 32,
            Padding = new Thickness(14, 0, 14, 0),
            Background = normalBg,
            Foreground = fg,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = normalBg;
        return btn;
    }

    private string GetSubtitleText()
    {
        var timeText = FormatTime(_remaining);
        return $"计算机将在{timeText}后自动关闭。请及时保存您的工作或选择其他操作。";
    }

    private void StartCountdown()
    {
        RunShutdown(_remaining);
        _shutdownTime = DateTime.Now.AddSeconds(_remaining);
        _isShuttingDown = true;
        _countdownTimer.Start();
    }

    private void UpdateCountdown()
    {
        _remaining--;
        if (_remaining <= 0)
        {
            _countdownTimer.Stop();
            _remaining = 0;
            PerformShutdown();
            return;
        }

        if (_remaining == _settings.Reminder && _isHidden)
        {
            ShowReminder();
        }

        UpdateUI();
        UpdateTray();
    }

    private void UpdateUI()
    {
        var text = GetSubtitleText();
        _subtitleLabel.Text = text;
    }

    private void UpdateTray()
    {
        if (_trayIcon == null) return;
        var timeText = FormatTime(_remaining);
        _trayIcon.Text = $"计时关机：{timeText}后关闭";
        if (_trayIcon.ContextMenuStrip?.Items.Count > 0)
            _trayIcon.ContextMenuStrip.Items[0].Text = $"剩余时间：{timeText}";
    }

    private void OnAcknowledgeClick()
    {
        _isHidden = true;
        Hide();
    }

    private void OnShutdownNowClick()
    {
        var result = MessageBox.Show("确定要立即关机吗？", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            PerformShutdown();
        }
    }

    private void OnDelayClick()
    {
        _remaining += _settings.Delay;
        _shutdownTime = DateTime.Now.AddSeconds(_remaining);
        if (!_isShuttingDown)
        {
            _isShuttingDown = true;
            _countdownTimer.Start();
        }
        UpdateUI();
        UpdateTray();
    }

    private void OnCancelClick()
    {
        CancelShutdownCommand();
        _isShuttingDown = false;
        _countdownTimer.Stop();
        _remaining = 0;
        Close();
    }

    private void ShowReminder()
    {
        _isHidden = false;
        Show();
        WindowState = WindowState.Normal;
        Topmost = true;
        Activate();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (_dialogBorder != null && !_dialogBorder.IsMouseOver)
        {
            if (!_settings.NoBeep)
            {
                try { System.Media.SystemSounds.Beep.Play(); } catch { }
            }

            if (!_settings.NoShake)
                ShakeDialog();
        }
        base.OnMouseLeftButtonDown(e);
    }

    private void ShakeDialog()
    {
        if (_dialogBorder == null) return;

        var transform = _dialogBorder.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            _dialogBorder.RenderTransform = transform;
        }

        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(500)
        };

        double[] offsets = { -10, 10, -8, 8, -6, 6, -4, 4, -2, 0 };
        double[] times = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };

        for (int i = 0; i < offsets.Length; i++)
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(offsets[i], KeyTime.FromPercent(times[i])));

        transform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void SetupTray()
    {
        try
        {
            var iconPath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "icon.png");

            System.Drawing.Icon trayIcon;
            if (File.Exists(iconPath))
            {
                using var bitmap = new System.Drawing.Bitmap(iconPath);
                trayIcon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
            else
            {
                trayIcon = System.Drawing.SystemIcons.Warning;
            }

            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = trayIcon,
                Visible = true,
                Text = $"计时关机：{FormatTime(_remaining)}后关闭"
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            var timeText = FormatTime(_remaining);
            menu.Items.Add($"剩余时间：{timeText}", null, (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isHidden = false;
                    Show();
                    WindowState = WindowState.Normal;
                    Topmost = true;
                    Activate();
                }));
            });
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add($"延迟{FormatTime(_settings.Delay)}", null, (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(OnDelayClick));
            });
            if (!_settings.HideCancel)
            {
                menu.Items.Add("取消关机计划", null, (_, _) =>
                {
                    Dispatcher.BeginInvoke(new Action(OnCancelClick));
                });
            }
            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.Click += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isHidden = false;
                    Show();
                    WindowState = WindowState.Normal;
                    Topmost = true;
                    Activate();
                }));
            };
        }
        catch { }
    }

    private void RemoveTray()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            e.Handled = true;
            OnCancelClick();
        }
        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _countdownTimer.Stop();
        RemoveTray();
        _isRunning = false;
        base.OnClosed(e);
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

    private void PerformShutdown()
    {
        _countdownTimer.Stop();
        try
        {
            var args = _settings.Force ? "/s /f /t 0" : "/s /t 0";
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
        RemoveTray();
        _isRunning = false;
        Close();
    }

    private static void CancelShutdownCommand()
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

    private enum FluentIconKind
    {
        CheckMark,
        Power,
        Clock,
        Cancel
    }
}
