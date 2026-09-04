using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Markup;

/// <summary>
/// Creates a binding that resolves Material token aliases through Avalonia resources.
/// </summary>
public sealed class TokenExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenExtension" /> class.
    /// </summary>
    public TokenExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenExtension" /> class with a root resource key.
    /// </summary>
    public TokenExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>
    /// Gets or sets the root token resource key.
    /// </summary>
    public object? ResourceKey { get; set; }

    /// <summary>
    /// Provides a binding that resolves the root token and any aliases from the target resource scope.
    /// </summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (ResourceKey is null) throw new InvalidOperationException("Token requires a resource key.");

        return TokenBindingFactory.Create(ResourceKey, serviceProvider: serviceProvider);
    }
}