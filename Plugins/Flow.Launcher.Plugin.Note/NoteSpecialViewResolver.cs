namespace Flow.Launcher.Plugin.Note;

internal static class NoteSpecialViewResolver
{
    internal static NoteSpecialView Resolve(string trimmedContent, NoteRepository repository, int browseNotesLimit)
    {
        if (NoteSpecialViewKeywords.IsAll(trimmedContent))
        {
            var notes = repository.GetAllNotes(browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.All,
                Title = Localize.flowlauncher_plugin_note_special_all_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_all_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (NoteSpecialViewKeywords.IsPinned(trimmedContent))
        {
            var notes = repository.GetPinnedNotes(browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.Pinned,
                Title = Localize.flowlauncher_plugin_note_special_pinned_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_pinned_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (NoteSpecialViewKeywords.IsToday(trimmedContent))
        {
            var notes = repository.GetNotesCreatedOn(System.DateTime.Now, browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.Today,
                Title = Localize.flowlauncher_plugin_note_special_today_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_today_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (NoteSpecialViewKeywords.IsWeek(trimmedContent))
        {
            var notes = repository.GetNotesCreatedThisWeek(browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.Week,
                Title = Localize.flowlauncher_plugin_note_special_week_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_week_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (NoteSpecialViewKeywords.IsRecent(trimmedContent))
        {
            var notes = repository.GetRecentNotes(browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.Recent,
                Title = Localize.flowlauncher_plugin_note_special_recent_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_recent_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (NoteSpecialViewKeywords.IsArchived(trimmedContent))
        {
            var notes = repository.GetArchivedNotes(browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.Archived,
                Title = Localize.flowlauncher_plugin_note_special_archived_title(),
                Subtitle = Localize.flowlauncher_plugin_note_special_archived_subtitle(notes.Count),
                Notes = notes
            };
        }

        if (trimmedContent.StartsWith("tag ", System.StringComparison.OrdinalIgnoreCase))
        {
            var tag = trimmedContent[4..].Trim();
            var notes = repository.GetNotesByTag(tag, browseNotesLimit);
            return new NoteSpecialView
            {
                Key = NoteSpecialViewKeywords.BuildTagViewKey(tag),
                Title = Localize.flowlauncher_plugin_note_special_tag_title(tag),
                Subtitle = Localize.flowlauncher_plugin_note_special_tag_subtitle(tag, notes.Count),
                Notes = notes
            };
        }

        return null;
    }
}
