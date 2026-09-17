using Avalonia;
using Avalonia.Media;

namespace Material3.Avalonia.Controls.Icons;

/// <summary>A geometry with an optional original design viewport.</summary>
public sealed class GeometryIconSource : AvaloniaObject
{
    /// <summary>Identifies the geometry property.</summary>
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<GeometryIconSource, Geometry?>(nameof(Data));

    /// <summary>Identifies the original viewport property.</summary>
    public static readonly StyledProperty<Rect?> ViewBoxProperty =
        AvaloniaProperty.Register<GeometryIconSource, Rect?>(nameof(ViewBox));

    /// <summary>Gets or sets the icon geometry.</summary>
    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>Gets or sets the original viewport; null fits the geometry bounds.</summary>
    public Rect? ViewBox
    {
        get => GetValue(ViewBoxProperty);
        set => SetValue(ViewBoxProperty, value);
    }
}

/// <summary>The visual transition used when an icon source changes.</summary>
public enum IconChangeAnimation
{
    /// <summary>Changes the source immediately.</summary>
    None,

    /// <summary>Shrinks the old icon and expands the new icon around its center.</summary>
    Scale
}