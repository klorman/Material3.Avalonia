using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Material3.Avalonia.Converters;

internal sealed class ThicknessConverter : IMultiValueConverter
{
    public static ThicknessConverter Instance { get; } = new();

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count is not (1 or 2 or 4))
            throw new InvalidOperationException("ThicknessConverter expects 1, 2, or 4 values.");

        if (values.Any(IsUnset))
            return BindingOperations.DoNothing;

        return values.Count switch
        {
            1 => new Thickness(Get(values[0])),
            2 => new Thickness(Get(values[0]), Get(values[1])),
            4 => new Thickness(Get(values[0]), Get(values[1]), Get(values[2]), Get(values[3])),
            _ => throw new InvalidOperationException("ThicknessConverter expects 1, 2, or 4 values.")
        };
    }

    private static double Get(object? value)
    {
        var result = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => throw new InvalidOperationException("ThicknessConverter values must be numeric.")
        };

        if (!double.IsFinite(result)) throw new InvalidOperationException("ThicknessConverter values must be finite.");

        return result;
    }

    private static bool IsUnset(object? value)
    {
        return value is UnsetValueType;
    }
}