using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Material3.Avalonia.Converters;

internal sealed class DividerInsetConverter : IMultiValueConverter
{
    public static DividerInsetConverter Instance { get; } = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 3 || values[0] is not Orientation orientation ||
            values[1] is not double start || values[2] is not double end)
            return BindingOperations.DoNothing;

        // Avalonia mirrors the template in RTL; swapping horizontal insets here would mirror twice.
        return orientation == Orientation.Horizontal
            ? new Thickness(start, 0, end, 0)
            : new Thickness(0, start, 0, end);
    }
}