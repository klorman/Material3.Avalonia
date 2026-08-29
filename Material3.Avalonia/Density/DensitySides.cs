namespace Material3.Avalonia.Density;

/// <summary>
/// Defines which vertical sides receive a density adjustment.
/// </summary>
public enum DensitySides
{
    /// <summary>
    /// Applies the density adjustment equally to the top and bottom sides.
    /// </summary>
    Vertical,

    /// <summary>
    /// Applies the density adjustment to the top side.
    /// </summary>
    Top,

    /// <summary>
    /// Applies the density adjustment to the bottom side.
    /// </summary>
    Bottom
}
