namespace Flow.Launcher.Plugin.Note;

public sealed class NoteStorageChangeResult
{
    public bool Succeeded { get; init; }

    public bool PathChanged { get; init; }

    public bool NotesMerged { get; init; }

    public int MigratedNoteCount { get; init; }

    public int ExistingTargetNoteCount { get; init; }

    public int CurrentNoteCount { get; init; }

    public string PreviousPath { get; init; } = string.Empty;

    public string CurrentPath { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;
}
