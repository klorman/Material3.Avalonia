namespace Material3.Avalonia.Density;

/// <summary>
/// Defines Material density levels for density-sensitive component geometry.
/// </summary>
public enum MaterialDensity
{
    /// <summary>
    /// Uses the base component geometry.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Reduces density-sensitive geometry by one density level.
    /// </summary>
    Dense1 = -1,

    /// <summary>
    /// Reduces density-sensitive geometry by two density levels.
    /// </summary>
    Dense2 = -2,

    /// <summary>
    /// Reduces density-sensitive geometry by three density levels.
    /// </summary>
    Dense3 = -3,

    /// <summary>
    /// Reduces density-sensitive geometry by four density levels.
    /// </summary>
    Dense4 = -4,

    /// <summary>
    /// Reduces density-sensitive geometry by five density levels.
    /// </summary>
    Dense5 = -5
}
