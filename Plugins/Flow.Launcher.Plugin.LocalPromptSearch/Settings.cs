using Flow.Launcher.Plugin;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.LocalPromptSearch;

public class Settings : BaseModel
{
    private string _promptFilePath = string.Empty;

    public string PromptFilePath
    {
        get => _promptFilePath;
        set
        {
            if (_promptFilePath != value)
            {
                _promptFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> RecentPromptIds { get; set; } = [];
}
