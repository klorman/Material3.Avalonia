using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Demo.ViewModels;

namespace Material3.Avalonia.Demo.Views;

public partial class Buttons : UserControl
{
    public Buttons()
    {
        InitializeComponent();
        DataContext = new ButtonsViewModel();
    }

    private Button[] ResizeSamples => [ResizeSample, ResizeSampleCentered, ResizeSampleRtl];

    private void OnResizeLabelClick(object? sender, RoutedEventArgs e)
    {
        var content = Equals(ResizeSample.Content, "Save") ? "Save changes to your profile" : "Save";
        foreach (var sample in ResizeSamples)
            sample.Content = content;
    }

    private void OnResizeIconClick(object? sender, RoutedEventArgs e)
    {
        object? icon = ButtonAssist.GetIcon(ResizeSample) is null ? MaterialSymbol.Check : null;
        foreach (var sample in ResizeSamples)
            ButtonAssist.SetIcon(sample, icon);
    }

    private void OnResizeSizeClick(object? sender, RoutedEventArgs e)
    {
        var size = ButtonAssist.GetSize(ResizeSample) == ButtonSize.Small ? ButtonSize.Medium : ButtonSize.Small;
        foreach (var sample in ResizeSamples)
            ButtonAssist.SetSize(sample, size);
    }
}