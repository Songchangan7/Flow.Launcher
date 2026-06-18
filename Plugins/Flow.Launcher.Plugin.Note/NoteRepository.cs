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

    public string NotesFilePath => ResolveNotesFilePath();

    public string LoadError { get; private set; } = string.Empty;

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

    public void UpdateStoragePath(string customNotesFilePath)
    {
        _customNotesFilePath = customNotesFilePath?.Trim() ?? string.Empty;
    }

    public void Load()
    {
        LoadError = string.Empty;
        _notes = [];

        try
        {
            Directory.CreateDirectory(GetNotesDirectoryPath());

            if (!File.Exists(NotesFilePath))
            {
                InitializeNotesFile();
            }

            var json = File.ReadAllText(NotesFilePath);
            var notes = JsonSerializer.Deserialize<List<NoteItem>>(json, _jsonOptions) ?? [];
            _notes = notes
                .Where(note => !string.IsNullOrWhiteSpace(note.Content))
                .Select(Normalize)
                .ToList();
            SortNotesInPlace();
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

    private void InitializeNotesFile()
    {
        var samplePath = Path.Combine(_pluginDirectory, SampleNotesFileName);
        if (File.Exists(samplePath))
        {
            File.Copy(samplePath, NotesFilePath, overwrite: false);
        }
        else
        {
            File.WriteAllText(NotesFilePath, "[]");
        }
    }

    private void PersistNotes()
    {
        Directory.CreateDirectory(GetNotesDirectoryPath());

        if (!File.Exists(NotesFilePath))
        {
            InitializeNotesFile();
        }

        var json = JsonSerializer.Serialize(_notes, _jsonOptions);
        File.WriteAllText(NotesFilePath, json);
    }

    private string ResolveNotesFilePath()
    {
        if (!string.IsNullOrWhiteSpace(_customNotesFilePath))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(_customNotesFilePath));
        }

        return Path.Combine(_defaultStorageDirectory, NotesFileName);
    }

    private string GetNotesDirectoryPath()
    {
        return Path.GetDirectoryName(NotesFilePath) ?? _defaultStorageDirectory;
    }

    private void SortNotesInPlace()
    {
        _notes = _notes
            .OrderByDescending(note => !note.IsArchived)
            .ThenByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt)
            .ToList();
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
