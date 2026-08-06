using System.Collections.Generic;

namespace Creta.Plugin.LocalPromptSearch;

internal sealed class PromptTemplate
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public List<string> Keywords { get; set; } = [];

    public string Content { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool Favorite { get; set; }
}
