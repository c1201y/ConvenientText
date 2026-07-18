using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ConvenientText.Models;
using ConvenientText.Services;

namespace ConvenientText.Components;

[SettingsPageInfo("convenienttext.shutdown", "快捷文本", "\uE8D2", "\uE8D2", SettingsPageCategory.External)]
public class ShutdownSettingsControl : SettingsPageBase
{
    private readonly ShutdownSettings _shutdown;
    private readonly SchoolStatsSettings _stats;
    private readonly DutyRotaSettings _duty;
    private readonly HolidayService _holidayService;
    private TextBlock _statusLabel = null!;
    private TextBlock _holidayListLabel = null!;
    private StackPanel _nameList = null!;
    private TextBlock _previewLabel = null!;

    private static Brush TryBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) is Brush b ? b : fallback;

    public ShutdownSettingsControl()
    {
        _shutdown = Plugin.ShutdownSettings;
        _stats = Plugin.SchoolStatsSettings;
        _duty = Plugin.DutyRotaSettings;
        _holidayService = Plugin.HolidayService;

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
        plainBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(80, 80, 80))));
        plainBoxStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(1)));
        plainBoxStyle.Seal();

        var hoursBox = MakeBox(_shutdown.Hours.ToString(), 56, 2, plainBoxStyle, v => { if (int.TryParse(v, out int h)) _shutdown.Hours = h; });
        var minutesBox = MakeBox(_shutdown.Minutes.ToString(), 56, 2, plainBoxStyle, v => { if (int.TryParse(v, out int m)) _shutdown.Minutes = m; });
        var delayBox = MakeBox(_shutdown.Delay.ToString(), 56, 4, plainBoxStyle, v => { if (int.TryParse(v, out int d) && d > 0) _shutdown.Delay = d; });
        var reminderBox = MakeBox(_shutdown.Reminder.ToString(), 56, 4, plainBoxStyle, v => { if (int.TryParse(v, out int r) && r >= 0) _shutdown.Reminder = r; });

        Content = new ScrollViewer { Content = BuildLayout(fg, cardBg, inputBg, hoursBox, minutesBox, delayBox, reminderBox) };
    }

    private static TextBox MakeBox(string text, int width, int maxLen, Style style, Action<string> onChanged)
    {
        var box = new TextBox { Text = text, Width = width, Height = 32, MaxLength = maxLen, Style = style };
        box.TextChanged += (_, _) => onChanged(box.Text);
        return box;
    }

    private StackPanel BuildLayout(Brush fg, Brush cardBg, Brush inputBg,
        TextBox hoursBox, TextBox minutesBox, TextBox delayBox, TextBox reminderBox)
    {
        var stack = new StackPanel { MaxWidth = 750, Margin = new Thickness(16) };

        // === 计时关机 ===
        stack.Children.Add(BuildSectionHeader("计时关机", "\uE7E8", fg));
        stack.Children.Add(BuildCard("倒计时时间", "触发「计时关机」时的默认倒计时", "\uE767", fg, cardBg,
            BuildTimeInput(hoursBox, minutesBox, fg)));
        stack.Children.Add(BuildCard("延迟时长", "点击「延迟」按钮增加的秒数", "\uE777", fg, cardBg,
            BuildSingleInput(delayBox, "秒", fg)));
        stack.Children.Add(BuildCard("再次提醒", "剩余多少秒时再次弹出提醒，0 为不提醒", "\uE81D", fg, cardBg,
            BuildSingleInput(reminderBox, "秒", fg)));
        stack.Children.Add(BuildCard("行为选项", "调整关机窗口的交互方式", "\uE713", fg, cardBg,
            BuildShutdownOptions(fg)));

        stack.Children.Add(MakeDivider());

        // === 在校日统计 ===
        stack.Children.Add(BuildSectionHeader("在校日统计", "\uE787", fg));
        stack.Children.Add(BuildCard("学期时间", "设置学期的起止日期", "\uE787", fg, cardBg,
            BuildDateInputs(fg, inputBg)));
        stack.Children.Add(BuildCard("显示模式", "切换组件的显示方式", "\uE8A9", fg, cardBg,
            BuildDisplayOptions(fg)));
        stack.Children.Add(BuildHolidayCard(fg, cardBg));

        stack.Children.Add(MakeDivider());

        // === 值日轮换 ===
        stack.Children.Add(BuildSectionHeader("值日轮换", "\uE77B", fg));
        stack.Children.Add(BuildCard("起始日期", "值日轮换的起始日期", "\uE787", fg, cardBg,
            BuildDutyDatePicker(fg, inputBg)));
        stack.Children.Add(BuildCard("轮换间隔", "每隔多少天轮换一次", "\uE777", fg, cardBg,
            BuildIntervalInput(fg, inputBg)));
        stack.Children.Add(BuildCard("偏移调整", "调整本周对应的值日生序号", "\uE8D2", fg, cardBg,
            BuildOffsetInput(fg, inputBg)));
        stack.Children.Add(BuildCard("值日生名单", "按顺序添加，按天轮换", "\uE77B", fg, cardBg,
            BuildNameSection(fg, inputBg)));
        stack.Children.Add(BuildDutyPreviewCard(fg, cardBg));

        return stack;
    }

    // === 通用 ===
    private static Border MakeDivider()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            Margin = new Thickness(0, 8, 0, 12)
        };
    }

    private static StackPanel BuildSectionHeader(string text, string icon, Brush fg)
    {
        var iconBlock = new TextBlock
        {
            Text = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 16, Foreground = fg, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var header = new TextBlock
        {
            Text = text, FontSize = 17, FontWeight = FontWeights.SemiBold,
            Foreground = fg, VerticalAlignment = VerticalAlignment.Center
        };
        var s = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
        s.Children.Add(iconBlock);
        s.Children.Add(header);
        return s;
    }

    private static StackPanel BuildTimeInput(TextBox hoursBox, TextBox minutesBox, Brush fg)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(hoursBox);
        s.Children.Add(new TextBlock { Text = " 时  ", VerticalAlignment = VerticalAlignment.Center, Foreground = fg, Margin = new Thickness(4, 0, 4, 0) });
        s.Children.Add(minutesBox);
        s.Children.Add(new TextBlock { Text = " 分", VerticalAlignment = VerticalAlignment.Center, Foreground = fg, Margin = new Thickness(4, 0, 0, 0) });
        return s;
    }

    private static StackPanel BuildSingleInput(TextBox box, string unit, Brush fg)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(box);
        s.Children.Add(new TextBlock { Text = $" {unit}", VerticalAlignment = VerticalAlignment.Center, Foreground = fg, Margin = new Thickness(4, 0, 0, 0) });
        return s;
    }

    private static CheckBox MakeCheckBox(string text, bool isChecked, Brush fg, Action<bool> onToggle)
    {
        var cb = new CheckBox { Content = text, IsChecked = isChecked, Foreground = fg, Margin = new Thickness(0, 2, 0, 2), FontSize = 13 };
        cb.Checked += (_, _) => onToggle(true);
        cb.Unchecked += (_, _) => onToggle(false);
        return cb;
    }

    private static Button MakeButton(string text, Brush fg)
    {
        var bg = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var hoverBg = new SolidColorBrush(Color.FromRgb(84, 84, 84));
        var btn = new Button
        {
            Content = text, Height = 30, Padding = new Thickness(10, 0, 10, 0),
            Background = bg, Foreground = fg, BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand, Margin = new Thickness(4, 0, 0, 0), FontSize = 12
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = bg;
        return btn;
    }

    private static DatePicker MakeDatePicker(DateTime date, Brush inputBg, Brush fg)
    {
        return new DatePicker
        {
            SelectedDate = date, Background = inputBg, Foreground = fg,
            BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Width = 160, Height = 32, VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static Border BuildCard(string title, string desc, string iconChar, Brush fg, Brush cardBg, UIElement rightContent)
    {
        var iconBlock = new TextBlock
        {
            Text = iconChar, FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18, Foreground = fg, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        var headerBlock = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = fg };
        var descBlock = new TextBlock
        {
            Text = desc, FontSize = 11, Foreground = fg, Opacity = 0.5,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
        };
        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(headerBlock);
        textStack.Children.Add(descBlock);
        var cardRow = new Grid();
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(iconBlock, 0);
        Grid.SetColumn(textStack, 1);
        Grid.SetColumn(rightContent as FrameworkElement ?? new Grid(), 2);
        cardRow.Children.Add(iconBlock);
        cardRow.Children.Add(textStack);
        cardRow.Children.Add(rightContent);
        return new Border
        {
            CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 4), Background = cardBg, Child = cardRow
        };
    }

    // === 计时关机 ===
    private StackPanel BuildShutdownOptions(Brush fg)
    {
        var s = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(MakeCheckBox("点击空白处播放提示音", !_shutdown.NoBeep, fg, v => _shutdown.NoBeep = !v));
        s.Children.Add(MakeCheckBox("点击空白处抖动动画", !_shutdown.NoShake, fg, v => _shutdown.NoShake = !v));
        s.Children.Add(MakeCheckBox("强制关机（不保存未关闭的应用）", _shutdown.Force, fg, v => _shutdown.Force = v));
        s.Children.Add(MakeCheckBox("隐藏「取消关机计划」按钮", _shutdown.HideCancel, fg, v => _shutdown.HideCancel = v));
        return s;
    }

    // === 在校日统计 ===
    private UIElement BuildDateInputs(Brush fg, Brush inputBg)
    {
        var startPicker = MakeDatePicker(_stats.SemesterStart, inputBg, fg);
        startPicker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue) _stats.SemesterStart = dp.SelectedDate.Value;
        };
        var endPicker = MakeDatePicker(_stats.SemesterEnd, inputBg, fg);
        endPicker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue) _stats.SemesterEnd = dp.SelectedDate.Value;
        };
        var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = "从 ", Foreground = fg, VerticalAlignment = VerticalAlignment.Center });
        stack.Children.Add(startPicker);
        stack.Children.Add(new TextBlock { Text = " 到 ", Foreground = fg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
        stack.Children.Add(endPicker);
        return stack;
    }

    private UIElement BuildDisplayOptions(Brush fg)
    {
        var simpleRadio = new RadioButton
        {
            Content = "简洁模式 — 仅显示进度条和校日数", IsChecked = !_stats.IsDetailedMode,
            GroupName = "DisplayMode", Foreground = fg, Margin = new Thickness(0, 4, 0, 4), FontSize = 13
        };
        simpleRadio.Checked += (_, _) => _stats.IsDetailedMode = false;
        var detailRadio = new RadioButton
        {
            Content = "详细模式 — 显示今日星期、校日/非校日", IsChecked = _stats.IsDetailedMode,
            GroupName = "DisplayMode", Foreground = fg, Margin = new Thickness(0, 4, 0, 4), FontSize = 13
        };
        detailRadio.Checked += (_, _) => _stats.IsDetailedMode = true;
        var autoUpdateCb = new CheckBox
        {
            Content = "启动时自动更新节假日数据", IsChecked = _stats.AutoUpdateHolidays,
            Foreground = fg, Margin = new Thickness(0, 8, 0, 0), FontSize = 13
        };
        autoUpdateCb.Checked += (_, _) => _stats.AutoUpdateHolidays = true;
        autoUpdateCb.Unchecked += (_, _) => _stats.AutoUpdateHolidays = false;
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(simpleRadio);
        stack.Children.Add(detailRadio);
        stack.Children.Add(autoUpdateCb);
        return stack;
    }

    private UIElement BuildHolidaySection(Brush fg)
    {
        _statusLabel = new TextBlock
        {
            Text = _holidayService.CurrentData != null && !string.IsNullOrEmpty(_holidayService.CurrentData.LastUpdated)
                ? $"上次更新: {_holidayService.CurrentData.LastUpdated}" : "尚未更新节假日数据",
            FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150))
        };
        var updateBtn = MakeButton("联网更新", fg);
        updateBtn.Click += async (_, _) => await OnUpdateClick();
        _holidayListLabel = new TextBlock
        {
            Text = GetHolidaySummary(), FontSize = 12, Foreground = fg,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
        };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(_statusLabel);
        stack.Children.Add(updateBtn);
        stack.Children.Add(_holidayListLabel);
        return stack;
    }

    private Border BuildHolidayCard(Brush fg, Brush cardBg)
    {
        var iconBlock = new TextBlock
        {
            Text = "\uE81D", FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18, Foreground = fg, Margin = new Thickness(0, 0, 14, 0)
        };
        var headerBlock = new TextBlock { Text = "节假日数据", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = fg };
        var descBlock = new TextBlock
        {
            Text = "管理法定节假日和调休补班", FontSize = 11, Foreground = fg,
            Opacity = 0.5, Margin = new Thickness(0, 2, 0, 0)
        };
        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
        titleStack.Children.Add(iconBlock);
        var textStack = new StackPanel();
        textStack.Children.Add(headerBlock);
        textStack.Children.Add(descBlock);
        titleStack.Children.Add(textStack);
        var cardStack = new StackPanel();
        cardStack.Children.Add(titleStack);
        cardStack.Children.Add(BuildHolidaySection(fg));
        return new Border
        {
            CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 4), Background = cardBg, Child = cardStack
        };
    }

    private async Task OnUpdateClick()
    {
        _statusLabel.Text = "正在更新...";
        var success = await _holidayService.UpdateFromApiAsync();
        _statusLabel.Text = success
            ? $"更新成功! {_holidayService.CurrentData?.LastUpdated}"
            : $"更新失败: {_holidayService.LastError}";
        _holidayListLabel.Text = GetHolidaySummary();
    }

    private string GetHolidaySummary()
    {
        var data = _holidayService.CurrentData;
        if (data == null || data.Holidays.Count == 0) return "暂无节假日数据";
        var holidays = data.Holidays.FindAll(h => h.Type == "holiday");
        var makeups = data.Holidays.FindAll(h => h.Type == "makeup");
        var summary = $"共 {holidays.Count} 个假期, {makeups.Count} 个补班日";
        if (makeups.Count > 0)
        {
            summary += "\n补班: ";
            foreach (var m in makeups) summary += $"{m.Date}({m.Name}) ";
        }
        return summary;
    }

    // === 值日轮换 ===
    private UIElement BuildDutyDatePicker(Brush fg, Brush inputBg)
    {
        var picker = MakeDatePicker(_duty.StartDate, inputBg, fg);
        picker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue) _duty.StartDate = dp.SelectedDate.Value;
            RefreshPreview();
        };
        return picker;
    }

    private UIElement BuildIntervalInput(Brush fg, Brush inputBg)
    {
        var box = new TextBox
        {
            Text = _duty.Interval.ToString(), Width = 56, Height = 32, MaxLength = 2,
            FontSize = 14, Foreground = fg, Background = inputBg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1), TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        box.TextChanged += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v) && v > 0) _duty.Interval = v;
            RefreshPreview();
        };
        var s = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(box);
        s.Children.Add(new TextBlock { Text = " 天", Foreground = fg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
        return s;
    }

    private UIElement BuildOffsetInput(Brush fg, Brush inputBg)
    {
        var box = new TextBox
        {
            Text = _duty.Offset.ToString(), Width = 56, Height = 32, MaxLength = 4,
            FontSize = 14, Foreground = fg, Background = inputBg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1), TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        box.TextChanged += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v)) _duty.Offset = v;
            RefreshPreview();
        };
        var s = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        s.Children.Add(box);
        s.Children.Add(new TextBlock { Text = " 人", Foreground = fg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
        return s;
    }

    private UIElement BuildNameSection(Brush fg, Brush inputBg)
    {
        _nameList = new StackPanel();
        var addBox = new TextBox
        {
            Width = 120, Height = 30, Background = inputBg, Foreground = fg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalContentAlignment = VerticalAlignment.Center, FontSize = 13,
            Margin = new Thickness(0, 0, 6, 0)
        };
        var addBtn = MakeSmallButton("添加", fg);
        addBtn.Click += (_, _) =>
        {
            var name = addBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;
            _duty.Names.Add(name);
            addBox.Text = "";
            RefreshNameList();
            RefreshPreview();
        };
        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        addRow.Children.Add(addBox);
        addRow.Children.Add(addBtn);
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(addRow);
        stack.Children.Add(_nameList);
        RefreshNameList();
        return stack;
    }

    private void RefreshNameList()
    {
        if (_nameList == null) return;
        _nameList.Children.Clear();
        for (int i = 0; i < _duty.Names.Count; i++)
        {
            var name = _duty.Names[i];
            var idx = i;
            var fg = TryBrush("MaterialDesignBody", Brushes.Black);
            var label = new TextBlock
            {
                Text = $"{i + 1}. {name}", FontSize = 13, Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 8, 2), MinWidth = 80
            };
            var delBtn = MakeSmallButton("删", fg);
            delBtn.Click += (_, _) =>
            {
                _duty.Names.RemoveAt(idx);
                RefreshNameList();
                RefreshPreview();
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(label);
            row.Children.Add(delBtn);
            _nameList.Children.Add(row);
        }
    }

    private static Button MakeSmallButton(string text, Brush fg)
    {
        var bg = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var hoverBg = new SolidColorBrush(Color.FromRgb(84, 84, 84));
        var btn = new Button
        {
            Content = text, MinWidth = 28, Height = 28, Background = bg, Foreground = fg,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 12, Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(0)
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = bg;
        return btn;
    }

    private Border BuildDutyPreviewCard(Brush fg, Brush cardBg)
    {
        var iconBlock = new TextBlock
        {
            Text = "\uE81D", FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18, Foreground = fg, Margin = new Thickness(0, 0, 14, 0)
        };
        var headerBlock = new TextBlock { Text = "值日预览", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = fg };
        var descBlock = new TextBlock
        {
            Text = "查看未来几天的值日安排", FontSize = 11, Foreground = fg,
            Opacity = 0.5, Margin = new Thickness(0, 2, 0, 0)
        };
        var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
        titleStack.Children.Add(iconBlock);
        var textStack = new StackPanel();
        textStack.Children.Add(headerBlock);
        textStack.Children.Add(descBlock);
        titleStack.Children.Add(textStack);
        _previewLabel = new TextBlock { FontSize = 12, Foreground = fg, TextWrapping = TextWrapping.Wrap };
        RefreshPreview();
        var cardStack = new StackPanel();
        cardStack.Children.Add(titleStack);
        cardStack.Children.Add(_previewLabel);
        return new Border
        {
            CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 4), Background = cardBg, Child = cardStack
        };
    }

    private void RefreshPreview()
    {
        if (_previewLabel == null) return;
        if (_duty.Names.Count == 0)
        {
            _previewLabel.Text = "请先添加值日生";
            return;
        }
        var today = DateTime.Today;
        var lines = "";
        var count = Math.Min(7, _duty.Names.Count * _duty.Interval + 1);
        for (int i = 0; i < count; i++)
        {
            var date = today.AddDays(i);
            var duty = _duty.GetDutyForDate(date);
            var dayStr = i == 0 ? "今天" : i == 1 ? "明天" : $"{date:MM/dd}";
            lines += $"{dayStr}: {duty}\n";
        }
        _previewLabel.Text = lines.TrimEnd('\n');
    }
}
