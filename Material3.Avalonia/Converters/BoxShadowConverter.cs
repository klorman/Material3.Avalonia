using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Material3.Avalonia.Controls.Primitives;

namespace Material3.Avalonia.Converters;

internal sealed class BoxShadowConverter : IMultiValueConverter
{
    public static BoxShadowConverter Instance { get; } = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            throw new InvalidOperationException("DynamicBoxShadow expects shadows and a color.");
        foreach (var value in values)
        {
            if (value is BindingNotification notification)
                return notification;
            if (value is UnsetValueType)
                return BindingOperations.DoNothing;
        }

        return values[0] is BoxShadows shadows && values[1] is Color color
            ? BoxShadowColors.WithColor(shadows, color)
            : new BindingNotification(
                new InvalidOperationException("DynamicBoxShadow requires BoxShadows and Color values."),
                BindingErrorType.Error);
    }
}