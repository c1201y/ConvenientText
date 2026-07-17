using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ConvenientText.Models;

namespace ConvenientText.Components;

[SettingsPageInfo("convenienttext.shutdown", "快捷文本", "\uE767", "\uE767", SettingsPageCategory.External)]
public class ShutdownSettingsControl : SettingsPageBase
{
    private readonly TextBox _hoursBox;
    private readonly TextBox _minutesBox;
    private readonly TextBox _delayBox;
    private readonly TextBox _reminderBox;
    private readonly ShutdownSettings _settings;

    private static Brush TryBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) is Brush b ? b : fallback;

    public ShutdownSettingsControl()
    {
        _settings = Plugin.ShutdownSettings;

        var fg = TryBrush("MaterialDesignBody", Brushes.Black);
        var inputBg = TryBrush("MaterialDesignPaper", new SolidColorBrush(Color.FromRgb(60, 60, 60)));

        Brush cardBg;
        if (TryBrush("MaterialDesignCardBackground", Brushes.Transparent) is SolidColorBrush solid && solid.Color != Colors.Transparent)
            cardBg = solid;
        else if (fg is SolidColorBrush fgColor && fgColor.Color.R > 128)
            cardBg = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
        else
            cardBg = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));

        var plainBoxStyle = new Style(typeof(TextBox));
        plainBoxStyle.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Center));
        plainBoxStyle.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        plainBoxStyle.Setters.Add(new Setter(TextBox.FontSizeProperty, 14.0));
        plainBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, fg));
        plainBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, inputBg));
        plainBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, fg));
        plainBoxStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(1)));
        plainBoxStyle.Seal();

        _hoursBox = new TextBox
        {
            Text = _settings.Hours.ToString(),
            Width = 56, Height = 32,
            MaxLength = 2,
            Style = plainBoxStyle
        };
        _hoursBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(_hoursBox.Text, out int h))
                _settings.Hours = h;
        };

        _minutesBox = new TextBox
        {
            Text = _settings.Minutes.ToString(),
            Width = 56, Height = 32,
            MaxLength = 2,
            Style = plainBoxStyle
        };
        _minutesBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(_minutesBox.Text, out int m))
                _settings.Minutes = m;
        };

        _delayBox = new TextBox
        {
            Text = _settings.Delay.ToString(),
            Width = 56, Height = 32,
            MaxLength = 4,
            Style = plainBoxStyle
        };
        _delayBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(_delayBox.Text, out int d) && d > 0)
                _settings.Delay = d;
        };

        _reminderBox = new TextBox
        {
            Text = _settings.Reminder.ToString(),
            Width = 56, Height = 32,
            MaxLength = 4,
            Style = plainBoxStyle
        };
        _reminderBox.TextChanged += (_, _) =>
        {
            if (int.TryParse(_reminderBox.Text, out int r) && r >= 0)
                _settings.Reminder = r;
        };

        var content = new ScrollViewer
        {
            Content = BuildLayout(fg, cardBg, inputBg)
        };

        Content = content;
    }

    private StackPanel BuildLayout(Brush fg, Brush cardBg, Brush inputBg)
    {
        var stack = new StackPanel
        {
            MaxWidth = 750,
            Margin = new Thickness(16)
        };

        stack.Children.Add(BuildCard(
            "默认关机倒计时",
            "设置触发「计时关机」行动时的默认倒计时时间",
            "\uE787",
            fg, cardBg,
            BuildTimeInput(fg)));

        stack.Children.Add(BuildCard(
            "延迟时长",
            "点击「延迟」按钮时增加的倒计时时间（秒）",
            "\uE787",
            fg, cardBg,
            BuildSingleInput(_delayBox, "秒", fg)));

        stack.Children.Add(BuildCard(
            "再次提醒",
            "关机前再次弹出提醒的剩余时间（秒），0 表示不提醒",
            "\uE787",
            fg, cardBg,
            BuildSingleInput(_reminderBox, "秒", fg)));

        stack.Children.Add(BuildCard(
            "行为选项",
            "调整关机窗口的交互方式",
            "\uE713",
            fg, cardBg,
            BuildOptions(fg)));

        return stack;
    }

    private Border BuildCard(string title, string desc, string iconChar, Brush fg, Brush cardBg, UIElement rightContent)
    {
        var iconPath = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M13,3 L11,3 L11,11 L13,11 Z M6.5,3 L4.5,3 L4.5,11 L6.5,11 Z M13,13 L4.5,13 L4.5,15 L13,15 Z M13,1 L13,3 L4.5,3 L4.5,1 Z"),
            Fill = fg,
            Width = 20, Height = 20,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };

        var headerBlock = new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = fg
        };

        var descBlock = new TextBlock
        {
            Text = desc,
            FontSize = 12,
            Foreground = fg,
            Opacity = 0.55,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(headerBlock);
        textStack.Children.Add(descBlock);

        var cardRow = new Grid();
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(iconPath, 0);
        Grid.SetColumn(textStack, 1);
        Grid.SetColumn(rightContent as FrameworkElement ?? new Grid(), 2);
        cardRow.Children.Add(iconPath);
        cardRow.Children.Add(textStack);
        cardRow.Children.Add(rightContent);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 6),
            Background = cardBg,
            Child = cardRow
        };
    }

    private StackPanel BuildTimeInput(Brush fg)
    {
        var inputStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        inputStack.Children.Add(_hoursBox);
        inputStack.Children.Add(new TextBlock
        {
            Text = " 时  ", VerticalAlignment = VerticalAlignment.Center,
            Foreground = fg, Margin = new Thickness(4, 0, 4, 0)
        });
        inputStack.Children.Add(_minutesBox);
        inputStack.Children.Add(new TextBlock
        {
            Text = " 分", VerticalAlignment = VerticalAlignment.Center,
            Foreground = fg, Margin = new Thickness(4, 0, 0, 0)
        });
        return inputStack;
    }

    private StackPanel BuildSingleInput(TextBox box, string unit, Brush fg)
    {
        var inputStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        inputStack.Children.Add(box);
        inputStack.Children.Add(new TextBlock
        {
            Text = $" {unit}", VerticalAlignment = VerticalAlignment.Center,
            Foreground = fg, Margin = new Thickness(4, 0, 0, 0)
        });
        return inputStack;
    }

    private StackPanel BuildOptions(Brush fg)
    {
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(MakeCheckBox("点击空白处播放提示音", !_settings.NoBeep, fg,
            v => _settings.NoBeep = !v));
        stack.Children.Add(MakeCheckBox("点击空白处抖动动画", !_settings.NoShake, fg,
            v => _settings.NoShake = !v));
        stack.Children.Add(MakeCheckBox("强制关机（不保存未关闭的应用）", _settings.Force, fg,
            v => _settings.Force = v));
        stack.Children.Add(MakeCheckBox("隐藏「取消关机计划」按钮", _settings.HideCancel, fg,
            v => _settings.HideCancel = v));

        return stack;
    }

    private CheckBox MakeCheckBox(string text, bool isChecked, Brush fg, Action<bool> onToggle)
    {
        var cb = new CheckBox
        {
            Content = text,
            IsChecked = isChecked,
            Foreground = fg,
            Margin = new Thickness(0, 4, 0, 4),
            FontSize = 13
        };
        cb.Checked += (_, _) => onToggle(true);
        cb.Unchecked += (_, _) => onToggle(false);
        return cb;
    }
}
