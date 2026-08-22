using Avalonia;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>
/// Provides Material attached properties for <see cref="ScrollViewer" />.
/// </summary>
public static class ScrollViewerAssist
{
    /// <summary>
    /// Defines whether mouse wheel scrolling is smoothed.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSmoothWheelScrollingEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsSmoothWheelScrollingEnabled",
            typeof(ScrollViewerAssist),
            defaultValue: true,
            inherits: true);

    /// <summary>
    /// Defines the scrollbar inset inside rounded containers.
    /// </summary>
    public static readonly AttachedProperty<Thickness> ScrollBarInsetProperty =
        AvaloniaProperty.RegisterAttached<Control, Thickness>(
            "ScrollBarInset",
            typeof(ScrollViewerAssist),
            defaultValue: new Thickness(0),
            inherits: true);

    /// <summary>
    /// Sets whether mouse wheel scrolling is smoothed.
    /// </summary>
    public static void SetIsSmoothWheelScrollingEnabled(Control control, bool value) =>
        control.SetValue(IsSmoothWheelScrollingEnabledProperty, value);

    /// <summary>
    /// Gets whether mouse wheel scrolling is smoothed.
    /// </summary>
    public static bool GetIsSmoothWheelScrollingEnabled(Control control) =>
        control.GetValue(IsSmoothWheelScrollingEnabledProperty);

    /// <summary>
    /// Sets the scrollbar inset inside rounded containers.
    /// </summary>
    public static void SetScrollBarInset(Control control, Thickness value) =>
        control.SetValue(ScrollBarInsetProperty, value);

    /// <summary>
    /// Gets the scrollbar inset inside rounded containers.
    /// </summary>
    public static Thickness GetScrollBarInset(Control control) =>
        control.GetValue(ScrollBarInsetProperty);
}
