using System;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Note.Views;

public partial class SettingsControl : UserControl
{
    private readonly TabItem _notesTab;

    public SettingsControl(
        Settings settings,
        NoteRepository repository,
        Func<NoteStorageChangeResult> applyAction,
        Func<string> activePathProvider)
    {
        InitializeComponent();

        var storageTab = new TabItem
        {
            Header = Localize.flowlauncher_plugin_note_settings_tab_storage(),
            Content = new StorageSettingsControl(settings, applyAction, activePathProvider)
        };

        _notesTab = new TabItem
        {
            Header = Localize.flowlauncher_plugin_note_settings_tab_notes(),
            Content = new NotesManagerControl(repository)
        };

        SettingsTabs.Items.Add(storageTab);
        SettingsTabs.Items.Add(_notesTab);
    }

    internal void SelectNotesManagerTab()
    {
        SettingsTabs.SelectedItem = _notesTab;
    }
}
