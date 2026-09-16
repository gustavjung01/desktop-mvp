using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CongTy.Desktop.Products;

internal static class ProductScrollWheel
{
    public static void Route(ScrollViewer outer, MouseWheelEventArgs e)
    {
        var current = e.OriginalSource as DependencyObject;
        while (current is not null && !ReferenceEquals(current, outer))
        {
            if (current is ScrollViewer inner
                && !ReferenceEquals(inner, outer)
                && CanScroll(inner, e.Delta))
            {
                return;
            }

            current = ParentOf(current);
        }

        if (!CanScroll(outer, e.Delta)) return;

        outer.ScrollToVerticalOffset(Math.Clamp(
            outer.VerticalOffset - e.Delta,
            0d,
            outer.ScrollableHeight));
        e.Handled = true;
    }

    private static bool CanScroll(ScrollViewer viewer, int delta) =>
        delta < 0
            ? viewer.VerticalOffset < viewer.ScrollableHeight - 0.5
            : viewer.VerticalOffset > 0.5;

    private static DependencyObject? ParentOf(DependencyObject current) =>
        current is Visual or Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);
}
