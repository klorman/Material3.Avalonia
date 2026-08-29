using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Converters;

internal sealed class DensityScalarConverter : IMultiValueConverter
{
    public static DensityScalarConverter Instance { get; } = new();

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count != 2)
            throw new InvalidOperationException("DensityScalarConverter expects base value and density values.");

        if (values.Any(DensityConverterHelpers.IsUnset)) return BindingOperations.DoNothing;

        var baseValue = DensityConverterHelpers.GetFiniteDouble(values[0], "DensityScalar base value");
        var density = DensityConverterHelpers.GetDensity(values[1]);
        var options = DensityConverterHelpers.GetScalarOptions(parameter);

        return Math.Max(0, baseValue + DensityCalculator.GetDelta(density, options.MostDense) * options.DeltaScale);
    }
}

internal sealed class DensityThicknessConverter : IMultiValueConverter
{
    public static DensityThicknessConverter Instance { get; } = new();

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count != 2)
            throw new InvalidOperationException("DensityThicknessConverter expects base thickness and density values.");

        if (values.Any(DensityConverterHelpers.IsUnset)) return BindingOperations.DoNothing;

        if (values[0] is not Thickness baseValue)
            throw new InvalidOperationException("DensityThicknessConverter base value must be a Thickness.");

        var density = DensityConverterHelpers.GetDensity(values[1]);
        var options = DensityConverterHelpers.GetThicknessOptions(parameter);
        var delta = DensityCalculator.GetDelta(density, options.MostDense);

        var left = DensityConverterHelpers.GetFiniteDouble(baseValue.Left, "DensityThickness left value");
        var top = DensityConverterHelpers.GetFiniteDouble(baseValue.Top, "DensityThickness top value");
        var right = DensityConverterHelpers.GetFiniteDouble(baseValue.Right, "DensityThickness right value");
        var bottom = DensityConverterHelpers.GetFiniteDouble(baseValue.Bottom, "DensityThickness bottom value");

        switch (options.Sides)
        {
            case DensitySides.Vertical:
                top += delta / 2;
                bottom += delta / 2;
                break;
            case DensitySides.Top:
                top += delta;
                break;
            case DensitySides.Bottom:
                bottom += delta;
                break;
            default:
                throw new InvalidOperationException(
                    "DensityThicknessConverter received an unsupported side selection.");
        }

        return new Thickness(
            Math.Max(0, left),
            Math.Max(0, top),
            Math.Max(0, right),
            Math.Max(0, bottom));
    }
}

internal readonly record struct DensityScalarOptions(MaterialDensity MostDense, double DeltaScale);

internal readonly record struct DensityThicknessOptions(DensitySides Sides, MaterialDensity MostDense);

internal static class DensityConverterHelpers
{
    public static bool IsUnset(object? value)
    {
        return ReferenceEquals(value, AvaloniaProperty.UnsetValue) || value is UnsetValueType;
    }

    public static double GetFiniteDouble(object? value, string valueName)
    {
        var result = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => throw new InvalidOperationException($"{valueName} must be numeric.")
        };

        if (!double.IsFinite(result)) throw new InvalidOperationException($"{valueName} must be finite.");

        return result;
    }

    public static MaterialDensity GetDensity(object? value)
    {
        return value is MaterialDensity density
            ? density
            : throw new InvalidOperationException("Density value must be a MaterialDensity.");
    }

    public static DensityScalarOptions GetScalarOptions(object? value)
    {
        if (value is not DensityScalarOptions options)
            throw new InvalidOperationException("DensityScalarConverter expects density scalar options.");

        if (!double.IsFinite(options.DeltaScale) || options.DeltaScale < 0d)
            throw new InvalidOperationException("DensityScalarConverter delta scale must be finite and non-negative.");

        return options;
    }

    public static DensityThicknessOptions GetThicknessOptions(object? value)
    {
        return value is DensityThicknessOptions options
            ? options
            : throw new InvalidOperationException("DensityThicknessConverter expects density thickness options.");
    }
}
