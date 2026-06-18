using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Note;

internal sealed class NoteViewStats
{
    public int TotalNotesCount { get; init; }

    public int PinnedNotesCount { get; init; }

    public int TodayNotesCount { get; init; }

    public int RecentNotesCount { get; init; }

    public int ArchivedNotesCount { get; init; }

    public IReadOnlyList<KeyValuePair<string, int>> TopTags { get; init; } = [];

    internal static NoteViewStats Create(NoteRepository repository, int browseNotesLimit, int tagListLimit)
    {
        var allNotesCount = repository.GetAllNotes(browseNotesLimit).Count;
        return new NoteViewStats
        {
            TotalNotesCount = allNotesCount,
            PinnedNotesCount = repository.GetPinnedNotes(browseNotesLimit).Count,
            TodayNotesCount = repository.GetNotesCreatedOn(DateTime.Now, browseNotesLimit).Count,
            RecentNotesCount = Math.Min(repository.GetRecentNotes(browseNotesLimit).Count, browseNotesLimit),
            ArchivedNotesCount = repository.GetArchivedNotes(browseNotesLimit).Count,
            TopTags = repository.GetTopTags(tagListLimit)
        };
    }
}
