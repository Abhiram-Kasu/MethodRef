using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using MethodRefFrontend.ViewModels;
using TextMateSharp.Grammars;


namespace MethodRefFrontend.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var box = sender as TextBox;
        var newText = box?.Text ?? string.Empty;
        if (DataContext is not MainViewModel viewModel) return;
        await viewModel.Search(newText);

    }

    private void Visual_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextEditor textEditor) return;
        
        //Here we initialize RegistryOptions with the theme we want to use.
        var  registryOptions = new RegistryOptions(ThemeName.DarkPlus);

//Initial setup of TextMate.
        var textMateInstallation = textEditor.InstallTextMate(registryOptions);

//Here we are getting the language by the extension and right after that we are initializing grammar with this language.
//And that's all 😀, you are ready to use AvaloniaEdit with syntax highlighting!
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id));

    }
}