using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using Creta.Infrastructure;
using Creta.Plugin.BrowserTab.Models;

namespace Creta.Plugin.BrowserTab.Commands;

internal static class ChromeTabActivator
{
    private const int SwRestore = 9;

    private static readonly Condition TabItemCondition = new PropertyCondition(
        AutomationElement.ControlTypeProperty, ControlType.TabItem);

    internal static bool Activate(ChromeTab tab)
    {
        try
        {
            if (tab.WindowHandle == nint.Zero)
            {
                return false;
            }

            ShowWindow(tab.WindowHandle, SwRestore);
            Win32Helper.SetForegroundWindow(tab.WindowHandle);
            Thread.Sleep(80);

            var window = AutomationElement.FromHandle(tab.WindowHandle);
            var tabItems = window.FindAll(TreeScope.Descendants, TabItemCondition)
                .Cast<AutomationElement>()
                .Where(item => !string.IsNullOrWhiteSpace(item.Current.Name))
                .ToList();

            if (tab.TabIndex < 0 || tab.TabIndex >= tabItems.Count)
            {
                return false;
            }

            var target = tabItems[tab.TabIndex];
            if (target.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObject) &&
                patternObject is SelectionItemPattern selectionItemPattern)
            {
                selectionItemPattern.Select();
                return true;
            }
        }
        catch (Exception e)
        {
            Main.Context.API.LogException(nameof(ChromeTabActivator), "Failed to activate Chrome tab.", e);
        }

        return false;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);
}
