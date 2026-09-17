namespace Material3.Avalonia.Symbols;

/// <summary>A searchable symbol and its official Google name.</summary>
public sealed record SymbolEntry(MaterialSymbol Symbol, string Name);

/// <summary>The design family of a Material Symbol.</summary>
public enum SymbolStyle
{
    /// <summary>The outlined family.</summary>
    Outlined,

    /// <summary>The rounded family.</summary>
    Rounded,

    /// <summary>The sharp family.</summary>
    Sharp
}