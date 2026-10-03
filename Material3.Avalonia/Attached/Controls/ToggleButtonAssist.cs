using Avalonia;
using Avalonia.Controls.Primitives;
using Material3.Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Material color variants supported by a toggle button.</summary>
public enum ToggleButtonVariant
{
    /// <summary>Filled container.</summary>
    Filled,

    /// <summary>Elevated container.</summary>
    Elevated,

    /// <summary>Tonal container.</summary>
    Tonal,

    /// <summary>Outlined container.</summary>
    Outlined
}

/// <summary>Applies Material toggle button options to native ToggleButton controls.</summary>
public static class ToggleButtonAssist
{
    static ToggleButtonAssist()
    {
        VariantProperty.Changed.AddClassHandler<ToggleButton>((button, _) => ValidateTarget(button));
        SizeProperty.Changed.AddClassHandler<ToggleButton>((button, _) => ValidateTarget(button));
        ShapeProperty.Changed.AddClassHandler<ToggleButton>((button, _) => ValidateTarget(button));
        IconProperty.Changed.AddClassHandler<ToggleButton>((button, _) => ValidateTarget(button));
    }

    /// <summary>Identifies the Material toggle button icon property.</summary>
    public static readonly AttachedProperty<object?> IconProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, object?>("Icon", typeof(ToggleButtonAssist));

    /// <summary>Sets the Material toggle button icon.</summary>
    public static void SetIcon(ToggleButton button, object? value)
    {
        ValidateTarget(button);
        button.SetValue(IconProperty, value);
    }

    /// <summary>Gets the Material toggle button icon.</summary>
    public static object? GetIcon(ToggleButton button) => button.GetValue(IconProperty);

    /// <summary>Identifies the Material toggle button variant property.</summary>
    public static readonly AttachedProperty<ToggleButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonVariant>(
            "Variant", typeof(ToggleButtonAssist), defaultValue: ToggleButtonVariant.Filled);

    /// <summary>Sets the Material toggle button variant.</summary>
    public static void SetVariant(ToggleButton button, ToggleButtonVariant value)
    {
        ValidateTarget(button);
        button.SetValue(VariantProperty, value);
    }

    /// <summary>Gets the Material toggle button variant.</summary>
    public static ToggleButtonVariant GetVariant(ToggleButton button) => button.GetValue(VariantProperty);

    /// <summary>Identifies the Material toggle button size property.</summary>
    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ButtonSize>(
            "Size", typeof(ToggleButtonAssist), defaultValue: ButtonSize.Small);

    /// <summary>Sets the Material toggle button size.</summary>
    public static void SetSize(ToggleButton button, ButtonSize value)
    {
        ValidateTarget(button);
        button.SetValue(SizeProperty, value);
    }

    /// <summary>Gets the Material toggle button size.</summary>
    public static ButtonSize GetSize(ToggleButton button) => button.GetValue(SizeProperty);

    /// <summary>Identifies the Material toggle button shape property.</summary>
    public static readonly AttachedProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ButtonShape>(
            "Shape", typeof(ToggleButtonAssist), defaultValue: ButtonShape.Round);

    /// <summary>Sets the Material toggle button shape.</summary>
    public static void SetShape(ToggleButton button, ButtonShape value)
    {
        ValidateTarget(button);
        button.SetValue(ShapeProperty, value);
    }

    /// <summary>Gets the Material toggle button shape.</summary>
    public static ButtonShape GetShape(ToggleButton button) => button.GetValue(ShapeProperty);

    private static void ValidateTarget(ToggleButton button)
    {
        if (button is IconToggleButton)
        {
            throw new ArgumentException(
                "ToggleButtonAssist cannot be used with IconToggleButton.",
                nameof(button));
        }
    }
}