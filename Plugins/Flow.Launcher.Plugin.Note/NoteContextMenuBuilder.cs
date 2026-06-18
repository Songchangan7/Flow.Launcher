using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Note;

internal sealed class NoteContextMenuBuilder
{
    internal List<Result> Build(
        NoteItem note,
        System.Func<NoteItem, bool> openEditor,
        System.Func<NoteItem, bool> copyNote,
        System.Func<NoteItem, bool> useAsQuery,
        System.Func<NoteItem, bool> toggleArchived,
        System.Func<NoteItem, bool> togglePinned,
        System.Func<NoteItem, bool> beginEdit,
        System.Func<NoteItem, bool> deleteNote)
    {
        return
        [
            new Result
            {
                Title = Localize.flowlauncher_plugin_note_editor_open_title(),
                SubTitle = Localize.flowlauncher_plugin_note_editor_edit_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => openEditor(note)
            },
            new Result
            {
                Title = Localize.flowlauncher_plugin_note_copy_title(),
                SubTitle = Localize.flowlauncher_plugin_note_copy_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => copyNote(note)
            },
            new Result
            {
                Title = Localize.flowlauncher_plugin_note_use_query_title(),
                SubTitle = Localize.flowlauncher_plugin_note_use_query_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => useAsQuery(note)
            },
            new Result
            {
                Title = note.IsArchived
                    ? Localize.flowlauncher_plugin_note_unarchive_title()
                    : Localize.flowlauncher_plugin_note_archive_title(),
                SubTitle = note.IsArchived
                    ? Localize.flowlauncher_plugin_note_unarchive_subtitle()
                    : Localize.flowlauncher_plugin_note_archive_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => toggleArchived(note)
            },
            new Result
            {
                Title = note.IsPinned
                    ? Localize.flowlauncher_plugin_note_unpin_title()
                    : Localize.flowlauncher_plugin_note_pin_title(),
                SubTitle = note.IsPinned
                    ? Localize.flowlauncher_plugin_note_unpin_subtitle()
                    : Localize.flowlauncher_plugin_note_pin_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => togglePinned(note)
            },
            new Result
            {
                Title = Localize.flowlauncher_plugin_note_edit_title(),
                SubTitle = Localize.flowlauncher_plugin_note_edit_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => beginEdit(note)
            },
            new Result
            {
                Title = Localize.flowlauncher_plugin_note_delete_title(),
                SubTitle = Localize.flowlauncher_plugin_note_delete_subtitle(),
                IcoPath = Main.IcoPathValue,
                Action = _ => deleteNote(note)
            }
        ];
    }
}
