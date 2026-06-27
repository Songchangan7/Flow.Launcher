using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Note.Views;

internal static class NotesManagerExporter
{
    internal static string BuildTextExport(IReadOnlyList<NoteItem> notes)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < notes.Count; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
                builder.AppendLine(new string('-', 40));
                builder.AppendLine();
            }

            AppendNoteText(builder, notes[index]);
        }

        return builder.ToString();
    }

    internal static string BuildMarkdownExport(IReadOnlyList<NoteItem> notes)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < notes.Count; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
            }

            AppendNoteMarkdown(builder, notes[index], index + 1);
        }

        return builder.ToString();
    }

    internal static bool TryExportNotes(
        IReadOnlyList<NoteItem> notes,
        string dialogTitle,
        string defaultFileName,
        string filter,
        Func<IReadOnlyList<NoteItem>, string> buildContent)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = dialogTitle,
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return false;
        }

        try
        {
            File.WriteAllText(dialog.FileName, buildContent(notes), Encoding.UTF8);
            Main.Context.API.ShowMsg(
                Localize.flowlauncher_plugin_note_settings_notes_export_success_title(),
                Localize.flowlauncher_plugin_note_settings_notes_export_success_subtitle(notes.Count, dialog.FileName));
            return true;
        }
        catch (Exception ex)
        {
            Main.Context.API.ShowMsgError(
                Localize.flowlauncher_plugin_note_settings_notes_export_failed_title(),
                ex.Message);
            return false;
        }
    }

    internal static bool TryExportJsonBackup(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            Main.Context.API.ShowMsgError(
                Localize.flowlauncher_plugin_note_settings_notes_export_json_missing_title(),
                Localize.flowlauncher_plugin_note_settings_notes_export_json_missing_subtitle(sourceFilePath ?? string.Empty));
            return false;
        }

        var sourceDirectory = Path.GetDirectoryName(sourceFilePath);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Localize.flowlauncher_plugin_note_settings_notes_export_dialog_title_json(),
            Filter = Localize.flowlauncher_plugin_note_settings_notes_export_filter_json(),
            FileName = $"notes-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json",
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = string.IsNullOrWhiteSpace(sourceDirectory) ? null : sourceDirectory
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return false;
        }

        try
        {
            File.Copy(sourceFilePath, dialog.FileName, overwrite: true);
            Main.Context.API.ShowMsg(
                Localize.flowlauncher_plugin_note_settings_notes_export_json_success_title(),
                Localize.flowlauncher_plugin_note_settings_notes_export_json_success_subtitle(dialog.FileName));
            return true;
        }
        catch (Exception ex)
        {
            Main.Context.API.ShowMsgError(
                Localize.flowlauncher_plugin_note_settings_notes_export_failed_title(),
                ex.Message);
            return false;
        }
    }

    private static void AppendNoteText(StringBuilder builder, NoteItem note)
    {
        builder.AppendLine($"{Localize.flowlauncher_plugin_note_preview_created_label()} {FormatDateTime(note.CreatedAt)}");
        builder.AppendLine($"{Localize.flowlauncher_plugin_note_preview_updated_label()} {FormatDateTime(note.UpdatedAt)}");
        builder.AppendLine($"{Localize.flowlauncher_plugin_note_preview_tags_label()} {NotePresentation.BuildTagText(note)}");
        builder.AppendLine(note.Content);
    }

    private static void AppendNoteMarkdown(StringBuilder builder, NoteItem note, int index)
    {
        var title = NotePresentation.BuildSavedSubtitle(note.Content);
        builder.AppendLine($"## {index}. {EscapeMarkdown(title)}");
        builder.AppendLine();
        builder.AppendLine($"- **{Localize.flowlauncher_plugin_note_preview_created_label()}** {FormatDateTime(note.CreatedAt)}");
        builder.AppendLine($"- **{Localize.flowlauncher_plugin_note_preview_updated_label()}** {FormatDateTime(note.UpdatedAt)}");
        builder.AppendLine($"- **{Localize.flowlauncher_plugin_note_preview_tags_label()}** {NotePresentation.BuildTagText(note)}");
        builder.AppendLine();
        builder.AppendLine(note.Content);
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("#", "\\#", StringComparison.Ordinal);
    }
}
