namespace Flow.Launcher.Plugin.BrowserTab.Models;

public record ChromeTab(string Title, nint WindowHandle, int TabIndex, string Url = "");
