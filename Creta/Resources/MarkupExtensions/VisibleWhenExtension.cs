using System.Windows;
using System.Windows.Data;

namespace Creta.Resources.MarkupExtensions;

#nullable enable

public class VisibleWhenExtension : CollapsedWhenExtension
{
    protected override Visibility DefaultVisibility => Visibility.Collapsed;
    protected override Visibility InvertedVisibility => Visibility.Visible;

    public VisibleWhenExtension(Binding when) : base(when) { }
}
