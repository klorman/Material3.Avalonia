using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Material button color variants.</summary>
public enum ButtonVariant
{
    /// <summary>Filled container.</summary>
    Filled,

    /// <summary>Elevated container.</summary>
    Elevated,

    /// <summary>Tonal container.</summary>
    Tonal,

    /// <summary>Outlined container.</summary>
    Outlined,

    /// <summary>Text button; unavailable for ToggleButton.</summary>
    Text
}

/// <summary>Material button size roles.</summary>
public enum ButtonSize
{
    /// <summary>Extra small size.</summary>
    ExtraSmall,

    /// <summary>Small size.</summary>
    Small,

    /// <summary>Medium size.</summary>
    Medium,

    /// <summary>Large size.</summary>
    Large,

    /// <summary>Extra large size.</summary>
    ExtraLarge
}

/// <summary>Material button container shapes.</summary>
public enum ButtonShape
{
    /// <summary>Round container.</summary>
    Round,

    /// <summary>Square container.</summary>
    Square
}

/// <summary>Applies Material button options to native Button and ToggleButton controls.</summary>
public static class ButtonAssist
{
    static ButtonAssist()
    {
        VariantProperty.Changed.AddClassHandler<ToggleButton>((button, args) =>
        {
            if (args.NewValue is ButtonVariant.Text)
            {
                throw new ArgumentOutOfRangeException(nameof(VariantProperty),
                    "The Text variant is not supported by Material ToggleButton.");
            }
        });
    }

    /// <summary>Identifies the Material button icon property.</summary>
    public static readonly AttachedProperty<object?> IconProperty =
        AvaloniaProperty.RegisterAttached<Button, object?>(
            "Icon", typeof(ButtonAssist));

    /// <summary>Sets the Material button icon.</summary>
    public static void SetIcon(Button b, object? value) => b.SetValue(IconProperty, value);

    /// <summary>Gets the Material button icon.</summary>
    public static object? GetIcon(Button b) => b.GetValue(IconProperty);

    /// <summary>Identifies the Material button variant property.</summary>
    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVariant>(
            "Variant", typeof(ButtonAssist), defaultValue: ButtonVariant.Filled);

    /// <summary>Sets the Material button variant.</summary>
    public static void SetVariant(Button b, ButtonVariant value) => b.SetValue(VariantProperty, value);

    /// <summary>Gets the Material button variant.</summary>
    public static ButtonVariant GetVariant(Button b) => b.GetValue(VariantProperty);

    /// <summary>Identifies the Material button size property.</summary>
    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonSize>(
            "Size", typeof(ButtonAssist), defaultValue: ButtonSize.Small);

    /// <summary>Sets the Material button size.</summary>
    public static void SetSize(Button b, ButtonSize value) => b.SetValue(SizeProperty, value);

    /// <summary>Gets the Material button size.</summary>
    public static ButtonSize GetSize(Button b) => b.GetValue(SizeProperty);

    /// <summary>Identifies the Material button shape property.</summary>
    public static readonly AttachedProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonShape>(
            "Shape", typeof(ButtonAssist), defaultValue: ButtonShape.Round);

    /// <summary>Sets the Material button shape.</summary>
    public static void SetShape(Button b, ButtonShape value) => b.SetValue(ShapeProperty, value);

    /// <summary>Gets the Material button shape.</summary>
    public static ButtonShape GetShape(Button b) => b.GetValue(ShapeProperty);
}