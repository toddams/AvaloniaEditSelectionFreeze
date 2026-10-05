using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using HeadlessRepro;
using TextMateSharp.Grammars;

// Drags a mouse selection below the bottom edge of a TextEditor until it reaches the last line, then
// reports whether the UI thread kept up. Options: --no-textmate, --allow-scroll-below, --clamp
var noTextMate = args.Contains("--no-textmate");
var allowScrollBelow = args.Contains("--allow-scroll-below");
var clamp = args.Contains("--clamp");
var scenario = $"TextMate={!noTextMate}, AllowScrollBelowDocument={allowScrollBelow}, clamp={clamp}";

var framesRendered = 0;
StartWatchdog();

AppBuilder.Configure<HeadlessApp>()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
    .SetupWithoutStarting();

TextEditor editor = clamp ? new ClampedTextEditor() : new TextEditor();
editor.FontFamily = new FontFamily("Consolas, Menlo, monospace");
editor.ShowLineNumbers = true;
editor.Options.AllowScrollBelowDocument = allowScrollBelow;
editor.Text = string.Join("\n", Enumerable.Range(1, 300).Select(i => $"var line{i} = {i};"));
if (!noTextMate)
{
    var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
    editor.InstallTextMate(registryOptions).SetGrammar(registryOptions.GetScopeByExtension(".cs"));
}

var window = new Window { Width = 800, Height = 500, Content = editor };
window.Show();
RenderFrames(5);

window.MouseDown(new Point(200, 100), MouseButton.Left);
RenderFrames(2);

// Each move below the editor extends the selection and scrolls one step towards the end
for (var i = 0; i < 400; i++)
{
    window.MouseMove(new Point(200 + i % 2, window.Height + 30), RawInputModifiers.LeftMouseButton);
    RenderFrames(1);
}

var scrollable = (IScrollable)editor.TextArea.TextView;
var maxOffset = scrollable.Extent.Height - scrollable.Viewport.Height;
Console.WriteLine(
    $"OK [{scenario}]: caret on line {editor.TextArea.Caret.Line} of {editor.Document.LineCount}, " +
    $"scroll offset {scrollable.Offset.Y:0.##}, max offset {maxOffset:0.##}");
return 0;

void RenderFrames(int count)
{
    for (var i = 0; i < count; i++)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Interlocked.Increment(ref framesRendered);
    }
}

void StartWatchdog()
{
    var watchdog = new Thread(() =>
    {
        var lastFrames = -1;
        var stuckSince = DateTime.UtcNow;
        while (true)
        {
            Thread.Sleep(250);
            var frames = Volatile.Read(ref framesRendered);
            if (frames != lastFrames)
            {
                lastFrames = frames;
                stuckSince = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow - stuckSince > TimeSpan.FromSeconds(5))
            {
                Console.WriteLine($"HANG [{scenario}]: the UI thread has not finished a frame for 5 s (after {frames} frames)");
                Environment.Exit(1);
            }
        }
    })
    {
        IsBackground = true
    };
    watchdog.Start();
}
