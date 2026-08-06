using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Creta.Plugin.LocalPromptSearch;

public class Main : IPlugin, ISettingProvider, IContextMenu
{
    internal static PluginInitContext Context { get; private set; } = null!;

    private PromptRepository _repository = null!;
    private Settings _settings = null!;

    public void Init(PluginInitContext context)
    {
        Context = context;
        _settings = context.API.LoadSettingJsonStorage<Settings>();
        _repository = new PromptRepository(context.CurrentPluginMetadata.PluginDirectory);
        _repository.Load(_settings.PromptFilePath);
    }

    public List<Result> Query(Query query)
    {
        if (string.Equals(query.Search?.Trim(), "reload", StringComparison.CurrentCultureIgnoreCase))
        {
            ReloadPromptsFromSettings();

            var subtitle = string.IsNullOrWhiteSpace(_repository.LoadError)
                ? $"已重新加载：{_repository.CurrentFilePath}"
                : _repository.LoadError;

            return
            [
                new Result
                {
                    Title = string.IsNullOrWhiteSpace(_repository.LoadError) ? "已重新加载 Prompt 模板" : "重新加载失败",
                    SubTitle = subtitle,
                    Score = 1200,
                    Action = _ => false
                }
            ];
        }

        if (!string.IsNullOrWhiteSpace(_repository.LoadError))
        {
            return
            [
                new Result
                {
                    Title = "无法加载本地 Prompt 模板",
                    SubTitle = _repository.LoadError,
                    Score = 1000,
                    Action = _ => false
                }
            ];
        }

        var prompts = _repository.GetPrompts();
        if (prompts.Count == 0)
        {
            return
            [
                new Result
                {
                    Title = "未找到可用的 Prompt 模板",
                    SubTitle = "请检查 prompts.json 是否存在并包含有效内容。",
                    Score = 1000,
                    Action = _ => false
                }
            ];
        }

        if (string.IsNullOrWhiteSpace(query.Search))
        {
            return BuildEmptySearchResults(prompts);
        }

        var search = query.Search.Trim();
        var matches = prompts
            .Select(prompt => CreateSearchMatch(prompt, search))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Prompt.Title, StringComparer.CurrentCultureIgnoreCase)
            .Take(12)
            .ToList();

        if (matches.Count == 0)
        {
            return
            [
                new Result
                {
                    Title = $"没有找到与“{search}”相关的 Prompt",
                    SubTitle = "可以尝试更短的关键词，或检查 prompts.json 中是否已有对应模板。",
                    Score = 900,
                    Action = _ => false
                }
            ];
        }

        return
        [
            .. matches.Select(match => CreatePromptResult(
                match.Prompt,
                match.Score,
                "按回车复制 Prompt 到剪贴板。",
                match.TitleHighlightData))
        ];
    }

    public List<Result> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.ContextData is not PromptTemplate prompt)
        {
            return
            [
                new Result
                {
                    Title = "清空最近使用",
                    SubTitle = "移除最近使用记录中的所有 Prompt。",
                    Action = _ =>
                    {
                        ClearRecentPrompts();
                        return true;
                    }
                }
            ];
        }

        return
        [
            new Result
            {
                Title = "复制 Prompt 正文",
                SubTitle = "将当前模板正文复制到剪贴板。",
                Action = _ => CopyPromptContent(prompt)
            },
            new Result
            {
                Title = "复制 Prompt 标题",
                SubTitle = "只复制当前模板的标题。",
                Action = _ => CopyPromptTitle(prompt)
            },
            new Result
            {
                Title = prompt.Favorite ? "取消收藏" : "加入收藏",
                SubTitle = prompt.Favorite ? "将该模板从收藏列表移除。" : "将该模板标记为收藏，便于优先显示。",
                Action = _ =>
                {
                    ToggleFavorite(prompt);
                    return true;
                }
            },
            new Result
            {
                Title = "打开模板文件位置",
                SubTitle = $"打开当前模板文件所在目录：{_repository.CurrentFilePath}",
                Action = _ => OpenPromptFileLocation()
            },
            new Result
            {
                Title = "清空最近使用",
                SubTitle = "移除最近使用记录中的所有 Prompt。",
                Action = _ =>
                {
                    ClearRecentPrompts();
                    return true;
                }
            }
        ];
    }

    public Control CreateSettingPanel()
    {
        return new Views.SettingsControl(_settings, ReloadPromptsFromSettings);
    }

    private List<Result> BuildEmptySearchResults(IReadOnlyList<PromptTemplate> prompts)
    {
        var recentIds = _settings.RecentPromptIds;
        var recentPrompts = recentIds
            .Select(id => prompts.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.CurrentCultureIgnoreCase)))
            .Where(prompt => prompt is not null)
            .Cast<PromptTemplate>()
            .ToList();

        var recentIdSet = new HashSet<string>(recentPrompts.Select(p => p.Id), StringComparer.CurrentCultureIgnoreCase);
        var results = new List<Result>();

        if (recentPrompts.Count > 0)
        {
            results.Add(new Result
            {
                Title = "最近使用的 Prompt",
                SubTitle = "下面优先展示最近复制过的模板。",
                Score = 950,
                Action = _ => false
            });

            results.AddRange(recentPrompts
                .Take(5)
                .Select(prompt => CreatePromptResult(prompt, 900, "最近使用，按回车可再次复制。")));
        }

        results.Add(new Result
        {
            Title = "重新加载 Prompt 模板",
            SubTitle = $"当前文件：{_repository.CurrentFilePath}",
            Score = 800,
            AutoCompleteText = $"{Context.CurrentPluginMetadata.ActionKeywords[0]} reload",
            Action = _ =>
            {
                ReloadPromptsFromSettings();
                return false;
            }
        });

        results.AddRange(prompts
            .Where(p => !recentIdSet.Contains(p.Id))
            .OrderByDescending(p => p.Favorite)
            .ThenBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .Take(8)
            .Select(prompt => CreatePromptResult(prompt, prompt.Favorite ? 700 : 600, "输入关键词继续筛选，按回车可复制 Prompt。")));

        return results;
    }

    private SearchMatch CreateSearchMatch(PromptTemplate prompt, string search)
    {
        var normalizedSearch = search.Trim();
        var title = prompt.Title ?? string.Empty;
        var description = prompt.Description ?? string.Empty;
        var keywordsText = string.Join(" ", prompt.Keywords);
        var tagsText = string.Join(" ", prompt.Tags);

        var titleMatch = Context.API.FuzzySearch(normalizedSearch, title);
        var descriptionMatch = Context.API.FuzzySearch(normalizedSearch, description);
        var keywordsMatch = Context.API.FuzzySearch(normalizedSearch, keywordsText);
        var tagsMatch = Context.API.FuzzySearch(normalizedSearch, tagsText);

        var score =
            titleMatch.Score +
            (descriptionMatch.Score / 3) +
            (keywordsMatch.Score / 2) +
            (tagsMatch.Score / 2);

        if (Contains(title, normalizedSearch))
        {
            score += 300;
        }

        if (StartsWith(title, normalizedSearch))
        {
            score += 300;
        }

        if (string.Equals(title, normalizedSearch, StringComparison.CurrentCultureIgnoreCase))
        {
            score += 500;
        }

        if (Contains(keywordsText, normalizedSearch))
        {
            score += 180;
        }

        if (Contains(tagsText, normalizedSearch))
        {
            score += 150;
        }

        if (Contains(description, normalizedSearch))
        {
            score += 80;
        }

        var terms = normalizedSearch.Split([' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var term in terms)
        {
            if (Contains(title, term))
            {
                score += 120;
            }

            if (Contains(keywordsText, term))
            {
                score += 70;
            }

            if (Contains(tagsText, term))
            {
                score += 60;
            }
        }

        if (prompt.Favorite)
        {
            score += 30;
        }

        var recentIndex = _settings.RecentPromptIds.FindIndex(id =>
            string.Equals(id, prompt.Id, StringComparison.CurrentCultureIgnoreCase));
        if (recentIndex >= 0)
        {
            score += 140 - (recentIndex * 10);
        }

        return new SearchMatch(prompt, score, titleMatch.Score > 0 ? titleMatch.MatchData : []);
    }

    private Result CreatePromptResult(PromptTemplate prompt, int score, string suffix, List<int>? titleHighlightData = null)
    {
        return new Result
        {
            Title = prompt.Title,
            SubTitle = BuildSubtitle(prompt, suffix),
            Score = score,
            TitleHighlightData = titleHighlightData ?? [],
            AutoCompleteText = $"{Context.CurrentPluginMetadata.ActionKeywords[0]} {prompt.Title}",
            CopyText = prompt.Content,
            ContextData = prompt,
            Action = _ => CopyPromptContent(prompt)
        };
    }

    private bool CopyPromptContent(PromptTemplate prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt.Content))
        {
            Context.API.ShowMsgError("该 Prompt 内容为空", $"模板“{prompt.Title}”没有可复制的正文。");
            return false;
        }

        try
        {
            Context.API.CopyToClipboard(prompt.Content, showDefaultNotification: false);
            Context.API.RestorePreviousForegroundWindow(_settings.PasteAfterCopy);
            RegisterRecentPrompt(prompt.Id);
            Context.API.ShowMsg("已复制 Prompt", $"“{prompt.Title}” 已复制到剪贴板。");
            return true;
        }
        catch (Exception ex)
        {
            Context.API.ShowMsgError("复制 Prompt 失败", ex.Message);
            return false;
        }
    }

    private bool CopyPromptTitle(PromptTemplate prompt)
    {
        try
        {
            Context.API.CopyToClipboard(prompt.Title, showDefaultNotification: false);
            Context.API.RestorePreviousForegroundWindow(_settings.PasteAfterCopy);
            Context.API.ShowMsg("已复制标题", $"“{prompt.Title}”标题已复制到剪贴板。");
            return true;
        }
        catch (Exception ex)
        {
            Context.API.ShowMsgError("复制标题失败", ex.Message);
            return false;
        }
    }

    private void ToggleFavorite(PromptTemplate prompt)
    {
        prompt.Favorite = !prompt.Favorite;
        if (_repository.Save())
        {
            Context.API.ShowMsg(
                prompt.Favorite ? "已加入收藏" : "已取消收藏",
                $"“{prompt.Title}”收藏状态已写入 prompts.json。");
        }
        else
        {
            var reason = _repository.CanPersistFavorites
                ? "当前模板文件写入失败，收藏仅在本次运行中生效。"
                : "当前使用的不是 prompts.json，收藏暂未持久化。";
            Context.API.ShowMsg(
                prompt.Favorite ? "已加入收藏" : "已取消收藏",
                reason);
        }

        Context.API.ReQuery();
    }

    private bool OpenPromptFileLocation()
    {
        try
        {
            var pathToOpen = Path.GetDirectoryName(_repository.CurrentFilePath);
            if (string.IsNullOrWhiteSpace(pathToOpen))
            {
                pathToOpen = _repository.CurrentFilePath;
            }

            Context.API.OpenDirectory(pathToOpen);
            return true;
        }
        catch (Exception ex)
        {
            Context.API.ShowMsgError("打开模板文件位置失败", ex.Message);
            return false;
        }
    }

    private void ClearRecentPrompts()
    {
        _settings.RecentPromptIds.Clear();
        Context.API.SaveSettingJsonStorage<Settings>();
        Context.API.ShowMsg("已清空最近使用", "最近使用的 Prompt 记录已移除。");
        Context.API.ReQuery();
    }

    private void RegisterRecentPrompt(string promptId)
    {
        if (string.IsNullOrWhiteSpace(promptId))
        {
            return;
        }

        _settings.RecentPromptIds.RemoveAll(id => string.Equals(id, promptId, StringComparison.CurrentCultureIgnoreCase));
        _settings.RecentPromptIds.Insert(0, promptId);

        const int maxRecentCount = 20;
        if (_settings.RecentPromptIds.Count > maxRecentCount)
        {
            _settings.RecentPromptIds = _settings.RecentPromptIds.Take(maxRecentCount).ToList();
        }

        Context.API.SaveSettingJsonStorage<Settings>();
    }

    private void ReloadPromptsFromSettings()
    {
        _repository.Load(_settings.PromptFilePath);
        Context.API.SaveSettingJsonStorage<Settings>();
    }

    private static string BuildSubtitle(PromptTemplate prompt, string suffix)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(prompt.Description))
        {
            details.Add(prompt.Description);
        }

        if (!string.IsNullOrWhiteSpace(prompt.Category))
        {
            details.Add(prompt.Category);
        }

        if (details.Count == 0)
        {
            return suffix;
        }

        return $"{string.Join(" · ", details)} · {suffix}";
    }

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.CurrentCultureIgnoreCase);

    private static bool StartsWith(string source, string value) =>
        source.StartsWith(value, StringComparison.CurrentCultureIgnoreCase);

    private sealed record SearchMatch(PromptTemplate Prompt, int Score, List<int> TitleHighlightData);
}
