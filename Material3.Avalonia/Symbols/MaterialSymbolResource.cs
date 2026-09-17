using Avalonia.Markup.Xaml;

namespace Material3.Avalonia.Symbols;

/// <summary>Declares a Material Symbol resource and the font families required in the built application.</summary>
public sealed class MaterialSymbolResource : MarkupExtension
{
    /// <summary>Gets or sets the symbol exposed by this resource.</summary>
    public MaterialSymbol Symbol { get; set; }

    /// <summary>Gets or sets the font families embedded for this symbol.</summary>
    public MaterialSymbolFamilies Families { get; set; } = MaterialSymbolFamilies.All;

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => Symbol;
}