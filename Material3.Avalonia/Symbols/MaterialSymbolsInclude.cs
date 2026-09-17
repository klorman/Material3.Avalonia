namespace Material3.Avalonia.Symbols;

/// <summary>Declares Material Symbols that cannot be discovered from static AXAML usage.</summary>
public sealed class MaterialSymbolsInclude
{
    /// <summary>Gets or sets a comma-separated list of symbols, or <c>All</c>.</summary>
    public string Symbols { get; set; } = string.Empty;

    /// <summary>Gets or sets the font families embedded for the declared symbols.</summary>
    public MaterialSymbolFamilies Families { get; set; } = MaterialSymbolFamilies.All;
}