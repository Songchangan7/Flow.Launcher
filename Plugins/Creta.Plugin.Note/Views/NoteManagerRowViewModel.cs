using System;

namespace Creta.Plugin.Note.Views;

internal sealed class NoteManagerRowViewModel
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    internal NoteManagerRowViewModel(NoteItem note)
    {
        Note = note;
    }

    internal NoteItem Note { get; }

    internal string ContentSummary => NotePresentation.BuildSavedSubtitle(Note.Content);

    internal string TagsText => NotePresentation.BuildTagText(Note);

    internal string CreatedAtText => FormatDateTime(Note.CreatedAt);

    internal string UpdatedAtText => FormatDateTime(Note.UpdatedAt);

    internal bool IsPinned => Note.IsPinned;

    internal bool IsArchived => Note.IsArchived;

    internal bool CanTogglePinned => !Note.IsArchived;

    internal string ContentToolTip => Note.Content;

    internal string RowToolTip => NotePresentation.BuildNoteTitleToolTip(Note);

    private static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString(DateTimeFormat);
    }
}
