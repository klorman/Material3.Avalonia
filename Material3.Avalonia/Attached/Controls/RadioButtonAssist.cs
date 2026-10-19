using Avalonia;
using Avalonia.Controls;
using Material3.Avalonia.Attached.Controls.Internal;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Provides optional Material keyboard navigation for native radio groups.</summary>
public static class RadioButtonAssist
{
    /// <summary>Enables arrow selection and a single tab stop for a radio group container.</summary>
    public static readonly AttachedProperty<bool> GroupNavigationProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("GroupNavigation", typeof(RadioButtonAssist));

    static RadioButtonAssist() => GroupNavigationProperty.Changed.AddClassHandler<Control>((control, _) =>
        RadioGroupNavigation.Update(control));

    /// <summary>Gets whether the container uses Material radio group navigation.</summary>
    public static bool GetGroupNavigation(Control control) => control.GetValue(GroupNavigationProperty);

    /// <summary>Sets whether the container uses Material radio group navigation.</summary>
    public static void SetGroupNavigation(Control control, bool value) =>
        control.SetValue(GroupNavigationProperty, value);
}