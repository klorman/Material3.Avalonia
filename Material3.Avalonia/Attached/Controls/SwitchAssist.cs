using Avalonia;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Material switch handle icon configurations.</summary>
public enum SwitchIconConfiguration
{
    /// <summary>No handle icons.</summary>
    None,

    /// <summary>An icon in the selected state.</summary>
    Selected,

    /// <summary>Icons in both states.</summary>
    SelectedAndUnselected
}

/// <summary>Logical placement of the adjacent switch label.</summary>
public enum SwitchLabelPlacement
{
    /// <summary>The label precedes the switch.</summary>
    Leading,

    /// <summary>The label follows the switch.</summary>
    Trailing
}

/// <summary>Applies Material switch options to native ToggleSwitch controls.</summary>
public static class SwitchAssist
{
    /// <summary>Identifies the handle icon configuration.</summary>
    public static readonly AttachedProperty<SwitchIconConfiguration> IconConfigurationProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, SwitchIconConfiguration>("IconConfiguration",
            typeof(SwitchAssist));

    /// <summary>Identifies the selected handle icon.</summary>
    public static readonly AttachedProperty<object?> SelectedIconProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, object?>("SelectedIcon", typeof(SwitchAssist));

    /// <summary>Identifies the unselected handle icon.</summary>
    public static readonly AttachedProperty<object?> UnselectedIconProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, object?>("UnselectedIcon", typeof(SwitchAssist));

    /// <summary>Identifies the logical label placement.</summary>
    public static readonly AttachedProperty<SwitchLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, SwitchLabelPlacement>("LabelPlacement", typeof(SwitchAssist));

    /// <summary>Gets the handle icon configuration.</summary>
    public static SwitchIconConfiguration GetIconConfiguration(ToggleSwitch control) =>
        control.GetValue(IconConfigurationProperty);

    /// <summary>Sets the handle icon configuration.</summary>
    public static void SetIconConfiguration(ToggleSwitch control, SwitchIconConfiguration value) =>
        control.SetValue(IconConfigurationProperty, value);

    /// <summary>Gets the selected handle icon.</summary>
    public static object? GetSelectedIcon(ToggleSwitch control) => control.GetValue(SelectedIconProperty);

    /// <summary>Sets the selected handle icon.</summary>
    public static void SetSelectedIcon(ToggleSwitch control, object? value) =>
        control.SetValue(SelectedIconProperty, value);

    /// <summary>Gets the unselected handle icon.</summary>
    public static object? GetUnselectedIcon(ToggleSwitch control) => control.GetValue(UnselectedIconProperty);

    /// <summary>Sets the unselected handle icon.</summary>
    public static void SetUnselectedIcon(ToggleSwitch control, object? value) =>
        control.SetValue(UnselectedIconProperty, value);

    /// <summary>Gets the logical label placement.</summary>
    public static SwitchLabelPlacement GetLabelPlacement(ToggleSwitch control) =>
        control.GetValue(LabelPlacementProperty);

    /// <summary>Sets the logical label placement.</summary>
    public static void SetLabelPlacement(ToggleSwitch control, SwitchLabelPlacement value) =>
        control.SetValue(LabelPlacementProperty, value);
}