using AvaloniaEdit;

namespace HeadlessRepro;

internal sealed class ClampedTextEditor : TextEditor
{
    public ClampedTextEditor()
        : base(new ClampedTextArea())
    {
    }

    protected override Type StyleKeyOverride => typeof(TextEditor);
}
