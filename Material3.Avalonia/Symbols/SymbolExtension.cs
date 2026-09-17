using Avalonia.Markup.Xaml;

namespace Material3.Avalonia.Symbols;

/// <summary>Returns a Material Symbol and declares the font families required by the application.</summary>
public sealed class SymbolExtension : MarkupExtension
{
    /// <summary>Creates an extension for the specified symbol.</summary>
    public SymbolExtension(MaterialSymbol symbol)
    {
        Symbol = symbol;
    }

    /// <summary>Gets the symbol returned by the extension.</summary>
    public MaterialSymbol Symbol { get; }

    /// <summary>Gets or sets the font families embedded for this symbol.</summary>
    public MaterialSymbolFamilies Families { get; set; } = MaterialSymbolFamilies.All;

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => Symbol;
}