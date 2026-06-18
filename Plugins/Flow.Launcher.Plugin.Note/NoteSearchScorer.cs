using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.Note;

public static class NoteSearchScorer
{
    public static NoteSearchMatch Match(NoteItem note, string search)
    {
        if (note is null || string.IsNullOrWhiteSpace(note.Content) || string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var content = note.Content.Trim();
        var normalizedContent = content.ToLowerInvariant();
        var normalizedSearch = search.Trim().ToLowerInvariant();
        var terms = normalizedSearch
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var score = 0;
        var highlightData = new SortedSet<int>();
        var isExactMatch = false;
        var isPrefixMatch = false;
        var isPhraseMatch = false;

        if (normalizedContent == normalizedSearch)
        {
            isExactMatch = true;
            score += 10000;
            AddHighlightRange(highlightData, FindFirstMatchIndex(content, search), search.Length);
        }
        else if (normalizedContent.StartsWith(normalizedSearch, StringComparison.OrdinalIgnoreCase))
        {
            isPrefixMatch = true;
            score += 8000;
            AddHighlightRange(highlightData, 0, search.Length);
        }
        else
        {
            var fullPhraseIndex = normalizedContent.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase);
            if (fullPhraseIndex >= 0)
            {
                isPhraseMatch = true;
                score += 6000;
                AddHighlightRange(highlightData, fullPhraseIndex, search.Length);
            }
        }

        var allTermsPresent = terms.Length > 0 && terms.All(term => normalizedContent.Contains(term, StringComparison.OrdinalIgnoreCase));
        var fullWordTermMatches = 0;

        foreach (var term in terms)
        {
            var index = normalizedContent.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            score += 200;
            AddHighlightRange(highlightData, index, term.Length);

            if (IsWholeWord(normalizedContent, index, term.Length))
            {
                score += 500;
                fullWordTermMatches += 1;
            }

            if (index == 0)
            {
                score += 300;
            }
        }

        if (fullWordTermMatches == terms.Length && terms.Length > 0)
        {
            score += 1800;
        }
        else if (allTermsPresent)
        {
            score += 1200;
        }

        if (score <= 0)
        {
            return null;
        }

        score += note.IsPinned ? 450 : 0;
        score += GetRecencyScore(note.UpdatedAt);

        return new NoteSearchMatch
        {
            Note = note,
            Score = score,
            HighlightData = [.. highlightData],
            IsExactMatch = isExactMatch,
            IsPrefixMatch = isPrefixMatch,
            IsPhraseMatch = isPhraseMatch,
            IsHighSimilarity = isExactMatch || isPrefixMatch || isPhraseMatch || fullWordTermMatches == terms.Length
        };
    }

    private static int GetRecencyScore(DateTime updatedAtUtc)
    {
        var age = DateTime.UtcNow - updatedAtUtc;
        if (age.TotalDays <= 1)
        {
            return 180;
        }

        if (age.TotalDays <= 7)
        {
            return 120;
        }

        if (age.TotalDays <= 30)
        {
            return 60;
        }

        return 0;
    }

    private static int FindFirstMatchIndex(string content, string search)
    {
        return content.IndexOf(search, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddHighlightRange(ISet<int> indices, int start, int length)
    {
        if (start < 0 || length <= 0)
        {
            return;
        }

        for (var index = start; index < start + length; index++)
        {
            indices.Add(index);
        }
    }

    private static bool IsWholeWord(string text, int start, int length)
    {
        var leftBoundary = start == 0 || !char.IsLetterOrDigit(text[start - 1]);
        var rightIndex = start + length;
        var rightBoundary = rightIndex >= text.Length || !char.IsLetterOrDigit(text[rightIndex]);
        return leftBoundary && rightBoundary;
    }
}
