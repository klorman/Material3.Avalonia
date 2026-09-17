namespace Material3.Avalonia.Symbols;

/// <summary>Material Symbol families selected for build-time embedding.</summary>
[Flags]
public enum MaterialSymbolFamilies
{
    /// <summary>No families.</summary>
    None = 0,

    /// <summary>The outlined family.</summary>
    Outlined = 1 << 0,

    /// <summary>The rounded family.</summary>
    Rounded = 1 << 1,

    /// <summary>The sharp family.</summary>
    Sharp = 1 << 2,

    /// <summary>All families.</summary>
    All = Outlined | Rounded | Sharp
}