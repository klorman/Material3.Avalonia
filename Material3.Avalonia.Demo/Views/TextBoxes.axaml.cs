using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Demo.ViewModels;

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
        {
            var iconKey = PasswordField.RevealPassword ? "VisibilityOffIcon" : "VisibilityIcon";
            if (Application.Current?.Resources.TryGetResource(iconKey, null, out var icon) == true)
                ButtonAssist.SetIcon(button, icon);
        }
    }
}
