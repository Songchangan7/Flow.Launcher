using System.Collections.Generic;
using System.Linq;
using Creta.Plugin.BrowserTab.Commands;
using Flow.Launcher.Plugin.SharedModels;

namespace Creta.Plugin.BrowserTab;

public class Main : IPlugin, IPluginI18n
{
    private const string IcoPath = "Images/bookmark.png";

    internal static PluginInitContext Context { get; private set; }

    public void Init(PluginInitContext context)
    {
        Context = context;
    }

    public List<Result> Query(Query query)
    {
        var search = query.Search.Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            return [];
        }

        return ChromeTabLoader.LoadAllTabs()
            .Select(tab =>
            {
                var match = MatchTab(search, tab);
                return new Result
                {
                    Title = tab.Title,
                    SubTitle = string.IsNullOrWhiteSpace(tab.Url)
                        ? Localize.creta_plugin_browsertab_jump_to_tab()
                        : $"{Localize.creta_plugin_browsertab_jump_to_tab()} - {tab.Url}",
                    IcoPath = IcoPath,
                    Score = match.Score + 10,
                    TitleHighlightData = match.MatchData,
                    Action = _ => ChromeTabActivator.Activate(tab)
                };
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ToList();
    }

    public string GetTranslatedPluginTitle()
    {
        return Localize.creta_plugin_browsertab_plugin_name();
    }

    public string GetTranslatedPluginDescription()
    {
        return Localize.creta_plugin_browsertab_plugin_description();
    }

    private static MatchResult MatchTab(string search, Models.ChromeTab tab)
    {
        var titleMatch = Context.API.FuzzySearch(search, tab.Title);
        if (titleMatch.IsSearchPrecisionScoreMet())
        {
            return titleMatch;
        }

        if (!string.IsNullOrWhiteSpace(tab.Url))
        {
            var urlMatch = Context.API.FuzzySearch(search, tab.Url);
            if (urlMatch.IsSearchPrecisionScoreMet())
            {
                return urlMatch;
            }
        }

        return titleMatch;
    }
}
