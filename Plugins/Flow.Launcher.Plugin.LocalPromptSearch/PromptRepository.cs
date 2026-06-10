using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Flow.Launcher.Plugin.LocalPromptSearch;

internal sealed class PromptRepository
{
    private readonly string _pluginDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private List<PromptTemplate> _prompts = [];

    internal string LoadError { get; private set; } = string.Empty;
    internal string CurrentFilePath { get; private set; } = string.Empty;

    internal PromptRepository(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
    }

    internal IReadOnlyList<PromptTemplate> GetPrompts() => _prompts;

    internal bool CanPersistFavorites =>
        !string.IsNullOrWhiteSpace(CurrentFilePath) &&
        string.Equals(Path.GetFileName(CurrentFilePath), "prompts.json", StringComparison.CurrentCultureIgnoreCase);

    internal void Load(string configuredPath = "")
    {
        LoadError = string.Empty;
        _prompts = [];
        CurrentFilePath = string.Empty;

        var primaryFilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(_pluginDirectory, "prompts.json")
            : ExpandPath(configuredPath);
        var fallbackFilePath = Path.Combine(_pluginDirectory, "prompts.sample.json");

        var filePath = File.Exists(primaryFilePath) ? primaryFilePath : fallbackFilePath;
        if (!File.Exists(filePath))
        {
            LoadError = "未找到 prompts.json 或 prompts.sample.json。";
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var prompts = JsonSerializer.Deserialize<List<PromptTemplate>>(json, _jsonOptions) ?? [];

            CurrentFilePath = filePath;
            _prompts = prompts
                .Where(prompt => !string.IsNullOrWhiteSpace(prompt.Title))
                .Select(prompt =>
                {
                    prompt.Tags ??= [];
                    prompt.Keywords ??= [];
                    prompt.Description ??= string.Empty;
                    prompt.Content ??= string.Empty;
                    prompt.Category ??= string.Empty;
                    return prompt;
                })
                .ToList();

            if (_prompts.Count == 0)
            {
                LoadError = $"{Path.GetFileName(filePath)} 中没有可用的 Prompt 模板。";
            }
        }
        catch (Exception ex)
        {
            LoadError = $"读取模板失败：{ex.Message}";
        }
    }

    private static string ExpandPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        return Path.GetFullPath(expanded);
    }

    internal bool Save()
    {
        if (!CanPersistFavorites)
        {
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(_prompts, _jsonOptions);
            File.WriteAllText(CurrentFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
