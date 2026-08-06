using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;
using Creta.Plugin.BrowserTab.Models;

namespace Creta.Plugin.BrowserTab.Commands;

internal static class ChromeTabLoader
{
    private static readonly Condition ChromeWindowCondition = new AndCondition(
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
        new PropertyCondition(AutomationElement.ClassNameProperty, "Chrome_WidgetWin_1"));

    private static readonly Condition TabItemCondition = new PropertyCondition(
        AutomationElement.ControlTypeProperty, ControlType.TabItem);

    internal static List<ChromeTab> LoadAllTabs()
    {
        var tabs = new List<ChromeTab>();
        var recentUrls = ChromeSessionUrlLoader.LoadRecentUrls();
        var urlIndex = 0;

        try
        {
            var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, ChromeWindowCondition);

            foreach (AutomationElement window in windows)
            {
                if (!IsChromeProcess(window))
                {
                    continue;
                }

                var tabItems = window.FindAll(TreeScope.Descendants, TabItemCondition);
                var tabIndex = 0;

                foreach (AutomationElement tabItem in tabItems)
                {
                    var title = tabItem.Current.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    var url = urlIndex < recentUrls.Count ? recentUrls[urlIndex] : string.Empty;
                    tabs.Add(new ChromeTab(title, (nint)window.Current.NativeWindowHandle, tabIndex, url));
                    tabIndex++;
                    urlIndex++;
                }
            }
        }
        catch (Exception e)
        {
            Main.Context.API.LogException(nameof(ChromeTabLoader), "Failed to enumerate Chrome tabs.", e);
        }

        return tabs
            .GroupBy(tab => $"{tab.Title}\u001f{tab.WindowHandle}\u001f{tab.TabIndex}")
            .Select(group => group.First())
            .ToList();
    }

    private static bool IsChromeProcess(AutomationElement window)
    {
        try
        {
            var process = Process.GetProcessById(window.Current.ProcessId);
            return process.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
