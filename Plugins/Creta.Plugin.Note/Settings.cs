using Flow.Launcher.Plugin;

namespace Creta.Plugin.Note;

public class Settings : BaseModel
{
    private string _notesFilePath = string.Empty;

    public string NotesFilePath
    {
        get => _notesFilePath;
        set
        {
            if (_notesFilePath != value)
            {
                _notesFilePath = value;
                OnPropertyChanged();
            }
        }
    }
}
