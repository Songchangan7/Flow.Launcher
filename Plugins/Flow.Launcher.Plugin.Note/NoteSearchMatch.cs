using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Note;

public sealed class NoteSearchMatch
{
    public NoteItem Note { get; init; } = null!;

    public int Score { get; init; }

    public IList<int> HighlightData { get; init; } = [];

    public bool IsExactMatch { get; init; }

    public bool IsPrefixMatch { get; init; }

    public bool IsPhraseMatch { get; init; }

    public bool IsHighSimilarity { get; init; }
}
