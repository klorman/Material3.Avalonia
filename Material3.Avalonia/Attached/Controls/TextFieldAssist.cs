using Avalonia;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>
/// Defines the Material text-field variant.
/// </summary>
public enum TextFieldVariant
{
    /// <summary>
    /// Filled text field.
    /// </summary>
    Filled,

    /// <summary>
    /// Outlined text field.
    /// </summary>
    Outlined
}

/// <summary>
/// Provides Material attached properties for <see cref="TextBox" />.
/// </summary>
public static class TextFieldAssist
{
    private const string HasLabelClass = "m3-text-field-has-label";
    private const string ManualErrorClass = "m3-text-field-manual-error";

    static TextFieldAssist()
    {
        // Rooted selectors are more reliable than template-internal class bindings.
        LabelProperty.Changed.AddClassHandler<TextBox>(
            static (textBox, change) =>
                SetPresentationClass(
                    textBox,
                    HasLabelClass,
                    !string.IsNullOrEmpty(change.GetNewValue<string?>())));

        IsErrorProperty.Changed.AddClassHandler<TextBox>(
            static (textBox, change) =>
                SetPresentationClass(
                    textBox,
                    ManualErrorClass,
                    change.GetNewValue<bool>()));
    }

    private static void SetPresentationClass(TextBox textBox, string className, bool value)
    {
        if (value)
        {
            if (!textBox.Classes.Contains(className))
                textBox.Classes.Add(className);
        }
        else
        {
            textBox.Classes.Remove(className);
        }
    }

    /// <summary>
    /// Defines the Material text-field variant.
    /// </summary>
    public static readonly AttachedProperty<TextFieldVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<TextBox, TextFieldVariant>(
            "Variant",
            typeof(TextFieldAssist),
            defaultValue: TextFieldVariant.Filled);

    /// <summary>
    /// Sets the Material text-field variant.
    /// </summary>
    public static void SetVariant(TextBox textBox, TextFieldVariant value) =>
        textBox.SetValue(VariantProperty, value);

    /// <summary>
    /// Gets the Material text-field variant.
    /// </summary>
    public static TextFieldVariant GetVariant(TextBox textBox) =>
        textBox.GetValue(VariantProperty);

    /// <summary>
    /// Defines the leading Material icon content.
    /// </summary>
    public static readonly AttachedProperty<object?> LeadingIconProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>(
            "LeadingIcon",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets the leading Material icon content.
    /// </summary>
    public static void SetLeadingIcon(TextBox textBox, object? value) =>
        textBox.SetValue(LeadingIconProperty, value);

    /// <summary>
    /// Gets the leading Material icon content.
    /// </summary>
    public static object? GetLeadingIcon(TextBox textBox) =>
        textBox.GetValue(LeadingIconProperty);

    /// <summary>
    /// Defines the trailing Material icon content.
    /// </summary>
    public static readonly AttachedProperty<object?> TrailingIconProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>(
            "TrailingIcon",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets the trailing Material icon content.
    /// </summary>
    public static void SetTrailingIcon(TextBox textBox, object? value) =>
        textBox.SetValue(TrailingIconProperty, value);

    /// <summary>
    /// Gets the trailing Material icon content.
    /// </summary>
    public static object? GetTrailingIcon(TextBox textBox) =>
        textBox.GetValue(TrailingIconProperty);

    /// <summary>
    /// Defines the floating label text.
    /// </summary>
    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>(
            "Label",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets the floating label text.
    /// </summary>
    public static void SetLabel(TextBox textBox, string? value) =>
        textBox.SetValue(LabelProperty, value);

    /// <summary>
    /// Gets the floating label text.
    /// </summary>
    public static string? GetLabel(TextBox textBox) =>
        textBox.GetValue(LabelProperty);

    /// <summary>
    /// Defines text shown before the input text.
    /// </summary>
    public static readonly AttachedProperty<string?> PrefixTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>(
            "PrefixText",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets text shown before the input text.
    /// </summary>
    public static void SetPrefixText(TextBox textBox, string? value) =>
        textBox.SetValue(PrefixTextProperty, value);

    /// <summary>
    /// Gets text shown before the input text.
    /// </summary>
    public static string? GetPrefixText(TextBox textBox) =>
        textBox.GetValue(PrefixTextProperty);

    /// <summary>
    /// Defines text shown after the input text.
    /// </summary>
    public static readonly AttachedProperty<string?> SuffixTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>(
            "SuffixText",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets text shown after the input text.
    /// </summary>
    public static void SetSuffixText(TextBox textBox, string? value) =>
        textBox.SetValue(SuffixTextProperty, value);

    /// <summary>
    /// Gets text shown after the input text.
    /// </summary>
    public static string? GetSuffixText(TextBox textBox) =>
        textBox.GetValue(SuffixTextProperty);

    /// <summary>
    /// Defines supporting text below the field.
    /// </summary>
    public static readonly AttachedProperty<string?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>(
            "SupportingText",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets supporting text below the field.
    /// </summary>
    public static void SetSupportingText(TextBox textBox, string? value) =>
        textBox.SetValue(SupportingTextProperty, value);

    /// <summary>
    /// Gets supporting text below the field.
    /// </summary>
    public static string? GetSupportingText(TextBox textBox) =>
        textBox.GetValue(SupportingTextProperty);

    /// <summary>
    /// Defines manual error text below the field.
    /// </summary>
    public static readonly AttachedProperty<string?> ErrorTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>(
            "ErrorText",
            typeof(TextFieldAssist));

    /// <summary>
    /// Sets manual error text below the field.
    /// </summary>
    public static void SetErrorText(TextBox textBox, string? value) =>
        textBox.SetValue(ErrorTextProperty, value);

    /// <summary>
    /// Gets manual error text below the field.
    /// </summary>
    public static string? GetErrorText(TextBox textBox) =>
        textBox.GetValue(ErrorTextProperty);

    /// <summary>
    /// Defines whether the field is in manual error state.
    /// </summary>
    public static readonly AttachedProperty<bool> IsErrorProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "IsError",
            typeof(TextFieldAssist),
            defaultValue: false);

    /// <summary>
    /// Sets whether the field is in manual error state.
    /// </summary>
    public static void SetIsError(TextBox textBox, bool value) =>
        textBox.SetValue(IsErrorProperty, value);

    /// <summary>
    /// Gets whether the field is in manual error state.
    /// </summary>
    public static bool GetIsError(TextBox textBox) =>
        textBox.GetValue(IsErrorProperty);

    /// <summary>
    /// Defines whether the label shows a required indicator.
    /// </summary>
    public static readonly AttachedProperty<bool> IsRequiredProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "IsRequired",
            typeof(TextFieldAssist),
            defaultValue: false);

    /// <summary>
    /// Sets whether the label shows a required indicator.
    /// </summary>
    public static void SetIsRequired(TextBox textBox, bool value) =>
        textBox.SetValue(IsRequiredProperty, value);

    /// <summary>
    /// Gets whether the label shows a required indicator.
    /// </summary>
    public static bool GetIsRequired(TextBox textBox) =>
        textBox.GetValue(IsRequiredProperty);
}
