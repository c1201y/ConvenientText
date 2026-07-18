using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ConvenientText.Models;
using ConvenientText.Services;

namespace ConvenientText.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "在校日统计",
    "\uE787",
    "显示学期在校日统计和进度")]
public partial class SchoolStatsComponent : ComponentBase<SchoolStatsSettings>
{
    private readonly SchoolStatsModel _model = new();
    private readonly HolidayService _holidayService;
    private SchoolStatsSettings _settings = null!;
    private TextBlock _infoText = null!;
    private Rectangle _progressBar = null!;
    private Grid _progressBg = null!;

    public SchoolStatsComponent()
    {
        _holidayService = IAppHost.GetService<HolidayService>() ?? new HolidayService();
        _settings = IAppHost.GetService<SchoolStatsSettings>() ?? new SchoolStatsSettings();
        _settings.PropertyChanged += (_, _) => RefreshStats();
        _holidayService.Load();
        BuildUI();
        RefreshStats();
    }

    private void BuildUI()
    {
        var fg = Application.Current?.TryFindResource("MaterialDesignBody") as Brush ?? Brushes.White;
        var baseFontSize = 16.0;
        if (Application.Current?.TryFindResource("MainWindowBodyFontSize") is double s) baseFontSize = s;
        var fontSize = baseFontSize * 22.0 / 16.0;
        var accentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));

        _infoText = new TextBlock
        {
            Foreground = fg,
            FontSize = fontSize,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Thickness(0, 2, 0, 2)
        };

        _progressBg = new Grid
        {
            Height = 4,
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
        };
        _progressBar = new Rectangle
        {
            Height = 4,
            Fill = accentBrush,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _progressBg.Children.Add(_progressBar);

        var stack = new StackPanel();
        stack.Children.Add(_infoText);
        stack.Children.Add(_progressBg);

        Content = stack;
        ((FrameworkElement)Content).SizeChanged += (_, _) => UpdateProgressBar();
    }

    private void RefreshStats()
    {
        var today = DateTime.Today;
        var start = _settings.SemesterStart.Date;
        var end = _settings.SemesterEnd.Date;

        var total = _holidayService.CountSchoolDaysInRange(start, end);
        var passed = today > start ? _holidayService.CountSchoolDaysInRange(start, today.AddDays(-1)) : 0;
        var remaining = today <= end ? _holidayService.CountSchoolDaysInRange(today, end) : 0;

        _model.TotalSchoolDays = total;
        _model.PassedSchoolDays = passed;
        _model.RemainingSchoolDays = remaining;
        _model.Progress = total > 0 ? (double)passed / total * 100 : 0;

        var daysLeft = (end - today).Days;

        if (_settings.IsDetailedMode)
        {
            var dayNames = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            var dayType = _holidayService.IsSchoolDay(today) ? "校日" : "非校日";
            _infoText.Text = $"{today:MM/dd} {dayNames[(int)today.DayOfWeek]} {dayType} | {passed}天/{total}天";
        }
        else
        {
            var breakText = daysLeft > 0 ? $" | 距放假{daysLeft}天" : " | 已放假";
            _infoText.Text = $"在校 {passed}天/{total}天{breakText}";
        }

        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (_progressBg == null || _progressBar == null) return;
        var w = _progressBg.ActualWidth;
        if (double.IsNaN(w) || double.IsInfinity(w) || w <= 0) return;
        var p = _model.Progress;
        if (double.IsNaN(p) || double.IsInfinity(p)) p = 0;
        _progressBar.Width = Math.Max(0, Math.Min(w, w * p / 100));
    }
}
