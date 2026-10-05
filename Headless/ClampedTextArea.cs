using AvaloniaEdit.Editing;

namespace HeadlessRepro;

internal sealed class ClampedTextArea : TextArea
{
    public ClampedTextArea()
        : base(new ClampedTextView())
    {
    }

    protected override Type StyleKeyOverride => typeof(TextArea);
}
