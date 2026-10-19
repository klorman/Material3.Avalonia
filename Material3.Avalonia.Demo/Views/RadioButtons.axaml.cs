using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Controls.Helpers;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Density;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

/// <summary>The radio button gallery shared by desktop and single-view hosts.</summary>
public partial class RadioButtons : UserControl
{
    private bool _alternateColor;

    /// <summary>Creates the radio button gallery.</summary>
    public RadioButtons()
    {
        InitializeComponent();
        DataContext = new RadioButtonsViewModel();
    }

    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RadioButtonsViewModel model) return;
        if (model.Home == true) model.Office = true;
        else model.Home = true;
    }

    private void OnMotionSelectionClick(object? sender, RoutedEventArgs e)
    {
        if (MotionOption.IsChecked == true)
            MotionAlternative.IsChecked = RtlMotionAlternative.IsChecked = true;
        else
            MotionOption.IsChecked = RtlMotionOption.IsChecked = true;
    }

    private void OnTextClick(object? sender, RoutedEventArgs e)
    {
        var text = Equals(MotionOption.Content, "Short label")
            ? "A longer radio label that wraps naturally while the indicator and focus ring stay centered."
            : "Short label";
        MotionOption.Content = RtlMotionOption.Content = text;
    }

    private void OnTemplateClick(object? sender, RoutedEventArgs e)
    {
        IDataTemplate? template = MotionOption.ContentTemplate is null
            ? new FuncDataTemplate<string>((text, _) =>
            {
                var detail = new TextBlock { Text = "Custom supporting content" };
                Typography.SetTextStyle(detail, MdTextStyle.BodySmall);
                return new StackPanel
                {
                    Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, detail }
                };
            })
            : null;
        MotionOption.ContentTemplate = RtlMotionOption.ContentTemplate = template;
    }

    private void OnWidthClick(object? sender, RoutedEventArgs e) =>
        MotionOption.MaxWidth = RtlMotionOption.MaxWidth = MotionOption.MaxWidth == 480 ? 240 : 480;

    private void OnDensityClick(object? sender, RoutedEventArgs e)
    {
        var density = DensityAssist.GetDensity(MotionOption);
        var next = density == MaterialDensity.Dense5 ? MaterialDensity.Default : density - 1;
        DensityAssist.SetDensity(MotionOption, next);
        DensityAssist.SetDensity(RtlMotionOption, next);
    }

    private void OnValidationClick(object? sender, RoutedEventArgs e)
    {
        if (DataValidationErrors.GetHasErrors(ValidatedOption))
            DataValidationErrors.ClearErrors(ValidatedOption);
        else
            DataValidationErrors.SetErrors(ValidatedOption,
                new[] { "This preference is not available for the selected address." });
    }

    private void OnRemapClick(object? sender, RoutedEventArgs e)
    {
        _alternateColor = !_alternateColor;
        foreach (var state in new[] { "", "Hover", "Focus", "Pressed" })
            RemappedOption.Resources[$"MdCompRadioButtonSelected{state}IconBrush"] =
                new TokenAlias(_alternateColor ? "MdSysTertiaryBrush" : "MdSysPrimaryBrush");
    }
}