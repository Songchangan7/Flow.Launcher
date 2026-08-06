using System;

namespace Creta.Plugin.Note;

internal static class NoteSpecialViewKeywords
{
    internal const string All = "all";
    internal const string Pinned = "pinned";
    internal const string Today = "today";
    internal const string Recent = "recent";
    internal const string Week = "week";
    internal const string Archived = "archived";
    internal const string TagPrefix = "tag:";

    private static readonly string[] AllAliases = [All, "a"];
    private static readonly string[] PinnedAliases = [Pinned, "p"];
    private static readonly string[] TodayAliases = [Today, "t"];
    private static readonly string[] RecentAliases = [Recent, "r"];
    private static readonly string[] WeekAliases = [Week, "w"];
    private static readonly string[] ArchivedAliases = [Archived, "ar"];

    internal static bool IsAll(string input) => Matches(input, AllAliases);
    internal static bool IsPinned(string input) => Matches(input, PinnedAliases);
    internal static bool IsToday(string input) => Matches(input, TodayAliases);
    internal static bool IsRecent(string input) => Matches(input, RecentAliases);
    internal static bool IsWeek(string input) => Matches(input, WeekAliases);
    internal static bool IsArchived(string input) => Matches(input, ArchivedAliases);
    internal static bool IsTagView(string input) => input?.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase) == true;

    internal static string BuildTagViewKey(string tag) => $"{TagPrefix}{tag}";

    private static bool Matches(string input, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (string.Equals(input, keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
