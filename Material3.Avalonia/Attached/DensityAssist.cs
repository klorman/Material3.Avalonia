using Avalonia;
using Avalonia.Styling;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Attached;

/// <summary>
/// Provides inherited Material density configuration for a control subtree.
/// </summary>
public static class DensityAssist
{
    /// <summary>
    /// Defines the inherited Material density level.
    /// </summary>
    public static readonly AttachedProperty<MaterialDensity> DensityProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, MaterialDensity>(
            "Density",
            typeof(DensityAssist),
            MaterialDensity.Default,
            true);

    /// <summary>
    /// Sets the inherited Material density level.
    /// </summary>
    public static void SetDensity(StyledElement element, MaterialDensity value)
    {
        element.SetValue(DensityProperty, value);
    }

    /// <summary>
    /// Gets the inherited Material density level.
    /// </summary>
    public static MaterialDensity GetDensity(StyledElement element)
    {
        return element.GetValue(DensityProperty);
    }
}
