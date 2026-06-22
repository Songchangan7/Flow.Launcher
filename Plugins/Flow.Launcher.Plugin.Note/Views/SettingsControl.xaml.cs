using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.Note.Views;

public partial class SettingsControl : UserControl
{
    private readonly Func<NoteStorageChangeResult> _applyAction;
    private readonly Func<string> _activePathProvider;
    private readonly SettingsViewModel _viewModel;

    public SettingsControl(Settings settings, Func<NoteStorageChangeResult> applyAction, Func<string> activePathProvider)
    {
        InitializeComponent();
        _applyAction = applyAction;
        _activePathProvider = activePathProvider;
        _viewModel = new SettingsViewModel(settings)
        {
            ActivePathText = IsChineseUi()
                ? $"当前生效路径：{_activePathProvider()}"
                : $"Active path: {_activePathProvider()}"
        };
        DataContext = _viewModel;
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = IsChineseUi() ? "选择随手记存储文件" : "Choose Quick Note storage file",
            Filter = IsChineseUi() ? "JSON 文件|*.json|所有文件|*.*" : "JSON files|*.json|All files|*.*",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = ResolveSuggestedFileName(),
            OverwritePrompt = false
        };

        var initialDirectory = ResolveInitialDirectory();
        if (!string.IsNullOrWhiteSpace(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        _viewModel.NotesFilePath = dialog.FileName;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = _applyAction();
        _viewModel.ActivePathText = IsChineseUi()
            ? $"当前生效路径：{_activePathProvider()}"
            : $"Active path: {_activePathProvider()}";

        if (result.Succeeded)
        {
            Main.Context.API.ShowMsg(
                GetSettingsSavedTitle(),
                BuildSuccessMessage(result));
            return;
        }

        Main.Context.API.ShowMsgError(
            GetSettingsSaveFailedTitle(),
            result.ErrorMessage);
    }

    private string ResolveInitialDirectory()
    {
        try
        {
            var currentPath = _viewModel.NotesFilePath;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                currentPath = _activePathProvider();
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(currentPath);
            if (string.IsNullOrWhiteSpace(expandedPath))
            {
                return string.Empty;
            }

            var fullPath = Path.GetFullPath(expandedPath);
            var directory = Path.GetDirectoryName(fullPath);
            return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)
                ? directory
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ResolveSuggestedFileName()
    {
        try
        {
            var currentPath = _viewModel.NotesFilePath;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                currentPath = _activePathProvider();
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(currentPath);
            var fileName = string.IsNullOrWhiteSpace(expandedPath)
                ? string.Empty
                : Path.GetFileName(expandedPath);

            return string.IsNullOrWhiteSpace(fileName) ? "notes.json" : fileName;
        }
        catch
        {
            return "notes.json";
        }
    }

    private static string BuildSuccessMessage(NoteStorageChangeResult result)
    {
        if (!result.PathChanged)
        {
            return IsChineseUi()
                ? $"存储位置未变化，当前仍可访问 {result.CurrentNoteCount} 条笔记。"
                : $"Storage path unchanged. {result.CurrentNoteCount} notes remain available.";
        }

        if (result.NotesMerged)
        {
            return IsChineseUi()
                ? $"已切换存储位置，并将迁移的 {result.MigratedNoteCount} 条笔记与新位置已有的 {result.ExistingTargetNoteCount} 条笔记合并，当前可访问 {result.CurrentNoteCount} 条笔记。"
                : $"Switched storage path, merged {result.MigratedNoteCount} migrated notes with {result.ExistingTargetNoteCount} existing notes, and now have {result.CurrentNoteCount} notes available.";
        }

        return IsChineseUi()
            ? $"已切换存储位置并迁移 {result.MigratedNoteCount} 条笔记，当前可访问 {result.CurrentNoteCount} 条笔记。"
            : $"Switched storage path and migrated {result.MigratedNoteCount} notes. {result.CurrentNoteCount} notes are now available.";
    }

    private static string GetSettingsSavedTitle()
    {
        return IsChineseUi() ? "随手记配置已保存" : "Quick Note settings saved";
    }

    private static string GetSettingsSaveFailedTitle()
    {
        return IsChineseUi() ? "更新笔记存储位置失败" : "Failed to update note storage";
    }

    private static bool IsChineseUi()
    {
        return string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
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
