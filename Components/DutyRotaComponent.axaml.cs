using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ConvenientText.Models;

namespace ConvenientText.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F12345678901",
    "值日轮换",
    "\uE716",
    "按天数自动轮换值日生")]
public partial class DutyRotaComponent : ComponentBase<DutyRotaSettings>
{
    private readonly DutyRotaSettings _settings;
    private TextBlock _dutyText = null!;

    public DutyRotaComponent()
    {
        _settings = IAppHost.GetService<DutyRotaSettings>() ?? new DutyRotaSettings();
        _settings.PropertyChanged += (_, _) => UpdateUI();
        BuildUI();
        UpdateUI();
    }

    private void BuildUI()
    {
        var fg = Application.Current?.TryFindResource("MaterialDesignBody") as Brush ?? Brushes.White;
        var baseFontSize = 16.0;
        if (Application.Current?.TryFindResource("MainWindowBodyFontSize") is double s) baseFontSize = s;
        var fontSize = baseFontSize * 22.0 / 16.0;

        _dutyText = new TextBlock
        {
            Foreground = fg,
            FontSize = fontSize,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Thickness(0, 2, 0, 2)
        };

        var container = new Grid { VerticalAlignment = VerticalAlignment.Center };
        container.Children.Add(_dutyText);
        Content = container;
    }

    private void UpdateUI()
    {
        _dutyText.Text = $"值日: {_settings.GetTodayDuty()}";
    }
}
