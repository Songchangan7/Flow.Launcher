using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using Flow.Launcher.Plugin.BrowserBookmark.Commands;
using Flow.Launcher.Plugin.BrowserBookmark.Models;
using Flow.Launcher.Plugin.BrowserBookmark.Views;
using Flow.Launcher.Plugin.SharedCommands;
using Flow.Launcher.Plugin.SharedModels;

namespace Flow.Launcher.Plugin.BrowserBookmark;

public class Main : ISettingProvider, IPlugin, IReloadable, IPluginI18n, IContextMenu, IDisposable
{
    private static readonly string ClassName = nameof(Main);

    internal static string _faviconCacheDir;

    internal static PluginInitContext Context { get; set; }

    internal static Settings _settings;

    private static List<Bookmark> _cachedBookmarks = new();

    private static bool _initialized = false;

    private static string _currentQuery = string.Empty;
    
    public void Init(PluginInitContext context)
    {
        Context = context;

        _settings = context.API.LoadSettingJsonStorage<Settings>();

        _faviconCacheDir = Path.Combine(
            context.CurrentPluginMetadata.PluginCacheDirectoryPath,
            "FaviconCache");
        
        try
        {
            if (Directory.Exists(_faviconCacheDir))
            {
                var files = Directory.GetFiles(_faviconCacheDir);
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file);
                    if (extension is ".db-shm" or ".db-wal" or ".sqlite-shm" or ".sqlite-wal")
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Context.API.LogException(ClassName, "Failed to clean up orphaned cache files.", e);
        }

        LoadBookmarksIfEnabled();
    }

    private static void LoadBookmarksIfEnabled()
    {
        if (Context.CurrentPluginMetadata.Disabled)
        {
            // Don't load or monitor files if disabled
            return;
        }

        // Validate the cache directory before loading all bookmarks because Flow needs this directory to storage favicons
        FilesFolders.ValidateDirectory(_faviconCacheDir);

        _cachedBookmarks = BookmarkLoader.LoadAllBookmarks(_settings);
        _ = MonitorRefreshQueueAsync();
        _initialized = true;
    }

    public List<Result> Query(Query query)
    {
        // For when the plugin being previously disabled and is now re-enabled
        if (!_initialized)
        {
            LoadBookmarksIfEnabled();
        }

        string param = query.Search.TrimStart();
        _currentQuery = param;

        // Should top results be returned? (true if no search parameters have been passed)
        var topResults = string.IsNullOrEmpty(param);

        if (!topResults)
        {
            var bookmarkResults = _cachedBookmarks
                .Select(CreateBookmarkResult);

            var chromeTabResults = ChromeTabLoader.LoadAllTabs()
                .Select(CreateChromeTabResult);

            return bookmarkResults
                .Concat(chromeTabResults)
                .OrderByDescending(r => r.Score)
                .Where(r => r.Score > 0)
                .ToList();
        }
        else
        {
            return _cachedBookmarks
                .Select(CreateBookmarkTopResult)
                .ToList();
        }
    }

    private static Result CreateBookmarkResult(Bookmark bookmark)
    {
        var match = BookmarkLoader.MatchProgram(bookmark, _currentQuery);

        return new Result
        {
            Title = bookmark.Name,
            SubTitle = bookmark.Url,
            IcoPath = !string.IsNullOrEmpty(bookmark.FaviconPath) && File.Exists(bookmark.FaviconPath)
                ? bookmark.FaviconPath
                : @"Images\bookmark.png",
            Score = match.Score,
            TitleHighlightData = match.MatchData,
            Action = _ =>
            {
                Context.API.OpenUrl(bookmark.Url);

                return true;
            },
            ContextData = new BookmarkAttributes { Url = bookmark.Url }
        };
    }

    private static Result CreateBookmarkTopResult(Bookmark bookmark)
    {
        return new Result
        {
            Title = bookmark.Name,
            SubTitle = bookmark.Url,
            IcoPath = !string.IsNullOrEmpty(bookmark.FaviconPath) && File.Exists(bookmark.FaviconPath)
                ? bookmark.FaviconPath
                : @"Images\bookmark.png",
            Score = 5,
            Action = _ =>
            {
                Context.API.OpenUrl(bookmark.Url);
                return true;
            },
            ContextData = new BookmarkAttributes { Url = bookmark.Url }
        };
    }

    private static Result CreateChromeTabResult(ChromeTab tab)
    {
        var match = MatchChromeTab(tab, _currentQuery);

        return new Result
        {
            Title = tab.Title,
            SubTitle = "Jump to open Chrome tab",
            IcoPath = @"Images\bookmark.png",
            Score = match.Score + 10,
            TitleHighlightData = match.MatchData,
            Action = _ => ChromeTabActivator.Activate(tab),
            ContextData = new ChromeTabAttributes
            {
                WindowHandle = tab.WindowHandle,
                TabIndex = tab.TabIndex
            }
        };
    }

    private static MatchResult MatchChromeTab(ChromeTab tab, string queryString)
    {
        return Context.API.FuzzySearch(queryString, tab.Title);
    }

    private static readonly Channel<byte> _refreshQueue = Channel.CreateBounded<byte>(1);

    private static readonly SemaphoreSlim _fileMonitorSemaphore = new(1, 1);

    private static async Task MonitorRefreshQueueAsync()
    {
        if (_fileMonitorSemaphore.CurrentCount < 1)
        {
            return;
        }
        await _fileMonitorSemaphore.WaitAsync();
        var reader = _refreshQueue.Reader;
        while (await reader.WaitToReadAsync())
        {
            if (reader.TryRead(out _))
            {
                ReloadAllBookmarks(false);
            }
        }
        _fileMonitorSemaphore.Release();
    }

    private static readonly List<FileSystemWatcher> Watchers = new();

    internal static void RegisterBookmarkFile(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) || !File.Exists(path))
        {
            return;
        }
        if (Watchers.Any(x => x.Path.Equals(directory, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var watcher = new FileSystemWatcher(directory!)
        {
            Filter = Path.GetFileName(path),
            NotifyFilter = NotifyFilters.FileName |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.Size
        };

        watcher.Changed += static (_, _) =>
        {
            _refreshQueue.Writer.TryWrite(default);
        };

        watcher.Renamed += static (_, _) =>
        {
            _refreshQueue.Writer.TryWrite(default);
        };

        watcher.EnableRaisingEvents = true;

        Watchers.Add(watcher);
    }

    public void ReloadData()
    {
        ReloadAllBookmarks();
    }

    public static void ReloadAllBookmarks(bool disposeFileWatchers = true)
    {
        _cachedBookmarks.Clear();
        if (disposeFileWatchers)
            DisposeFileWatchers();
        LoadBookmarksIfEnabled();
    }

    public string GetTranslatedPluginTitle()
    {
        return Localize.flowlauncher_plugin_browserbookmark_plugin_name();
    }

    public string GetTranslatedPluginDescription()
    {
        return Localize.flowlauncher_plugin_browserbookmark_plugin_description();
    }

    public Control CreateSettingPanel()
    {
        return new SettingsControl(_settings);
    }

    public List<Result> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.ContextData is not BookmarkAttributes bookmarkAttributes)
        {
            return new List<Result>();
        }

        return new List<Result>()
        {
            new()
            {
                Title = Localize.flowlauncher_plugin_browserbookmark_copyurl_title(),
                SubTitle = Localize.flowlauncher_plugin_browserbookmark_copyurl_subtitle(),
                Action = _ =>
                {
                    try
                    {
                        Context.API.CopyToClipboard(bookmarkAttributes.Url);

                        return true;
                    }
                    catch (Exception e)
                    {
                        Context.API.LogException(ClassName, "Failed to set url in clipboard", e);
                        Context.API.ShowMsgError(Localize.flowlauncher_plugin_browserbookmark_copy_failed());
                        return false;
                    }
                },
                IcoPath = @"Images\copylink.png",
                Glyph = new GlyphInfo(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\ue8c8")
            }
        };
    }

    internal class BookmarkAttributes
    {
        internal string Url { get; set; }
    }

    internal class ChromeTabAttributes
    {
        internal nint WindowHandle { get; set; }
        internal int TabIndex { get; set; }
    }

    public void Dispose()
    {
        DisposeFileWatchers();
    }

    private static void DisposeFileWatchers()
    {
        foreach (var watcher in Watchers)
        {
            watcher.Dispose();
        }
        Watchers.Clear();
    }
}
