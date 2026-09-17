using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Symbols;

namespace Material3.Avalonia.Demo.Views;

public partial class TextBoxes : UserControl
{
    public TextBoxes()
    {
        InitializeComponent();
        DataContext = new TextBoxesViewModel();
    }

    private void TogglePasswordReveal(object? sender, RoutedEventArgs e)
    {
        PasswordField.RevealPassword = !PasswordField.RevealPassword;

        if (sender is Button button)
            ButtonAssist.SetIcon(button,
                PasswordField.RevealPassword ? MaterialSymbol.VisibilityOff : MaterialSymbol.Visibility);
    }
}
