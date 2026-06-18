using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Note;

internal sealed class NoteSpecialView
{
    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public IReadOnlyList<NoteItem> Notes { get; init; } = [];
}
