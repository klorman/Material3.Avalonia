using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Converters;

internal sealed class DynamicCornerRadiusConverter : IMultiValueConverter
{
    public static DynamicCornerRadiusConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2 || values[1] is not Rect bounds)
            return BindingOperations.DoNothing;

        return values[0] switch
        {
            CornerRadius radius => radius,
            FullCornerRadiusToken => new CornerRadius(0.5 * Math.Min(bounds.Width, bounds.Height)),
            BindingNotification notification => notification,
            UnsetValueType => BindingOperations.DoNothing,
            null => new BindingNotification(
                new InvalidOperationException("Corner radius token resolved to null."), BindingErrorType.Error),
            var value => new BindingNotification(
                new InvalidOperationException(
                    $"Corner radius token resolved to '{value.GetType().FullName}', expected CornerRadius or Full."),
                BindingErrorType.Error)
        };
    }
}