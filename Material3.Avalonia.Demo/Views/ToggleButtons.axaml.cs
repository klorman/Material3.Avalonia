using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Demo.Views;

public partial class ToggleButtons : UserControl
{
    public ToggleButtons() => InitializeComponent();

    private void OnDefaultDensityClick(object? sender, RoutedEventArgs e) =>
        DensityAssist.SetDensity(ExamplesPanel, MaterialDensity.Default);

    private void OnDenseDensityClick(object? sender, RoutedEventArgs e) =>
        DensityAssist.SetDensity(ExamplesPanel, MaterialDensity.Dense3);
}