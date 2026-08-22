using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace Material3.Avalonia.Converters;

/// <summary>
/// Provides converters used by Material scrollbar templates.
/// </summary>
public static class ScrollBarConverters
{
    /// <summary>
    /// Converts a rounded container shape into a scrollbar inset.
    /// </summary>
    public static IValueConverter InsetFromCornerRadius { get; } = new ScrollBarInsetFromCornerRadiusConverter();

    /// <summary>
    /// Converts a scrollbar inset into the orientation-specific track margin.
    /// </summary>
    public static IValueConverter TrackMargin { get; } = new ScrollBarTrackMarginConverter();
}

/// <summary>
/// Converts a rounded scroll container shape into scrollbar inset values.
/// </summary>
public sealed class ScrollBarInsetFromCornerRadiusConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CornerRadius cornerRadius)
            return new Thickness(0);

        return new Thickness(
            cornerRadius.BottomLeft,
            cornerRadius.TopRight,
            cornerRadius.BottomRight,
            cornerRadius.BottomRight);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

/// <summary>
/// Converts scrollbar inset values into the actual track margin for one scrollbar orientation.
/// </summary>
public sealed class ScrollBarTrackMarginConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Thickness inset)
            return new Thickness(0);

        var orientation = Orientation.Vertical;
        if (parameter is Orientation parameterOrientation)
        {
            orientation = parameterOrientation;
        }
        else if (parameter is string text && Enum.TryParse(text, ignoreCase: true, out Orientation parsedOrientation))
        {
            orientation = parsedOrientation;
        }

        return orientation == Orientation.Horizontal
            ? new Thickness(inset.Left, 0, inset.Right, 0)
            : new Thickness(0, inset.Top, 0, inset.Bottom);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}
