using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Flow.Launcher.Plugin.Note;

public sealed class NoteRepository
{
    private const string NotesFileName = "notes.json";
    private const string SampleNotesFileName = "notes.sample.json";

    private readonly string _pluginDirectory;
    private readonly string _defaultStorageDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private List<NoteItem> _notes = [];
    private string _customNotesFilePath;

    public string CustomNotesFilePath => _customNotesFilePath;

    public string NotesFilePath => ResolveNotesFilePath();

    public string LoadError { get; private set; } = string.Empty;

    public NoteRepository(string pluginDirectory, string storageDirectory)
        : this(pluginDirectory, storageDirectory, string.Empty)
    {
    }

    public NoteRepository(string pluginDirectory, string storageDirectory, string customNotesFilePath)
    {
        _pluginDirectory = pluginDirectory;
        _defaultStorageDirectory = storageDirectory;
        _customNotesFilePath = customNotesFilePath?.Trim() ?? string.Empty;
    }

    public IReadOnlyList<NoteItem> Notes => _notes;

    public IReadOnlyList<NoteItem> GetRecentNotes(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => !note.IsArchived)
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> GetAllNotes(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => !note.IsArchived)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> GetPinnedNotes(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => note.IsPinned && !note.IsArchived)
            .OrderByDescending(note => note.UpdatedAt)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> GetArchivedNotes(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => note.IsArchived)
            .OrderByDescending(note => note.UpdatedAt)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<KeyValuePair<string, int>> GetTopTags(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => !note.IsArchived)
            .SelectMany(note => note.Tags)
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
            .OrderByDescending(group => group.Value)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> GetNotesCreatedOn(DateTime date, int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        var target = date.Date;
        return _notes
            .Where(note => !note.IsArchived)
            .Where(note => note.CreatedAt.ToLocalTime().Date == target)
            .OrderByDescending(note => note.UpdatedAt)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> GetNotesByTag(string tag, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(tag) || maxCount <= 0)
        {
            return [];
        }

        var normalizedTag = NormalizeTag(tag);
        return _notes
            .Where(note => !note.IsArchived)
            .Where(note => note.Tags.Any(existingTag =>
                string.Equals(existingTag, normalizedTag, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt)
            .Take(maxCount)
            .ToList();
    }

    public IReadOnlyList<NoteItem> SearchNotes(string search, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(search) || maxCount <= 0)
        {
            return [];
        }

        return _notes
            .Where(note => !note.IsArchived)
            .Select(note => NoteSearchScorer.Match(note, search))
            .Where(match => match is not null)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.Note.UpdatedAt)
            .Take(maxCount)
            .Select(match => match.Note)
            .ToList();
    }

    public NoteStorageChangeResult UpdateStoragePath(string customNotesFilePath)
    {
        var nextCustomNotesFilePath = customNotesFilePath?.Trim() ?? string.Empty;
        var previousCustomNotesFilePath = _customNotesFilePath;
        var previousPath = NotesFilePath;
        var nextPath = ResolveNotesFilePath(nextCustomNotesFilePath);

        if (PathsEqual(previousPath, nextPath))
        {
            _customNotesFilePath = nextCustomNotesFilePath;
            Load();

            return new NoteStorageChangeResult
            {
                Succeeded = string.IsNullOrWhiteSpace(LoadError),
                PathChanged = false,
                PreviousPath = previousPath,
                CurrentPath = NotesFilePath,
                CurrentNoteCount = _notes.Count,
                ErrorMessage = LoadError
            };
        }

        try
        {
            var sourceNotes = CloneNotes(_notes);
            var targetNotes = LoadNotesFromPath(nextPath, initializeIfMissing: false);
            var mergedNotes = MergeNotes(targetNotes, sourceNotes);

            PersistNotesToPath(nextPath, mergedNotes);

            _customNotesFilePath = nextCustomNotesFilePath;
            _notes = mergedNotes;
            LoadError = string.Empty;

            return new NoteStorageChangeResult
            {
                Succeeded = true,
                PathChanged = true,
                NotesMerged = targetNotes.Count > 0,
                PreviousPath = previousPath,
                CurrentPath = NotesFilePath,
                MigratedNoteCount = sourceNotes.Count,
                ExistingTargetNoteCount = targetNotes.Count,
                CurrentNoteCount = _notes.Count
            };
        }
        catch (Exception ex)
        {
            _customNotesFilePath = previousCustomNotesFilePath;
            LoadError = $"Failed to migrate notes: {ex.Message}";

            return new NoteStorageChangeResult
            {
                Succeeded = false,
                PathChanged = false,
                PreviousPath = previousPath,
                CurrentPath = previousPath,
                CurrentNoteCount = _notes.Count,
                ErrorMessage = LoadError
            };
        }
    }

    public void Load()
    {
        LoadError = string.Empty;
        _notes = [];

        try
        {
            _notes = LoadNotesFromPath(NotesFilePath, initializeIfMissing: true);
            _notes = SortNotes(_notes);
        }
        catch (Exception ex)
        {
            LoadError = $"Failed to load notes: {ex.Message}";
            _notes = [];
        }
    }

    public bool SaveNote(string content, out NoteItem savedNote, out string errorMessage)
    {
        savedNote = null;
        errorMessage = string.Empty;

        var trimmedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            errorMessage = "Note content cannot be empty.";
            return false;
        }

        try
        {
            var now = DateTime.UtcNow;
            var note = Normalize(new NoteItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Content = trimmedContent,
                CreatedAt = now,
                UpdatedAt = now,
                IsPinned = false,
                IsArchived = false,
                Tags = ExtractTags(trimmedContent)
            });

            _notes.Insert(0, note);
            PersistNotes();
            savedNote = note;
            LoadError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save note: {ex.Message}";
            LoadError = errorMessage;
            return false;
        }
    }

    public bool UpdateNote(string noteId, string content, out NoteItem updatedNote, out string errorMessage)
    {
        updatedNote = null;
        errorMessage = string.Empty;

        var trimmedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(noteId))
        {
            errorMessage = "Note id cannot be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            errorMessage = "Note content cannot be empty.";
            return false;
        }

        var note = _notes.FirstOrDefault(x => string.Equals(x.Id, noteId, StringComparison.OrdinalIgnoreCase));
        if (note is null)
        {
            errorMessage = "Note not found.";
            return false;
        }

        try
        {
            note.Content = trimmedContent;
            note.Tags = ExtractTags(trimmedContent);
            note.UpdatedAt = DateTime.UtcNow;
            SortNotesInPlace();
            PersistNotes();
            updatedNote = note;
            LoadError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update note: {ex.Message}";
            LoadError = errorMessage;
            return false;
        }
    }

    public bool DeleteNote(string noteId, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(noteId))
        {
            errorMessage = "Note id cannot be empty.";
            return false;
        }

        var note = _notes.FirstOrDefault(x => string.Equals(x.Id, noteId, StringComparison.OrdinalIgnoreCase));
        if (note is null)
        {
            errorMessage = "Note not found.";
            return false;
        }

        try
        {
            _notes.Remove(note);
            PersistNotes();
            LoadError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to delete note: {ex.Message}";
            LoadError = errorMessage;
            return false;
        }
    }

    public bool SetPinned(string noteId, bool isPinned, out NoteItem updatedNote, out string errorMessage)
    {
        updatedNote = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(noteId))
        {
            errorMessage = "Note id cannot be empty.";
            return false;
        }

        var note = _notes.FirstOrDefault(x => string.Equals(x.Id, noteId, StringComparison.OrdinalIgnoreCase));
        if (note is null)
        {
            errorMessage = "Note not found.";
            return false;
        }

        try
        {
            note.IsPinned = isPinned && !note.IsArchived;
            note.UpdatedAt = DateTime.UtcNow;
            SortNotesInPlace();
            PersistNotes();
            updatedNote = note;
            LoadError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update note pin state: {ex.Message}";
            LoadError = errorMessage;
            return false;
        }
    }

    public bool SetArchived(string noteId, bool isArchived, out NoteItem updatedNote, out string errorMessage)
    {
        updatedNote = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(noteId))
        {
            errorMessage = "Note id cannot be empty.";
            return false;
        }

        var note = _notes.FirstOrDefault(x => string.Equals(x.Id, noteId, StringComparison.OrdinalIgnoreCase));
        if (note is null)
        {
            errorMessage = "Note not found.";
            return false;
        }

        try
        {
            note.IsArchived = isArchived;
            if (isArchived)
            {
                note.IsPinned = false;
            }

            note.UpdatedAt = DateTime.UtcNow;
            SortNotesInPlace();
            PersistNotes();
            updatedNote = note;
            LoadError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to update note archive state: {ex.Message}";
            LoadError = errorMessage;
            return false;
        }
    }

    private void InitializeNotesFile(string notesFilePath)
    {
        var samplePath = Path.Combine(_pluginDirectory, SampleNotesFileName);
        if (File.Exists(samplePath))
        {
            File.Copy(samplePath, notesFilePath, overwrite: false);
        }
        else
        {
            File.WriteAllText(notesFilePath, "[]");
        }
    }

    private void PersistNotes()
    {
        PersistNotesToPath(NotesFilePath, _notes);
    }

    private string ResolveNotesFilePath()
    {
        return ResolveNotesFilePath(_customNotesFilePath);
    }

    private string ResolveNotesFilePath(string customNotesFilePath)
    {
        if (!string.IsNullOrWhiteSpace(customNotesFilePath))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(customNotesFilePath));
        }

        return Path.Combine(_defaultStorageDirectory, NotesFileName);
    }

    private string GetNotesDirectoryPath()
    {
        return Path.GetDirectoryName(NotesFilePath) ?? _defaultStorageDirectory;
    }

    private void SortNotesInPlace()
    {
        _notes = SortNotes(_notes);
    }

    private static List<NoteItem> SortNotes(IEnumerable<NoteItem> notes)
    {
        return notes
            .OrderByDescending(note => !note.IsArchived)
            .ThenByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt)
            .ToList();
    }

    private List<NoteItem> LoadNotesFromPath(string notesFilePath, bool initializeIfMissing)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(notesFilePath) ?? _defaultStorageDirectory);

        if (!File.Exists(notesFilePath))
        {
            if (!initializeIfMissing)
            {
                return [];
            }

            InitializeNotesFile(notesFilePath);
        }

        var json = File.ReadAllText(notesFilePath);
        var notes = JsonSerializer.Deserialize<List<NoteItem>>(json, _jsonOptions) ?? [];
        return notes
            .Where(note => !string.IsNullOrWhiteSpace(note.Content))
            .Select(CloneAndNormalize)
            .ToList();
    }

    private void PersistNotesToPath(string notesFilePath, IReadOnlyCollection<NoteItem> notes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(notesFilePath) ?? _defaultStorageDirectory);

        if (!File.Exists(notesFilePath))
        {
            InitializeNotesFile(notesFilePath);
        }

        var json = JsonSerializer.Serialize(SortNotes(CloneNotes(notes)), _jsonOptions);
        File.WriteAllText(notesFilePath, json);
    }

    private static List<NoteItem> MergeNotes(IEnumerable<NoteItem> existingTargetNotes, IEnumerable<NoteItem> sourceNotes)
    {
        var merged = new Dictionary<string, NoteItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var note in existingTargetNotes)
        {
            merged[note.Id] = CloneAndNormalize(note);
        }

        foreach (var note in sourceNotes)
        {
            var candidate = CloneAndNormalize(note);
            if (!merged.TryGetValue(candidate.Id, out var existing) ||
                candidate.UpdatedAt >= existing.UpdatedAt)
            {
                merged[candidate.Id] = candidate;
            }
        }

        return SortNotes(merged.Values);
    }

    private static List<NoteItem> CloneNotes(IEnumerable<NoteItem> notes)
    {
        return notes.Select(CloneAndNormalize).ToList();
    }

    private static NoteItem CloneAndNormalize(NoteItem note)
    {
        return Normalize(new NoteItem
        {
            Id = note.Id,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            IsPinned = note.IsPinned,
            IsArchived = note.IsArchived,
            Tags = note.Tags?.ToList() ?? []
        });
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static NoteItem Normalize(NoteItem note)
    {
        note.Id ??= string.Empty;
        note.Content ??= string.Empty;
        note.Tags ??= [];

        if (string.IsNullOrWhiteSpace(note.Id))
        {
            note.Id = Guid.NewGuid().ToString("N");
        }

        if (note.CreatedAt == default)
        {
            note.CreatedAt = DateTime.UtcNow;
        }

        if (note.UpdatedAt == default)
        {
            note.UpdatedAt = note.CreatedAt;
        }

        note.Tags = note.Tags
            .Select(NormalizeTag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return note;
    }

    private static List<string> ExtractTags(string content)
    {
        return content
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.StartsWith('#') && term.Length > 1)
            .Select(term => NormalizeTag(term[1..]))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeTag(string tag)
    {
        return tag?.Trim().TrimStart('#').ToLowerInvariant() ?? string.Empty;
    }
}
