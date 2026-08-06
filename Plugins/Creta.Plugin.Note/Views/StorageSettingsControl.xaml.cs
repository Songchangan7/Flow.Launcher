using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Microsoft.Win32;

namespace Creta.Plugin.Note.Views;

public partial class StorageSettingsControl : UserControl
{
    private readonly Func<NoteStorageChangeResult> _applyAction;
    private readonly Func<string> _activePathProvider;
    private readonly SettingsViewModel _viewModel;

    public StorageSettingsControl(Settings settings, Func<NoteStorageChangeResult> applyAction, Func<string> activePathProvider)
    {
        InitializeComponent();
        _applyAction = applyAction;
        _activePathProvider = activePathProvider;
        _viewModel = new SettingsViewModel(settings)
        {
            ActivePathText = FormatActivePathText(_activePathProvider())
        };
        DataContext = _viewModel;
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Localize.creta_plugin_note_settings_storage_browse_dialog_title(),
            Filter = Localize.creta_plugin_note_settings_storage_browse_filter(),
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
        _viewModel.ActivePathText = FormatActivePathText(_activePathProvider());

        if (result.Succeeded)
        {
            Main.Context.API.ShowMsg(
                Localize.creta_plugin_note_settings_storage_saved_title(),
                BuildSuccessMessage(result));
            return;
        }

        Main.Context.API.ShowMsgError(
            Localize.creta_plugin_note_settings_storage_save_failed_title(),
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

    private static string FormatActivePathText(string path)
    {
        return Localize.creta_plugin_note_settings_storage_active_path(path);
    }

    private static string BuildSuccessMessage(NoteStorageChangeResult result)
    {
        if (!result.PathChanged)
        {
            return Localize.creta_plugin_note_settings_storage_success_unchanged(result.CurrentNoteCount);
        }

        if (result.NotesMerged)
        {
            return Localize.creta_plugin_note_settings_storage_success_merged(
                result.MigratedNoteCount,
                result.ExistingTargetNoteCount,
                result.CurrentNoteCount);
        }

        return Localize.creta_plugin_note_settings_storage_success_migrated(
            result.MigratedNoteCount,
            result.CurrentNoteCount);
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
