using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Flow.Launcher.Plugin.Note;

namespace Flow.Launcher.Test.Plugins;

public class NoteRepositoryTests
{
    private string _testRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FlowLauncher.NoteTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Test]
    public void GivenNoNotesFileWhenLoadThenCreatesNotesFileFromSample()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "notes.sample.json"),
            """
            [
              {
                "Id": "sample",
                "Content": "hello",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);

        repository.Load();

        ClassicAssert.IsTrue(File.Exists(Path.Combine(storageDirectory, "notes.json")));
        ClassicAssert.AreEqual(string.Empty, repository.LoadError);
        ClassicAssert.AreEqual(1, repository.Notes.Count);
        ClassicAssert.AreEqual("hello", repository.Notes[0].Content);
    }

    [Test]
    public void GivenInvalidJsonWhenLoadThenReturnsLoadError()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"), "{invalid json");

        var repository = new NoteRepository(pluginDirectory, storageDirectory);

        repository.Load();

        ClassicAssert.IsNotEmpty(repository.LoadError);
        ClassicAssert.AreEqual(0, repository.Notes.Count);
    }

    [Test]
    public void GivenValidContentWhenSaveNoteThenPersistsNewNote()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "notes.sample.json"), "[]");

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.SaveNote("  new idea  ", out var savedNote, out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.IsNotNull(savedNote);
        ClassicAssert.AreEqual("new idea", savedNote.Content);
        ClassicAssert.AreEqual(1, repository.Notes.Count);
        ClassicAssert.AreEqual("new idea", repository.Notes[0].Content);
        ClassicAssert.IsTrue(File.ReadAllText(repository.NotesFilePath).Contains("new idea"));
    }

    [Test]
    public void GivenWhitespaceContentWhenSaveNoteThenReturnsFalse()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "notes.sample.json"), "[]");

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.SaveNote("   ", out var savedNote, out var errorMessage);

        ClassicAssert.IsFalse(result);
        ClassicAssert.IsNull(savedNote);
        ClassicAssert.AreEqual("Note content cannot be empty.", errorMessage);
        ClassicAssert.AreEqual(0, repository.Notes.Count);
    }

    [Test]
    public void GivenPinnedAndRecentNotesWhenGetRecentNotesThenPinnedArePrioritized()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "old-pinned",
                "Content": "pinned note",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": true
              },
              {
                "Id": "new-unpinned",
                "Content": "new note",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": false
              },
              {
                "Id": "older-unpinned",
                "Content": "older note",
                "CreatedAt": "2026-06-15T00:00:00Z",
                "UpdatedAt": "2026-06-15T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);

        repository.Load();
        var recentNotes = repository.GetRecentNotes(2);

        ClassicAssert.AreEqual(2, recentNotes.Count);
        ClassicAssert.AreEqual("pinned note", recentNotes[0].Content);
        ClassicAssert.AreEqual("new note", recentNotes[1].Content);
    }

    [Test]
    public void GivenPinnedAndUnpinnedNotesWhenLoadThenPinnedAreSortedFirst()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "unpinned-newer",
                "Content": "newer unpinned",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": false
              },
              {
                "Id": "pinned-older",
                "Content": "older pinned",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": true
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);

        repository.Load();

        ClassicAssert.AreEqual(2, repository.Notes.Count);
        ClassicAssert.AreEqual("older pinned", repository.Notes[0].Content);
        ClassicAssert.AreEqual("newer unpinned", repository.Notes[1].Content);
    }

    [Test]
    public void GivenExistingNoteWhenUpdateNoteThenReplacesContentAndPersists()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "before",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.UpdateNote("note-1", " after ", out var updatedNote, out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.IsNotNull(updatedNote);
        ClassicAssert.AreEqual("after", updatedNote.Content);
        ClassicAssert.IsTrue(File.ReadAllText(repository.NotesFilePath).Contains("after"));
    }

    [Test]
    public void GivenExistingNoteWhenDeleteNoteThenRemovesIt()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "to delete",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.DeleteNote("note-1", out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.AreEqual(0, repository.Notes.Count);
        ClassicAssert.IsFalse(File.ReadAllText(repository.NotesFilePath).Contains("to delete"));
    }

    [Test]
    public void GivenExistingNoteWhenSetPinnedThenMovesItToPinnedState()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "pin me",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.SetPinned("note-1", true, out var updatedNote, out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.IsNotNull(updatedNote);
        ClassicAssert.IsTrue(updatedNote.IsPinned);
        ClassicAssert.IsTrue(File.ReadAllText(repository.NotesFilePath).Contains("\"IsPinned\": true"));
    }

    [Test]
    public void GivenSearchTextWhenSearchNotesThenReturnsMatchingNotes()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "build quick note plugin",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              },
              {
                "Id": "note-2",
                "Content": "something else",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": true
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var matches = repository.SearchNotes("quick", 10);

        ClassicAssert.AreEqual(1, matches.Count);
        ClassicAssert.AreEqual("build quick note plugin", matches[0].Content);
    }

    [Test]
    public void GivenSearchNotesWhenExactAndPinnedMatchesExistThenRankingMatchesScorerRules()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "contains-pinned",
                "Content": "daily quick note summary",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": true
              },
              {
                "Id": "exact",
                "Content": "quick note",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var matches = repository.SearchNotes("quick note", 10);

        ClassicAssert.AreEqual(2, matches.Count);
        ClassicAssert.AreEqual("quick note", matches[0].Content);
        ClassicAssert.AreEqual("daily quick note summary", matches[1].Content);
    }

    [Test]
    public void GivenDateWhenGetNotesCreatedOnThenReturnsSameDayNotes()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "today note",
                "CreatedAt": "2026-06-16T12:00:00Z",
                "UpdatedAt": "2026-06-16T12:00:00Z",
                "IsPinned": false
              },
              {
                "Id": "note-2",
                "Content": "other day",
                "CreatedAt": "2026-06-15T12:00:00Z",
                "UpdatedAt": "2026-06-15T12:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var notes = repository.GetNotesCreatedOn(new DateTime(2026, 6, 16), 10);

        ClassicAssert.AreEqual(1, notes.Count);
        ClassicAssert.AreEqual("today note", notes[0].Content);
    }

    [Test]
    public void GivenDifferentMatchStrengthsWhenScoringThenExactMatchRanksHighest()
    {
        var exact = new NoteItem
        {
            Id = "exact",
            Content = "quick note",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };
        var prefix = new NoteItem
        {
            Id = "prefix",
            Content = "quick note plugin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };
        var contains = new NoteItem
        {
            Id = "contains",
            Content = "build a quick note plugin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };

        var exactMatch = NoteSearchScorer.Match(exact, "quick note");
        var prefixMatch = NoteSearchScorer.Match(prefix, "quick note");
        var containsMatch = NoteSearchScorer.Match(contains, "quick note");

        ClassicAssert.IsNotNull(exactMatch);
        ClassicAssert.IsNotNull(prefixMatch);
        ClassicAssert.IsNotNull(containsMatch);
        ClassicAssert.Greater(exactMatch!.Score, prefixMatch!.Score);
        ClassicAssert.Greater(prefixMatch.Score, containsMatch!.Score);
    }

    [Test]
    public void GivenPinnedAndUnpinnedSameMatchWhenScoringThenPinnedRanksHigher()
    {
        var pinned = new NoteItem
        {
            Id = "pinned",
            Content = "project roadmap",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = true
        };
        var normal = new NoteItem
        {
            Id = "normal",
            Content = "project roadmap",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };

        var pinnedMatch = NoteSearchScorer.Match(pinned, "project roadmap");
        var normalMatch = NoteSearchScorer.Match(normal, "project roadmap");

        ClassicAssert.IsNotNull(pinnedMatch);
        ClassicAssert.IsNotNull(normalMatch);
        ClassicAssert.Greater(pinnedMatch!.Score, normalMatch!.Score);
    }

    [Test]
    public void GivenExactOrPrefixMatchesWhenScoringThenTheyAreMarkedHighSimilarity()
    {
        var exact = new NoteItem
        {
            Id = "exact",
            Content = "weekly sync",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };
        var prefix = new NoteItem
        {
            Id = "prefix",
            Content = "weekly sync notes",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };
        var broad = new NoteItem
        {
            Id = "broad",
            Content = "notes for the weekly product sync meeting",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsPinned = false
        };

        var exactMatch = NoteSearchScorer.Match(exact, "weekly sync");
        var prefixMatch = NoteSearchScorer.Match(prefix, "weekly sync");
        var broadMatch = NoteSearchScorer.Match(broad, "sync");

        ClassicAssert.IsTrue(exactMatch!.IsHighSimilarity);
        ClassicAssert.IsTrue(prefixMatch!.IsHighSimilarity);
        ClassicAssert.IsFalse(broadMatch!.IsExactMatch);
    }

    [Test]
    public void GivenAllNotesWhenGetAllNotesThenReturnsCurrentSortedOrder()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "regular note",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              },
              {
                "Id": "note-2",
                "Content": "pinned note",
                "CreatedAt": "2026-06-15T00:00:00Z",
                "UpdatedAt": "2026-06-15T00:00:00Z",
                "IsPinned": true
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var notes = repository.GetAllNotes(10);

        ClassicAssert.AreEqual(2, notes.Count);
        ClassicAssert.AreEqual("pinned note", notes[0].Content);
        ClassicAssert.AreEqual("regular note", notes[1].Content);
    }

    [Test]
    public void GivenTaggedNoteWhenSaveNoteThenExtractsTags()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "notes.sample.json"), "[]");

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.SaveNote("ship plugin #flow #note", out var savedNote, out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.AreEqual(2, savedNote.Tags.Count);
        ClassicAssert.IsTrue(savedNote.Tags.Contains("flow"));
        ClassicAssert.IsTrue(savedNote.Tags.Contains("note"));
    }

    [Test]
    public void GivenArchivedNoteWhenGetRecentNotesThenItIsExcluded()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "archived note",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false,
                "IsArchived": true,
                "Tags": []
              },
              {
                "Id": "note-2",
                "Content": "active note",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": false,
                "IsArchived": false,
                "Tags": []
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var notes = repository.GetRecentNotes(10);

        ClassicAssert.AreEqual(1, notes.Count);
        ClassicAssert.AreEqual("active note", notes[0].Content);
    }

    [Test]
    public void GivenArchivedStateWhenSetArchivedThenMovesNoteBetweenViews()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "archive me #work",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": true,
                "IsArchived": false,
                "Tags": [ "work" ]
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.SetArchived("note-1", true, out var updatedNote, out var errorMessage);

        ClassicAssert.IsTrue(result);
        ClassicAssert.AreEqual(string.Empty, errorMessage);
        ClassicAssert.IsTrue(updatedNote.IsArchived);
        ClassicAssert.IsFalse(updatedNote.IsPinned);
        ClassicAssert.AreEqual(0, repository.GetRecentNotes(10).Count);
        ClassicAssert.AreEqual(1, repository.GetArchivedNotes(10).Count);
    }

    [Test]
    public void GivenTagFilterWhenGetNotesByTagThenReturnsMatchingActiveNotes()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "active work note #work",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false,
                "IsArchived": false,
                "Tags": [ "work" ]
              },
              {
                "Id": "note-2",
                "Content": "archived work note #work",
                "CreatedAt": "2026-06-15T00:00:00Z",
                "UpdatedAt": "2026-06-15T00:00:00Z",
                "IsPinned": false,
                "IsArchived": true,
                "Tags": [ "work" ]
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var notes = repository.GetNotesByTag("work", 10);

        ClassicAssert.AreEqual(1, notes.Count);
        ClassicAssert.AreEqual("active work note #work", notes[0].Content);
    }

    [Test]
    public void GivenStoragePathChangesWhenUpdateStoragePathThenMigratesExistingNotesToNewPath()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        var customDirectory = Path.Combine(_testRoot, "custom");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "migrate me",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.UpdateStoragePath(Path.Combine(customDirectory, "notes.json"));

        ClassicAssert.IsTrue(result.Succeeded);
        ClassicAssert.IsTrue(result.PathChanged);
        ClassicAssert.IsFalse(result.NotesMerged);
        ClassicAssert.AreEqual(1, result.MigratedNoteCount);
        ClassicAssert.AreEqual(1, repository.Notes.Count);
        ClassicAssert.AreEqual("migrate me", repository.Notes[0].Content);
        ClassicAssert.IsTrue(File.Exists(Path.Combine(customDirectory, "notes.json")));
        ClassicAssert.IsTrue(File.ReadAllText(Path.Combine(customDirectory, "notes.json")).Contains("migrate me"));
    }

    [Test]
    public void GivenTargetPathHasNotesWhenUpdateStoragePathThenMergesSourceAndTargetNotes()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        var customDirectory = Path.Combine(_testRoot, "custom");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        Directory.CreateDirectory(customDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "source-note",
                "Content": "source note",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);
        File.WriteAllText(Path.Combine(customDirectory, "notes.json"),
            """
            [
              {
                "Id": "target-note",
                "Content": "target note",
                "CreatedAt": "2026-06-17T00:00:00Z",
                "UpdatedAt": "2026-06-17T00:00:00Z",
                "IsPinned": true
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.UpdateStoragePath(Path.Combine(customDirectory, "notes.json"));

        ClassicAssert.IsTrue(result.Succeeded);
        ClassicAssert.IsTrue(result.PathChanged);
        ClassicAssert.IsTrue(result.NotesMerged);
        ClassicAssert.AreEqual(1, result.MigratedNoteCount);
        ClassicAssert.AreEqual(1, result.ExistingTargetNoteCount);
        ClassicAssert.AreEqual(2, repository.Notes.Count);
        ClassicAssert.IsTrue(File.ReadAllText(Path.Combine(customDirectory, "notes.json")).Contains("source note"));
        ClassicAssert.IsTrue(File.ReadAllText(Path.Combine(customDirectory, "notes.json")).Contains("target note"));
    }

    [Test]
    public void GivenStoragePathUnchangedWhenUpdateStoragePathThenDoesNotMigrateAgain()
    {
        var pluginDirectory = Path.Combine(_testRoot, "plugin");
        var storageDirectory = Path.Combine(_testRoot, "storage");
        Directory.CreateDirectory(pluginDirectory);
        Directory.CreateDirectory(storageDirectory);
        File.WriteAllText(Path.Combine(storageDirectory, "notes.json"),
            """
            [
              {
                "Id": "note-1",
                "Content": "stay here",
                "CreatedAt": "2026-06-16T00:00:00Z",
                "UpdatedAt": "2026-06-16T00:00:00Z",
                "IsPinned": false
              }
            ]
            """);

        var repository = new NoteRepository(pluginDirectory, storageDirectory);
        repository.Load();

        var result = repository.UpdateStoragePath(string.Empty);

        ClassicAssert.IsTrue(result.Succeeded);
        ClassicAssert.IsFalse(result.PathChanged);
        ClassicAssert.AreEqual(1, result.CurrentNoteCount);
        ClassicAssert.AreEqual("stay here", repository.Notes[0].Content);
        ClassicAssert.IsTrue(File.ReadAllText(Path.Combine(storageDirectory, "notes.json")).Contains("stay here"));
    }
}
