using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Helpers;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Density;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

/// <summary>The switch gallery shared by desktop and single-view hosts.</summary>
public partial class Switches : UserControl
{
    private bool _alternateColor;

    /// <summary>Creates the switch gallery.</summary>
    public Switches()
    {
        InitializeComponent();
        DataContext = new SwitchesViewModel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ReduceMotionSwitch.IsChecked = MotionSettings.ReduceMotion;
    }

    private void OnSettingClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SwitchesViewModel model) model.NotificationsEnabled = model.NotificationsEnabled != true;
    }

    private void OnToggleClick(object? sender, RoutedEventArgs e) =>
        MotionSwitch.IsChecked = RtlMotionSwitch.IsChecked = MotionSwitch.IsChecked != true;

    private void OnTextClick(object? sender, RoutedEventArgs e) =>
        MotionSwitch.Content = RtlMotionSwitch.Content = Equals(MotionSwitch.Content, "Short label")
            ? "A longer setting label that wraps while the switch and its focus indicator stay centered."
            : "Short label";

    private void OnTemplateClick(object? sender, RoutedEventArgs e)
    {
        IDataTemplate? template = MotionSwitch.ContentTemplate is null
            ? new FuncDataTemplate<string>((text, _) =>
            {
                var detail = new TextBlock { Text = "Custom supporting content", TextWrapping = TextWrapping.Wrap };
                Typography.SetTextStyle(detail, MdTextStyle.BodySmall);
                return new StackPanel
                    { Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, detail } };
            })
            : null;
        MotionSwitch.ContentTemplate = RtlMotionSwitch.ContentTemplate = template;
    }

    private void OnWidthClick(object? sender, RoutedEventArgs e) =>
        MotionSwitch.MaxWidth = RtlMotionSwitch.MaxWidth = MotionSwitch.MaxWidth == 480 ? 240 : 480;

    private void OnDensityClick(object? sender, RoutedEventArgs e)
    {
        var density = DensityAssist.GetDensity(MotionSwitch);
        var next = density == MaterialDensity.Dense5 ? MaterialDensity.Default : density - 1;
        DensityAssist.SetDensity(MotionSwitch, next);
        DensityAssist.SetDensity(RtlMotionSwitch, next);
    }

    private void OnIconsClick(object? sender, RoutedEventArgs e)
    {
        var mode = SwitchAssist.GetIconConfiguration(MotionSwitch);
        var next = mode == SwitchIconConfiguration.SelectedAndUnselected ? SwitchIconConfiguration.None : mode + 1;
        SwitchAssist.SetIconConfiguration(MotionSwitch, next);
        SwitchAssist.SetIconConfiguration(RtlMotionSwitch, next);
    }

    private void OnReduceMotionClick(object? sender, RoutedEventArgs e) =>
        MotionSettings.ReduceMotion = ReduceMotionSwitch.IsChecked == true;

    private void OnValidationClick(object? sender, RoutedEventArgs e)
    {
        if (DataValidationErrors.GetHasErrors(ValidatedSwitch)) DataValidationErrors.ClearErrors(ValidatedSwitch);
        else
            DataValidationErrors.SetErrors(ValidatedSwitch,
                new[] { "This preference is unavailable for the current account." });
    }

    private void OnRemapClick(object? sender, RoutedEventArgs e)
    {
        _alternateColor = !_alternateColor;
        foreach (var state in new[] { "", "Hover", "Focus", "Pressed" })
            RemappedSwitch.Resources[$"MdCompSwitchSelected{state}TrackBrush"] =
                new TokenAlias(_alternateColor ? "MdSysTertiaryBrush" : "MdSysPrimaryBrush");
    }
}