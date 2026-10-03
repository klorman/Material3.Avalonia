using Avalonia.Controls;
using Avalonia.Interactivity;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Demo.Views;

public partial class IconButtons : UserControl
{
    public IconButtons() => InitializeComponent();

    private static readonly IconButtonWidth[] WidthModes =
    [
        IconButtonWidth.Narrow,
        IconButtonWidth.Default,
        IconButtonWidth.Wide,
    ];

    private void OnWidthModeClick(object? sender, RoutedEventArgs e)
    {
        var currentIndex = Array.IndexOf(WidthModes, WidthModeSample.WidthMode);
        var nextMode = WidthModes[(currentIndex + 1) % WidthModes.Length];
        WidthModeSample.WidthMode = nextMode;
        WidthModeValue.Text = $"Current: {nextMode}";
    }

    private void OnDefaultDensityClick(object? sender, RoutedEventArgs e) =>
        SetDensity(MaterialDensity.Default);

    private void OnDense2Click(object? sender, RoutedEventArgs e) =>
        SetDensity(MaterialDensity.Dense2);

    private void OnDense3Click(object? sender, RoutedEventArgs e) =>
        SetDensity(MaterialDensity.Dense3);

    private void SetDensity(MaterialDensity density)
    {
        DensityAssist.SetDensity(DenseSmallSample, density);
        DensityAssist.SetDensity(DenseMediumSample, density);
        DensityAssist.SetDensity(DenseToggleSample, density);
    }
}