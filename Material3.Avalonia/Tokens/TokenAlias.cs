namespace Material3.Avalonia.Tokens;

/// <summary>
/// Represents a Material token resource that redirects to another resource key.
/// </summary>
public sealed class TokenAlias
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenAlias" /> class.
    /// </summary>
    public TokenAlias()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenAlias" /> class with the target resource key.
    /// </summary>
    public TokenAlias(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>
    /// Gets or sets the resource key this token aliases.
    /// </summary>
    public object? ResourceKey { get; set; }
}