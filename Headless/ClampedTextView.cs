using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaEdit.Rendering;

namespace HeadlessRepro;

/// <summary>
/// The workaround: only scrolls to the part of a rectangle inside the scroll extent.
/// </summary>
internal sealed class ClampedTextView : TextView
{
    public override void MakeVisible(Rect rectangle)
    {
        var extent = new Rect(((IScrollable)this).Extent);
        if (rectangle.Intersects(extent))
        {
            rectangle = rectangle.Intersect(extent);
        }

        base.MakeVisible(rectangle);
    }
}
