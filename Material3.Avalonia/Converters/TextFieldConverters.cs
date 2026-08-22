using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3.Avalonia.Converters;

/// <summary>
/// Resolves the Material supporting/error text slot.
/// </summary>
public sealed class TextFieldSupportingTextConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 5)
        {
            return null;
        }

        var isManualError = values[0] is true;
        var errorText = NormalizeText(values[1]);
        var hasNativeErrors = values[2] is true;
        var supportingText = NormalizeText(values[4]);

        if (isManualError)
        {
            return errorText ?? supportingText;
        }

        if (hasNativeErrors)
        {
            return GetFirstValidationMessage(values[3]) ?? supportingText;
        }

        return supportingText;
    }

    private static string? GetFirstValidationMessage(object? value)
    {
        if (IsUnset(value) || value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return NormalizeText(text);
        }

        if (value is IEnumerable errors)
        {
            foreach (var error in errors)
            {
                var message = FormatValidationError(error);
                if (message is not null)
                {
                    return message;
                }
            }

            return null;
        }

        return FormatValidationError(value);
    }

    private static string? FormatValidationError(object? error)
    {
        if (IsUnset(error) || error is null)
        {
            return null;
        }

        return error switch
        {
            string text => NormalizeText(text),
            Exception exception => NormalizeText(exception.Message),
            _ => NormalizeText(error.ToString())
        };
    }

    private static string? NormalizeText(object? value)
    {
        if (IsUnset(value) || value is null)
        {
            return null;
        }

        var text = value as string ?? value.ToString();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static bool IsUnset(object? value)
        => ReferenceEquals(value, AvaloniaProperty.UnsetValue);
}

/// <summary>
/// Builds the visual Material text-field label.
/// </summary>
public sealed class TextFieldLabelConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 2 || ReferenceEquals(values[0], AvaloniaProperty.UnsetValue))
        {
            return null;
        }

        var label = values[0] as string;
        if (string.IsNullOrEmpty(label))
        {
            return null;
        }

        return values[1] is true ? $"{label} *" : label;
    }
}

/// <summary>
/// Reserves the trailing text-field section inside multiline scroll content.
/// </summary>
public sealed class TextFieldScrollPaddingConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 3 || !IsMultiline(values[1], values[2]))
        {
            return new Thickness(0);
        }

        return new Thickness(0, 0, GetWidth(values[0]), 0);
    }

    private static bool IsMultiline(object? acceptsReturn, object? textWrapping) =>
        acceptsReturn is true ||
        textWrapping is TextWrapping.Wrap or TextWrapping.WrapWithOverflow;

    private static double GetWidth(object? value)
    {
        if (value is not double width || !double.IsFinite(width) || width <= 0)
        {
            return 0;
        }

        return width;
    }
}
