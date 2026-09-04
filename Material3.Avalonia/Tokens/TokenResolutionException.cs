namespace Material3.Avalonia.Tokens;

/// <summary>
/// Describes a token resolution failure with the resolved token path.
/// </summary>
public sealed class TokenResolutionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenResolutionException" /> class.
    /// </summary>
    public TokenResolutionException(string message, IReadOnlyList<object> path)
        : base(message)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the token resolution path that led to the failure.
    /// </summary>
    public IReadOnlyList<object> Path { get; }
}