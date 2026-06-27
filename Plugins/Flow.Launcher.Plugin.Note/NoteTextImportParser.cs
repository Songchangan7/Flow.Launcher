using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.Note;

public static class NoteTextImportParser
{
    private static readonly Regex DashSeparatorRegex = new(@"\r?\n-{40,}\r?\n", RegexOptions.Compiled);
    private static readonly Regex MarkdownHeadingSplitRegex = new(@"(?<=\r?\n)(?=## \d+\.)", RegexOptions.Compiled);
    private static readonly Regex MetadataPlainRegex = new(
        @"^((Created|Updated|Tags)\s*[:：]|创建时间|更新时间|标签)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex MetadataBulletRegex = new(
        @"^-\s*\*\*((Created|Updated|Tags)|创建时间|更新时间|标签)\*\*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Split(string content, NotesTextImportSplitMode mode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        return mode switch
        {
            NotesTextImportSplitMode.DashSeparator => SplitAndNormalize(DashSeparatorRegex.Split(content)),
            NotesTextImportSplitMode.BlankLine => SplitAndNormalize(Regex.Split(content, @"\r?\n\s*\r?\n")),
            NotesTextImportSplitMode.MarkdownHeading => SplitAndNormalize(MarkdownHeadingSplitRegex.Split(content)),
            _ => [NormalizeChunk(content)]
        };
    }

    public static string NormalizeChunk(string chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk))
        {
            return string.Empty;
        }

        var lines = chunk
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        if (lines.Count > 0 && lines[0].TrimStart().StartsWith("## ", StringComparison.Ordinal))
        {
            lines.RemoveAt(0);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            {
                lines.RemoveAt(0);
            }
        }

        while (lines.Count > 0 && IsMetadataLine(lines[0]))
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    public static NotesTextImportSplitMode SuggestSplitMode(string content, bool isMarkdown)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return NotesTextImportSplitMode.EntireFile;
        }

        if (isMarkdown && Regex.IsMatch(content, @"## \d+\.", RegexOptions.CultureInvariant))
        {
            return NotesTextImportSplitMode.MarkdownHeading;
        }

        if (DashSeparatorRegex.IsMatch(content))
        {
            return NotesTextImportSplitMode.DashSeparator;
        }

        if (Regex.IsMatch(content, @"\r?\n\s*\r?\n"))
        {
            return NotesTextImportSplitMode.BlankLine;
        }

        return NotesTextImportSplitMode.EntireFile;
    }

    private static IReadOnlyList<string> SplitAndNormalize(IEnumerable<string> chunks)
    {
        return chunks
            .Select(NormalizeChunk)
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .ToList();
    }

    private static bool IsMetadataLine(string line)
    {
        var trimmed = line.Trim();
        return MetadataPlainRegex.IsMatch(trimmed) || MetadataBulletRegex.IsMatch(trimmed);
    }
}
