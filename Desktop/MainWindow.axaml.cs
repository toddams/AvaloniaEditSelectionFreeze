using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace AvaloniaEditSelectionFreeze;

public partial class MainWindow : Window
{
    private readonly RegistryOptions _registryOptions = new(ThemeName.DarkPlus);
    private TextMate.Installation? _textMate;

    public MainWindow()
    {
        InitializeComponent();

        Editor.Options.AllowScrollBelowDocument = false;
        Editor.Text = string.Join(Environment.NewLine, Enumerable.Range(1, 300).Select(i => $"var line{i} = {i};"));
        InstallTextMate();

        AllowScrollBelowDocument.IsChecked = Editor.Options.AllowScrollBelowDocument;
        AllowScrollBelowDocument.IsCheckedChanged += (_, _) =>
            Editor.Options.AllowScrollBelowDocument = AllowScrollBelowDocument.IsChecked == true;

        TextMateInstalled.IsChecked = true;
        TextMateInstalled.IsCheckedChanged += (_, _) =>
        {
            if (TextMateInstalled.IsChecked == true)
            {
                InstallTextMate();
            }
            else
            {
                _textMate?.Dispose();
                _textMate = null;
            }
        };
    }

    private void InstallTextMate()
    {
        _textMate = Editor.InstallTextMate(_registryOptions);
        _textMate.SetGrammar(_registryOptions.GetScopeByExtension(".cs"));
    }
}
