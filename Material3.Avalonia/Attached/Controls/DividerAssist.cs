using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>
/// Defines the Material divider inset variant.
/// </summary>
public enum DividerVariant
{
    /// <summary>
    /// Extends across the available length without insets.
    /// </summary>
    FullWidth,

    /// <summary>
    /// Insets the start of the divider.
    /// </summary>
    Inset,

    /// <summary>
    /// Insets both ends of the divider.
    /// </summary>
    MiddleInset
}

/// <summary>
/// Provides Material divider properties for <see cref="Separator" />.
/// </summary>
public static class DividerAssist
{
    /// <summary>
    /// Defines the Material divider inset variant.
    /// </summary>
    public static readonly AttachedProperty<DividerVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Separator, DividerVariant>(
            "Variant", typeof(DividerAssist), DividerVariant.FullWidth);

    /// <summary>
    /// Defines the direction of the divider line.
    /// </summary>
    public static readonly AttachedProperty<Orientation> OrientationProperty =
        AvaloniaProperty.RegisterAttached<Separator, Orientation>(
            "Orientation", typeof(DividerAssist), Orientation.Horizontal);

    /// <summary>
    /// Defines the finite, nonnegative inset from the leading edge, or the top for vertical dividers.
    /// </summary>
    public static readonly AttachedProperty<double> InsetStartProperty =
        AvaloniaProperty.RegisterAttached<Separator, double>(
            "InsetStart", typeof(DividerAssist), validate: IsValidInset);

    /// <summary>
    /// Defines the finite, nonnegative inset from the trailing edge, or the bottom for vertical dividers.
    /// </summary>
    public static readonly AttachedProperty<double> InsetEndProperty =
        AvaloniaProperty.RegisterAttached<Separator, double>(
            "InsetEnd", typeof(DividerAssist), validate: IsValidInset);

    /// <summary>
    /// Sets the Material divider inset variant.
    /// </summary>
    public static void SetVariant(Separator separator, DividerVariant value) =>
        separator.SetValue(VariantProperty, value);

    /// <summary>
    /// Gets the Material divider inset variant.
    /// </summary>
    public static DividerVariant GetVariant(Separator separator) => separator.GetValue(VariantProperty);

    /// <summary>
    /// Sets the direction of the divider line.
    /// </summary>
    public static void SetOrientation(Separator separator, Orientation value) =>
        separator.SetValue(OrientationProperty, value);

    /// <summary>
    /// Gets the direction of the divider line.
    /// </summary>
    public static Orientation GetOrientation(Separator separator) => separator.GetValue(OrientationProperty);

    /// <summary>
    /// Sets the leading inset, or the top inset for vertical dividers.
    /// </summary>
    public static void SetInsetStart(Separator separator, double value) =>
        separator.SetValue(InsetStartProperty, value);

    /// <summary>
    /// Gets the leading inset, or the top inset for vertical dividers.
    /// </summary>
    public static double GetInsetStart(Separator separator) => separator.GetValue(InsetStartProperty);

    /// <summary>
    /// Sets the trailing inset, or the bottom inset for vertical dividers.
    /// </summary>
    public static void SetInsetEnd(Separator separator, double value) => separator.SetValue(InsetEndProperty, value);

    /// <summary>
    /// Gets the trailing inset, or the bottom inset for vertical dividers.
    /// </summary>
    public static double GetInsetEnd(Separator separator) => separator.GetValue(InsetEndProperty);

    private static bool IsValidInset(double value) => double.IsFinite(value) && value >= 0;
}