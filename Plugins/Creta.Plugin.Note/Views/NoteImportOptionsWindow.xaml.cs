using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Creta.Plugin.Note.Views;

public partial class NoteImportOptionsWindow
{
    private readonly List<RadioButton> _splitModeButtons = [];

    public NoteImportOptionsWindow(NotesTextImportSplitMode suggestedSplitMode, bool showMarkdownHeadingOption)
    {
        InitializeComponent();
        CancelButton.Content = Localize.creta_plugin_note_editor_cancel();
        ConfirmButton.Content = Localize.creta_plugin_note_settings_notes_import_confirm();

        AddSplitOption(
            NotesTextImportSplitMode.EntireFile,
            Localize.creta_plugin_note_settings_notes_import_split_entire_file(),
            suggestedSplitMode);
        AddSplitOption(
            NotesTextImportSplitMode.DashSeparator,
            Localize.creta_plugin_note_settings_notes_import_split_dash_separator(),
            suggestedSplitMode);
        AddSplitOption(
            NotesTextImportSplitMode.BlankLine,
            Localize.creta_plugin_note_settings_notes_import_split_blank_line(),
            suggestedSplitMode);

        if (showMarkdownHeadingOption)
        {
            AddSplitOption(
                NotesTextImportSplitMode.MarkdownHeading,
                Localize.creta_plugin_note_settings_notes_import_split_markdown_heading(),
                suggestedSplitMode);
        }
    }

    public NotesTextImportSplitMode SplitMode
    {
        get
        {
            foreach (var button in _splitModeButtons)
            {
                if (button.IsChecked == true && button.Tag is NotesTextImportSplitMode mode)
                {
                    return mode;
                }
            }

            return NotesTextImportSplitMode.EntireFile;
        }
    }

    public bool SkipDuplicates => SkipDuplicatesCheckBox.IsChecked == true;

    private void AddSplitOption(NotesTextImportSplitMode mode, string label, NotesTextImportSplitMode suggestedSplitMode)
    {
        var button = new RadioButton
        {
            Content = label,
            Tag = mode,
            Margin = new Thickness(0, 0, 0, 6),
            IsChecked = mode == suggestedSplitMode
        };
        _splitModeButtons.Add(button);
        SplitModePanel.Children.Add(button);
    }

    private void ConfirmEdit(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelEdit(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
