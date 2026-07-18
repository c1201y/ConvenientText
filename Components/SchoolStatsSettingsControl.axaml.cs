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

[SettingsPageInfo("convenienttext.schoolstats", "在校日统计", "\uE787", "\uE787", SettingsPageCategory.External)]
public class SchoolStatsSettingsControl : SettingsPageBase
{
    private readonly SchoolStatsSettings _settings;
    private readonly HolidayService _holidayService;
    private TextBlock _statusLabel = null!;
    private TextBox _addDateBox = null!;
    private TextBox _addNameBox = null!;
    private ComboBox _addTypeCombo = null!;
    private TextBlock _holidayListLabel = null!;

    private static Brush TryBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) is Brush b ? b : fallback;

    public SchoolStatsSettingsControl()
    {
        _settings = Plugin.SchoolStatsSettings;
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

        BuildLayout(fg, cardBg, inputBg);
    }

    private void BuildLayout(Brush fg, Brush cardBg, Brush inputBg)
    {
        var stack = new StackPanel { MaxWidth = 750, Margin = new Thickness(16) };

        stack.Children.Add(BuildCard("学期时间", "设置学期的起止日期", "\uE787", fg, cardBg,
            BuildDateInputs(fg, inputBg)));

        stack.Children.Add(BuildCard("显示设置", "调整组件的显示方式", "\uE787", fg, cardBg,
            BuildDisplayOptions(fg)));

        stack.Children.Add(BuildCard("节假日数据", "管理中国法定节假日和调休数据", "\uE787", fg, cardBg,
            BuildHolidaySection(fg, inputBg)));

        Content = new ScrollViewer { Content = stack };
    }

    private UIElement BuildDateInputs(Brush fg, Brush inputBg)
    {
        var startPicker = MakeDatePicker(_settings.SemesterStart, inputBg, fg);
        startPicker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue)
                _settings.SemesterStart = dp.SelectedDate.Value;
        };

        var endPicker = MakeDatePicker(_settings.SemesterEnd, inputBg, fg);
        endPicker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue)
                _settings.SemesterEnd = dp.SelectedDate.Value;
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock { Text = "从 ", Foreground = fg, VerticalAlignment = VerticalAlignment.Center });
        stack.Children.Add(startPicker);
        stack.Children.Add(new TextBlock { Text = " 到 ", Foreground = fg, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
        stack.Children.Add(endPicker);
        return stack;
    }

    private static DatePicker MakeDatePicker(DateTime date, Brush inputBg, Brush fg)
    {
        return new DatePicker
        {
            SelectedDate = date,
            Background = inputBg,
            Foreground = fg,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Width = 160,
            Height = 32,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private UIElement BuildDisplayOptions(Brush fg)
    {
        var simpleRadio = new RadioButton
        {
            Content = "简洁模式 — 仅显示进度条和校日数",
            IsChecked = !_settings.IsDetailedMode,
            Foreground = fg,
            Margin = new Thickness(0, 4, 0, 4),
            FontSize = 13
        };
        simpleRadio.Checked += (_, _) => _settings.IsDetailedMode = false;

        var detailRadio = new RadioButton
        {
            Content = "详细模式 — 额外显示今日信息和距放假天数",
            IsChecked = _settings.IsDetailedMode,
            Foreground = fg,
            Margin = new Thickness(0, 4, 0, 4),
            FontSize = 13
        };
        detailRadio.Checked += (_, _) => _settings.IsDetailedMode = true;

        var autoUpdateCb = new CheckBox
        {
            Content = "启动时自动更新节假日数据",
            IsChecked = _settings.AutoUpdateHolidays,
            Foreground = fg,
            Margin = new Thickness(0, 8, 0, 4),
            FontSize = 13
        };
        autoUpdateCb.Checked += (_, _) => _settings.AutoUpdateHolidays = true;
        autoUpdateCb.Unchecked += (_, _) => _settings.AutoUpdateHolidays = false;

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(simpleRadio);
        stack.Children.Add(detailRadio);
        stack.Children.Add(autoUpdateCb);
        return stack;
    }

    private UIElement BuildHolidaySection(Brush fg, Brush inputBg)
    {
        _statusLabel = new TextBlock
        {
            Text = _holidayService.CurrentData != null && !string.IsNullOrEmpty(_holidayService.CurrentData.LastUpdated)
                ? $"上次更新: {_holidayService.CurrentData.LastUpdated}"
                : "尚未更新节假日数据",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var updateBtn = MakeButton("联网更新节假日数据", fg);
        updateBtn.Click += async (_, _) => await OnUpdateClick();

        _holidayListLabel = new TextBlock
        {
            Text = GetHolidaySummary(),
            FontSize = 12,
            Foreground = fg,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8),
            MaxHeight = 120
        };

        var addDateBox = new TextBox
        {
            Text = DateTime.Now.ToString("yyyy-MM-dd"),
            Width = 110,
            Height = 30,
            Background = inputBg,
            Foreground = fg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        _addDateBox = addDateBox;

        var addNameBox = new TextBox
        {
            Text = "",
            Width = 120,
            Height = 30,
            Background = inputBg,
            Foreground = fg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        _addNameBox = addNameBox;

        var typeCombo = new ComboBox
        {
            Width = 90,
            Height = 30,
            Background = inputBg,
            Foreground = fg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        typeCombo.Items.Add("假期");
        typeCombo.Items.Add("补班");
        typeCombo.SelectedIndex = 0;
        _addTypeCombo = typeCombo;

        var addBtn = MakeButton("添加", fg);
        addBtn.Width = 60;
        addBtn.Click += (_, _) => OnAddClick();

        var addRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };
        addRow.Children.Add(addDateBox);
        addRow.Children.Add(addNameBox);
        addRow.Children.Add(typeCombo);
        addRow.Children.Add(addBtn);

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(_statusLabel);
        stack.Children.Add(updateBtn);
        stack.Children.Add(_holidayListLabel);
        stack.Children.Add(addRow);
        return stack;
    }

    private async Task OnUpdateClick()
    {
        _statusLabel.Text = "正在更新...";
        var success = await _holidayService.UpdateFromApiAsync();
        _statusLabel.Text = success
            ? $"更新成功! {_holidayService.CurrentData?.LastUpdated}"
            : "更新失败，请检查网络连接";
        _holidayListLabel.Text = GetHolidaySummary();
    }

    private void OnAddClick()
    {
        if (!DateTime.TryParse(_addDateBox.Text, out var date)) return;
        var name = string.IsNullOrWhiteSpace(_addNameBox.Text) ? "手动添加" : _addNameBox.Text;
        var type = _addTypeCombo.SelectedIndex == 0 ? "holiday" : "makeup";
        _holidayService.AddManualEntry(date.ToString("yyyy-MM-dd"), name, type);
        _holidayListLabel.Text = GetHolidaySummary();
        _statusLabel.Text = $"已添加: {date:yyyy-MM-dd} ({name})";
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
            summary += "\n补班日: ";
            foreach (var m in makeups)
                summary += $"{m.Date}({m.Name}) ";
        }
        return summary;
    }

    private static Button MakeButton(string text, Brush fg)
    {
        var bg = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var hoverBg = new SolidColorBrush(Color.FromRgb(84, 84, 84));

        var btn = new Button
        {
            Content = text,
            Height = 30,
            Padding = new Thickness(12, 0, 12, 0),
            Background = bg,
            Foreground = fg,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = bg;
        return btn;
    }

    private static Border BuildCard(string title, string desc, string iconChar, Brush fg, Brush cardBg, UIElement rightContent)
    {
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

        var iconPath = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M13,3 L11,3 L11,11 L13,11 Z M6.5,3 L4.5,3 L4.5,11 L6.5,11 Z M13,13 L4.5,13 L4.5,15 L13,15 Z M13,1 L13,3 L4.5,3 L4.5,1 Z"),
            Fill = fg,
            Width = 20, Height = 20,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };

        var cardRow = new Grid();
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(iconPath, 0);
        Grid.SetColumn(textStack, 1);
        if (rightContent is FrameworkElement fe) Grid.SetColumn(fe, 2);
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
}
