using Avalonia;
using Avalonia.Controls.Primitives;
using Material3.Avalonia.Attached.Controls;

namespace Material3.Avalonia.Controls;

/// <summary>A Material icon toggle button that preserves native Avalonia toggle behavior.</summary>
public class IconToggleButton : ToggleButton
{
    /// <summary>Identifies the <see cref="IconButtonVariant"/> property.</summary>
    public static readonly StyledProperty<IconButtonVariant> VariantProperty =
        IconButton.VariantProperty.AddOwner<IconToggleButton>();

    /// <summary>Identifies the <see cref="ButtonSize"/> property.</summary>
    public static readonly StyledProperty<ButtonSize> SizeProperty =
        IconButton.SizeProperty.AddOwner<IconToggleButton>();

    /// <summary>Identifies the <see cref="ButtonShape"/> property.</summary>
    public static readonly StyledProperty<ButtonShape> ShapeProperty =
        IconButton.ShapeProperty.AddOwner<IconToggleButton>();

    /// <summary>Identifies the <see cref="IconButtonWidth"/> property.</summary>
    public static readonly StyledProperty<IconButtonWidth> WidthModeProperty =
        IconButton.WidthModeProperty.AddOwner<IconToggleButton>();

    /// <summary>Gets or sets the icon button color variant.</summary>
    public IconButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Gets or sets the icon button size.</summary>
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Gets or sets the icon button container shape.</summary>
    public ButtonShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    /// <summary>Gets or sets the icon button horizontal width mode.</summary>
    public IconButtonWidth WidthMode
    {
        get => GetValue(WidthModeProperty);
        set => SetValue(WidthModeProperty, value);
    }
}