using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ConvenientText.Models;

namespace ConvenientText.Components;

[SettingsPageInfo("convenienttext.dutyrota", "值日轮换", "\uE77B", "\uE77B", SettingsPageCategory.External)]
public class DutyRotaSettingsControl : SettingsPageBase
{
    private readonly DutyRotaSettings _settings;
    private StackPanel _nameList = null!;
    private TextBlock _previewLabel = null!;

    private static Brush TryBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) is Brush b ? b : fallback;

    public DutyRotaSettingsControl()
    {
        _settings = Plugin.DutyRotaSettings;

        var fg = TryBrush("MaterialDesignBody", Brushes.Black);
        var inputBg = TryBrush("MaterialDesignPaper", new SolidColorBrush(Color.FromRgb(60, 60, 60)));

        Brush cardBg;
        if (TryBrush("MaterialDesignCardBackground", Brushes.Transparent) is SolidColorBrush solid && solid.Color != Colors.Transparent)
            cardBg = solid;
        else if (fg is SolidColorBrush fgColor && fgColor.Color.R > 128)
            cardBg = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
        else
            cardBg = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));

        Content = new ScrollViewer { Content = BuildLayout(fg, cardBg, inputBg) };
    }

    private StackPanel BuildLayout(Brush fg, Brush cardBg, Brush inputBg)
    {
        var stack = new StackPanel { MaxWidth = 750, Margin = new Thickness(16) };

        stack.Children.Add(new TextBlock
        {
            Text = "值日轮换",
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Foreground = fg,
            Margin = new Thickness(0, 0, 0, 10)
        });

        stack.Children.Add(BuildCard("起始日期",
            "值日轮换的起始日期",
            "\uE787", fg, cardBg, BuildDatePicker(fg, inputBg)));

        stack.Children.Add(BuildCard("值日生名单",
            "按顺序添加值日生，按天轮换",
            "\uE77B", fg, cardBg, BuildNameSection(fg, inputBg)));

        stack.Children.Add(BuildCard("预览",
            "查看未来几天的值日安排",
            "\uE81D", fg, cardBg, BuildPreview(fg)));

        return stack;
    }

    private UIElement BuildDatePicker(Brush fg, Brush inputBg)
    {
        var picker = new DatePicker
        {
            SelectedDate = _settings.StartDate,
            Background = inputBg,
            Foreground = fg,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Width = 160,
            Height = 32,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        picker.SelectedDateChanged += (s, _) =>
        {
            if (s is DatePicker dp && dp.SelectedDate.HasValue)
                _settings.StartDate = dp.SelectedDate.Value;
            RefreshPreview();
        };
        return picker;
    }

    private UIElement BuildNameSection(Brush fg, Brush inputBg)
    {
        _nameList = new StackPanel();

        var addBox = new TextBox
        {
            Width = 120,
            Height = 30,
            Background = inputBg,
            Foreground = fg,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 13,
            Margin = new Thickness(0, 0, 6, 0)
        };

        var addBtn = MakeButton("添加", fg, 50);
        addBtn.Click += (_, _) =>
        {
            var name = addBox.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;
            _settings.Names.Add(name);
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
        _nameList.Children.Clear();
        for (int i = 0; i < _settings.Names.Count; i++)
        {
            var name = _settings.Names[i];
            var idx = i;
            var label = new TextBlock
            {
                Text = $"{i + 1}. {name}",
                FontSize = 13,
                Foreground = TryBrush("MaterialDesignBody", Brushes.Black),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 8, 2)
            };

            var upBtn = MakeButton("\uE70E", Brushes.Black, 28);
            upBtn.Click += (_, _) =>
            {
                if (idx > 0)
                {
                    (_settings.Names[idx], _settings.Names[idx - 1]) = (_settings.Names[idx - 1], _settings.Names[idx]);
                    RefreshNameList();
                    RefreshPreview();
                }
            };

            var downBtn = MakeButton("\uE70D", Brushes.Black, 28);
            downBtn.Click += (_, _) =>
            {
                if (idx < _settings.Names.Count - 1)
                {
                    (_settings.Names[idx], _settings.Names[idx + 1]) = (_settings.Names[idx + 1], _settings.Names[idx]);
                    RefreshNameList();
                    RefreshPreview();
                }
            };

            var delBtn = MakeButton("\uE711", Brushes.Black, 28);
            delBtn.Click += (_, _) =>
            {
                _settings.Names.RemoveAt(idx);
                RefreshNameList();
                RefreshPreview();
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(label);
            row.Children.Add(upBtn);
            row.Children.Add(downBtn);
            row.Children.Add(delBtn);

            _nameList.Children.Add(row);
        }
    }

    private UIElement BuildPreview(Brush fg)
    {
        _previewLabel = new TextBlock
        {
            FontSize = 12,
            Foreground = fg,
            TextWrapping = TextWrapping.Wrap
        };
        RefreshPreview();
        return _previewLabel;
    }

    private void RefreshPreview()
    {
        if (_previewLabel == null) return;
        if (_settings.Names.Count == 0)
        {
            _previewLabel.Text = "请先添加值日生";
            return;
        }

        var today = DateTime.Today;
        var lines = "";
        for (int i = 0; i < Math.Min(7, _settings.Names.Count + 1); i++)
        {
            var date = today.AddDays(i);
            var duty = _settings.GetDutyForDate(date);
            var dayStr = i == 0 ? "今天" : i == 1 ? "明天" : $"{date:MM/dd}";
            lines += $"{dayStr}: {duty}\n";
        }
        _previewLabel.Text = lines.TrimEnd('\n');
    }

    private static Button MakeButton(string text, Brush fg, int width)
    {
        var bg = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        var hoverBg = new SolidColorBrush(Color.FromRgb(84, 84, 84));
        var btn = new Button
        {
            Content = text,
            Width = width,
            Height = 28,
            Background = bg,
            Foreground = fg,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 12,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(0)
        };
        btn.MouseEnter += (_, _) => btn.Background = hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = bg;
        return btn;
    }

    private static Border BuildCard(string title, string desc, string iconChar, Brush fg, Brush cardBg, UIElement rightContent)
    {
        var iconBlock = new TextBlock
        {
            Text = iconChar,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };

        var headerBlock = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = fg };
        var descBlock = new TextBlock
        {
            Text = desc, FontSize = 11, Foreground = fg,
            Opacity = 0.5, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(headerBlock);
        textStack.Children.Add(descBlock);

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(iconBlock);
        titleRow.Children.Add(textStack);

        var cardStack = new StackPanel();
        cardStack.Children.Add(titleRow);
        cardStack.Children.Add(rightContent);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 4),
            Background = cardBg,
            Child = cardStack
        };
    }
}
