using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Creta.Plugin.Note;

internal sealed class NoteResultFactory
{
    private readonly string _actionKeyword;

    internal NoteResultFactory(string actionKeyword)
    {
        _actionKeyword = actionKeyword;
    }

    internal Result CreateSectionResult(string title, string subtitle, int score)
    {
        return new Result
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = Main.IcoPathValue,
            Score = score,
            Action = _ => false
        };
    }

    internal Result CreateViewJumpResult(string title, string subtitle, string viewKeyword, int score)
    {
        return new Result
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = Main.IcoPathValue,
            Score = score,
            Action = _ =>
            {
                Main.Context.API.ChangeQuery($"{_actionKeyword} {viewKeyword}", true);
                return false;
            }
        };
    }

    internal Result CreateTagJumpResult(KeyValuePair<string, int> tag)
    {
        return new Result
        {
            Title = $"#{tag.Key}",
            SubTitle = Localize.creta_plugin_note_tag_jump_subtitle(tag.Key, tag.Value),
            IcoPath = Main.IcoPathValue,
            Score = 934,
            Action = _ =>
            {
                Main.Context.API.ChangeQuery($"{_actionKeyword} tag {tag.Key}", true);
                return false;
            }
        };
    }

    internal Result CreateRecentNoteResult(NoteItem note)
    {
        var updatedLabel = note.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return new Result
        {
            Title = NotePresentation.BuildSavedSubtitle(note.Content),
            SubTitle = note.IsArchived
                ? Localize.creta_plugin_note_archived_note_subtitle(updatedLabel)
                : note.IsPinned
                    ? Localize.creta_plugin_note_pinned_note_subtitle(updatedLabel)
                    : Localize.creta_plugin_note_recent_note_subtitle(updatedLabel),
            IcoPath = Main.IcoPathValue,
            Score = note.IsArchived ? 820 : note.IsPinned ? 950 : 850,
            CopyText = note.Content,
            AutoCompleteText = $"{_actionKeyword} {note.Content}",
            TitleToolTip = NotePresentation.BuildNoteTitleToolTip(note),
            SubTitleToolTip = note.Content,
            Preview = NotePresentation.BuildPreviewInfo(note),
            ContextData = note,
            Action = _ => Main.CopyNoteToClipboardStatic(note)
        };
    }

    internal Result CreateSearchNoteResult(NoteSearchMatch match)
    {
        return new Result
        {
            Title = NotePresentation.BuildSavedSubtitle(match.Note.Content),
            SubTitle = NotePresentation.BuildSearchResultSubtitle(match),
            IcoPath = Main.IcoPathValue,
            Score = match.Score,
            CopyText = match.Note.Content,
            AutoCompleteText = $"{_actionKeyword} {match.Note.Content}",
            TitleHighlightData = match.HighlightData,
            TitleToolTip = NotePresentation.BuildNoteTitleToolTip(match.Note),
            SubTitleToolTip = match.Note.Content,
            Preview = NotePresentation.BuildPreviewInfo(match.Note),
            ContextData = match.Note,
            Action = _ => Main.CopyNoteToClipboardStatic(match.Note)
        };
    }
}
