using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Note;

public sealed class NoteItem
{
    public string Id { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsPinned { get; set; }

    public bool IsArchived { get; set; }

    public List<string> Tags { get; set; } = [];
}
