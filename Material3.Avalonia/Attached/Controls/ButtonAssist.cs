using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material3.Avalonia.Controls;

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

    /// <summary>Text button.</summary>
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

/// <summary>Applies Material button options to native Button controls.</summary>
public static class ButtonAssist
{
    static ButtonAssist()
    {
        VariantProperty.Changed.AddClassHandler<Button>((button, _) => ValidateTarget(button));
        SizeProperty.Changed.AddClassHandler<Button>((button, _) => ValidateTarget(button));
        ShapeProperty.Changed.AddClassHandler<Button>((button, _) => ValidateTarget(button));
        IconProperty.Changed.AddClassHandler<Button>((button, _) => ValidateTarget(button));
    }

    /// <summary>Identifies the Material button icon property.</summary>
    public static readonly AttachedProperty<object?> IconProperty =
        AvaloniaProperty.RegisterAttached<Button, object?>(
            "Icon", typeof(ButtonAssist));

    /// <summary>Sets the Material button icon.</summary>
    public static void SetIcon(Button b, object? value)
    {
        ValidateTarget(b);
        b.SetValue(IconProperty, value);
    }

    /// <summary>Gets the Material button icon.</summary>
    public static object? GetIcon(Button b) => b.GetValue(IconProperty);

    /// <summary>Identifies the Material button variant property.</summary>
    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVariant>(
            "Variant", typeof(ButtonAssist), defaultValue: ButtonVariant.Filled);

    /// <summary>Sets the Material button variant.</summary>
    public static void SetVariant(Button b, ButtonVariant value)
    {
        ValidateTarget(b);
        b.SetValue(VariantProperty, value);
    }

    /// <summary>Gets the Material button variant.</summary>
    public static ButtonVariant GetVariant(Button b) => b.GetValue(VariantProperty);

    /// <summary>Identifies the Material button size property.</summary>
    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonSize>(
            "Size", typeof(ButtonAssist), defaultValue: ButtonSize.Small);

    /// <summary>Sets the Material button size.</summary>
    public static void SetSize(Button b, ButtonSize value)
    {
        ValidateTarget(b);
        b.SetValue(SizeProperty, value);
    }

    /// <summary>Gets the Material button size.</summary>
    public static ButtonSize GetSize(Button b) => b.GetValue(SizeProperty);

    /// <summary>Identifies the Material button shape property.</summary>
    public static readonly AttachedProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonShape>(
            "Shape", typeof(ButtonAssist), defaultValue: ButtonShape.Round);

    /// <summary>Sets the Material button shape.</summary>
    public static void SetShape(Button b, ButtonShape value)
    {
        ValidateTarget(b);
        b.SetValue(ShapeProperty, value);
    }

    /// <summary>Gets the Material button shape.</summary>
    public static ButtonShape GetShape(Button b) => b.GetValue(ShapeProperty);

    private static void ValidateTarget(Button button)
    {
        if (button is IconToggleButton)
        {
            throw new ArgumentException(
                "ButtonAssist cannot be used with IconToggleButton.",
                nameof(button));
        }

        if (button is ToggleButton)
        {
            throw new ArgumentException(
                "ButtonAssist can only be used with Button and its ordinary descendants.",
                nameof(button));
        }

        if (button is IconButton)
        {
            throw new ArgumentException(
                "ButtonAssist cannot be used with IconButton.",
                nameof(button));
        }
    }
}