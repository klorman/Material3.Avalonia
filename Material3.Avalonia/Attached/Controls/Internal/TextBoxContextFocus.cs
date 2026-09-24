using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class TextBoxContextFocus
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(TextBoxContextFocus));

    static TextBoxContextFocus()
    {
        InputElement.PointerPressedEvent.AddClassHandler<TextBox>((textBox, e) =>
        {
            if (!GetIsEnabled(textBox) || e.Handled || !textBox.IsEffectivelyEnabled || !textBox.Focusable ||
                e.Pointer.Type != PointerType.Mouse)
                return;

            var point = e.GetCurrentPoint(textBox);
            if (point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
                return;

            textBox.Focus(NavigationMethod.Pointer, e.KeyModifiers);
        }, RoutingStrategies.Tunnel);
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
}
