using System.Windows;

namespace Flow.Launcher.Plugin.Note.Views;

public partial class NoteEditorWindow
{
    public string EditedContent => EditorTextBox.Text;

    public NoteEditorWindow(string title, string subtitle, string confirmText, string initialContent)
    {
        InitializeComponent();

        EditorTitle.Text = title;
        EditorSubtitle.Text = subtitle;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = Localize.flowlauncher_plugin_note_editor_cancel();
        EditorTextBox.Text = initialContent ?? string.Empty;

        Loaded += (_, _) =>
        {
            EditorTextBox.Focus();
            EditorTextBox.CaretIndex = EditorTextBox.Text.Length;
        };
    }

    private void ConfirmEdit(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EditorTextBox.Text))
        {
            Main.Context.API.ShowMsgBox(
                Localize.flowlauncher_plugin_note_editor_empty_warning(),
                Localize.flowlauncher_plugin_note_editor_window_title());
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelEdit(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
