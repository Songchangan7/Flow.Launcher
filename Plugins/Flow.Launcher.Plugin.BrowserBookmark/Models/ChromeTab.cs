namespace Flow.Launcher.Plugin.BrowserBookmark.Models;

public record ChromeTab(string Title, nint WindowHandle, int TabIndex);
