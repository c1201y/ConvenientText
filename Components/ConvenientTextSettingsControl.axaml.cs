using System.Windows.Controls;
using System.Windows;

namespace ConvenientText.Components;

public partial class ConvenientTextSettingsControl : UserControl
{
    public ConvenientTextSettingsControl()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(12),
            Children =
            {
                new TextBlock
                {
                    Text = "便捷文本设置",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                new TextBlock
                {
                    Text = "插件通过悬浮窗笔按钮进入编辑界面，设置入口这里显示说明。",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new TextBlock
                {
                    Text = "编辑窗口始终使用深色主题。",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6)
                },
                new TextBlock
                {
                    Text = "当前可修改文本、颜色和字号，关闭窗口后会自动保存。",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        Content = panel;
    }
}