using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Flow.Launcher.Plugin.Note.Views;

public partial class NoteTagsEditorWindow
{
    public IReadOnlyList<string> EditedTags { get; private set; } = [];

    public NoteTagsEditorWindow(
        string initialTagsText,
        string title = null,
        string hint = null,
        string confirmText = null)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(hint))
        {
            HintTextBlock.Text = hint;
        }

        CancelButton.Content = Localize.flowlauncher_plugin_note_editor_cancel();
        ConfirmButton.Content = confirmText
            ?? Localize.flowlauncher_plugin_note_settings_notes_tags_editor_save();
        TagsTextBox.Text = initialTagsText ?? string.Empty;

        Loaded += (_, _) =>
        {
            TagsTextBox.Focus();
            TagsTextBox.SelectAll();
        };
    }

    private void ConfirmEdit(object sender, RoutedEventArgs e)
    {
        EditedTags = NotesManagerTagParser.Parse(TagsTextBox.Text);
        DialogResult = true;
        Close();
    }

    private void CancelEdit(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

internal static class NotesManagerTagParser
{
    internal static string Format(IReadOnlyList<string> tags)
    {
        return tags is null || tags.Count == 0
            ? string.Empty
            : string.Join(" ", tags.Select(tag => $"#{tag}"));
    }

    internal static IReadOnlyList<string> Parse(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return [];
        }

        return rawInput
            .Split([',', ';', ' ', '\t', '\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim().TrimStart('#').ToLowerInvariant())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static IReadOnlyList<string> Merge(
        IEnumerable<string> existingTags,
        IEnumerable<string> additionalTags)
    {
        return (existingTags ?? [])
            .Concat(additionalTags ?? [])
            .Select(tag => tag?.Trim().TrimStart('#').ToLowerInvariant() ?? string.Empty)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
