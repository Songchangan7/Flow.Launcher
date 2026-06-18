using System;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Note.Views;

public partial class SettingsControl : UserControl
{
    private readonly Action _applyAction;
    private readonly Func<string> _activePathProvider;
    private readonly SettingsViewModel _viewModel;

    public SettingsControl(Settings settings, Action applyAction, Func<string> activePathProvider)
    {
        InitializeComponent();
        _applyAction = applyAction;
        _activePathProvider = activePathProvider;
        _viewModel = new SettingsViewModel(settings)
        {
            ActivePathText = $"当前生效路径：{_activePathProvider()}"
        };
        DataContext = _viewModel;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        _applyAction();
        _viewModel.ActivePathText = $"当前生效路径：{_activePathProvider()}";
        Main.Context.API.ShowMsg("随手记配置已保存", "新的笔记保存路径已应用。");
    }

    private sealed class SettingsViewModel : BaseModel
    {
        private string _activePathText = string.Empty;

        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
        }

        public Settings Settings { get; }

        public string NotesFilePath
        {
            get => Settings.NotesFilePath;
            set
            {
                if (Settings.NotesFilePath != value)
                {
                    Settings.NotesFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ActivePathText
        {
            get => _activePathText;
            set
            {
                if (_activePathText != value)
                {
                    _activePathText = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
