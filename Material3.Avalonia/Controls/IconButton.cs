using Avalonia;
using Avalonia.Controls;
using Material3.Avalonia.Attached.Controls;

namespace Material3.Avalonia.Controls;

/// <summary>Material icon button color variants.</summary>
public enum IconButtonVariant
{
    /// <summary>Transparent, low-emphasis container.</summary>
    Standard,

    /// <summary>Primary filled container.</summary>
    Filled,

    /// <summary>Secondary filled container.</summary>
    Tonal,

    /// <summary>Transparent container with an outline.</summary>
    Outlined
}

/// <summary>Material icon button horizontal width modes.</summary>
public enum IconButtonWidth
{
    /// <summary>Reduced horizontal spacing.</summary>
    Narrow,

    /// <summary>Standard horizontal spacing.</summary>
    Default,

    /// <summary>Increased horizontal spacing.</summary>
    Wide
}

/// <summary>A Material icon-only button that preserves native Avalonia button behavior.</summary>
public class IconButton : Button
{
    /// <summary>Identifies the <see cref="Variant"/> property.</summary>
    public static readonly StyledProperty<IconButtonVariant> VariantProperty =
        AvaloniaProperty.Register<IconButton, IconButtonVariant>(nameof(Variant), IconButtonVariant.Standard);

    /// <summary>Identifies the <see cref="Size"/> property.</summary>
    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<IconButton, ButtonSize>(nameof(Size), ButtonSize.Small);

    /// <summary>Identifies the <see cref="Shape"/> property.</summary>
    public static readonly StyledProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.Register<IconButton, ButtonShape>(nameof(Shape), ButtonShape.Round);

    /// <summary>Identifies the <see cref="WidthMode"/> property.</summary>
    public static readonly StyledProperty<IconButtonWidth> WidthModeProperty =
        AvaloniaProperty.Register<IconButton, IconButtonWidth>(nameof(WidthMode), IconButtonWidth.Default);

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