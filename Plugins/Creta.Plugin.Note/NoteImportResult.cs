namespace Creta.Plugin.Note;

public sealed class NoteImportResult
{
    public bool Succeeded { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public int ImportedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int SkippedDuplicateCount { get; init; }

    public int SkippedEmptyCount { get; init; }
}
