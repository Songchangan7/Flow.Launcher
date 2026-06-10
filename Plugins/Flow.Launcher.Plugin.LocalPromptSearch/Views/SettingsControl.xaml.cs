using System;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.LocalPromptSearch.Views;

public partial class SettingsControl : UserControl
{
    private readonly Action _reloadAction;

    public SettingsControl(Settings settings, Action reloadAction)
    {
        InitializeComponent();
        DataContext = settings;
        _reloadAction = reloadAction;
    }

    private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        _reloadAction();
        Main.Context.API.ShowMsg("Prompt 配置已保存", "新的模板文件路径已应用，并已重新加载。");
    }
}
