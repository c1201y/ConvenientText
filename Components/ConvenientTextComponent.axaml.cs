using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ConvenientText.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConvenientText.Components;

[ComponentInfo(
    "9E7F8A2D-4C1B-4E5F-9A3C-7D8B2E1F0A3C",
    "便捷文本",
    "\uE9B0",
    "可通过悬浮窗快速修改文字")]
public partial class ConvenientTextComponent : ComponentBase<TextDataModel>
{
    private readonly TextBlock _textBlock;
    private TextDataModel? _dataModel;

    public ConvenientTextComponent()
    {
        _textBlock = new TextBlock
        {
            Text = "点击✎编辑文字",
            Foreground = Brushes.White,
            FontSize = 18,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        };
        Content = _textBlock;

        InitializeFromDataModel();
    }

    private void InitializeFromDataModel()
    {
        _dataModel = IAppHost.GetService<TextDataModel>();
        if (_dataModel == null) return;

        UpdateUI();

        _dataModel.PropertyChanged += (_, _) =>
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(UpdateUI));
        };
    }

    private void UpdateUI()
    {
        if (_dataModel == null) return;

        _textBlock.Text = _dataModel.DisplayText;
        _textBlock.Foreground = new SolidColorBrush(_dataModel.TextColor);
        _textBlock.FontSize = _dataModel.FontSize;
    }
}