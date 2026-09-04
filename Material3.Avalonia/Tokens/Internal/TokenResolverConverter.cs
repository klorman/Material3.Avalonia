using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tokens.Internal;

internal sealed class TokenResolverConverter : IMultiValueConverter
{
    private readonly TokenValueKind _valueKind;

    private TokenResolverConverter(TokenValueKind valueKind)
    {
        _valueKind = valueKind;
    }

    public static TokenResolverConverter TargetType { get; } = new(TokenValueKind.TargetType);

    public static TokenResolverConverter Numeric { get; } = new(TokenValueKind.Numeric);

    public static TokenResolverConverter Thickness { get; } = new(TokenValueKind.Thickness);

    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count != 2)
            throw new InvalidOperationException(
                "Token resolver expects the root resource value and resolution target.");

        if (parameter is null)
            throw new InvalidOperationException("Token resolver requires a root resource key.");

        if (values[1] is UnsetValueType)
            return BindingOperations.DoNothing;

        var target = values[1];
        var templatedParent = target is StyledElement styledElement
            ? styledElement.TemplatedParent
            : AvaloniaProperty.UnsetValue;

        try
        {
            return TokenResolver.Resolve(
                parameter,
                values[0],
                target,
                templatedParent,
                targetType,
                _valueKind);
        }
        catch (TokenResolutionException ex)
        {
            return new BindingNotification(ex, BindingErrorType.Error);
        }
    }
}