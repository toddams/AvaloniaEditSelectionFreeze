# AvaloniaEdit selection freeze repro

Dragging a mouse selection to the last line of a `TextEditor` freezes the UI thread for good when `Options.AllowScrollBelowDocument` is `false` and AvaloniaEdit.TextMate is installed. Windows marks the window as not responding; nothing is thrown or logged.

Avalonia 12.1.3, Avalonia.AvaloniaEdit 12.0.0, AvaloniaEdit.TextMate 12.0.0 (TextMateSharp 2.0.3), .NET 10, Windows 11.

## Automatic repro

`dotnet run --project Headless` drives the drag through Avalonia.Headless and exits with code 1 once the UI thread stops finishing frames.

| Command | Result |
|---|---|
| `dotnet run --project Headless` | HANG |
| `dotnet run --project Headless -- --no-textmate` | OK, but the view is left 5px past the end (offset 4814.72, max 4809.72) |
| `dotnet run --project Headless -- --allow-scroll-below` | OK |
| `dotnet run --project Headless -- --clamp` | OK, with the workaround below |

## Manual repro

`dotnet run --project Desktop`, press the left mouse button on a line, keep holding it, and drag the pointer below the bottom edge of the editor. When the selection reaches the last line, the window stops responding. The two check boxes switch `AllowScrollBelowDocument` and TextMate; change either one and the same drag works.

## Cause

Two pieces that are harmless on their own.

**A 5px fight at the end of the document.** `SelectionMouseHandler.TextView_ScrollOffsetChanged` (added in 35f8ea3, "Fix: Update the selection when scrolling") re-runs `ExtendSelectionToMouse` on every scroll change while a selection drag is active, and that ends with `Caret.BringCaretToView(5.0)`. On the last line, the 5px border makes `TextView.MakeVisible` ask for `DocumentHeight + 5 - viewport`, past the scroll extent. `TextView.ArrangeOverride` clamps the offset back through `SetScrollData`, which raises `ScrollOffsetChanged` again:

```
TextView.ArrangeOverride → SetScrollData → SetScrollOffset
  → SelectionMouseHandler.TextView_ScrollOffsetChanged → ExtendSelectionToMouse
  → Caret.BringCaretToView(5.0) → TextView.MakeVisible → InvalidateMeasure → next layout pass → …
```

Without TextMate, Avalonia's layout manager stops re-measuring the TextView after 10 rounds in one layout pass and nothing requests another pass, so the fight ends within a frame. The view is left scrolled 5px past the document.

**TextMate restarting it.** `TextEditorModel.TextView_ScrollOffsetChanged` posts `TokenizeViewPort` at `DispatcherPriority.Default` on every scroll change, so each round of the fight queues more work above input priority, and that work leads to another layout pass. The fight never ends and input is never processed. Removing only that handler (by reflection) makes the hang go away.

## Workaround

A `TextView` subclass that only scrolls to the part of the rectangle inside the scroll extent, installed through `TextArea(TextView)` and `TextEditor(TextArea)` subclasses (see `Headless/Clamped*.cs`):

```csharp
public override void MakeVisible(Rect rectangle)
{
    var extent = new Rect(((IScrollable)this).Extent);
    if (rectangle.Intersects(extent))
    {
        rectangle = rectangle.Intersect(extent);
    }

    base.MakeVisible(rectangle);
}
```

Clamping the new offset to `[0, extent - viewport]` inside `MakeVisible` (it only clamps to `>= 0` today) would fix both the hang and the 5px overshoot upstream.
