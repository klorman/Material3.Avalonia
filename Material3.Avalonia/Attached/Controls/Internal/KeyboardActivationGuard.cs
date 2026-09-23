using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class KeyboardActivationGuard
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("IsEnabled", typeof(KeyboardActivationGuard));

    private static readonly AttachedProperty<bool> IsEnterDownProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("IsEnterDown", typeof(KeyboardActivationGuard));

    static KeyboardActivationGuard()
    {
        InputElement.KeyDownEvent.AddClassHandler<InputElement>(OnKeyDown, RoutingStrategies.Tunnel, true);
        InputElement.KeyUpEvent.AddClassHandler<InputElement>(OnKeyUp, RoutingStrategies.Tunnel, true);
        InputElement.LostFocusEvent.AddClassHandler<InputElement>(OnLostFocus, RoutingStrategies.Bubble, true);
    }

    public static void SetIsEnabled(InputElement element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnKeyDown(InputElement element, KeyEventArgs e)
    {
        if (e.Source != element || e.Key != Key.Enter || !element.GetValue(IsEnabledProperty)) return;
        if (element.GetValue(IsEnterDownProperty))
            e.Handled = true;
        else
            element.SetValue(IsEnterDownProperty, true);
    }

    private static void OnKeyUp(InputElement element, KeyEventArgs e)
    {
        if (e.Source == element && e.Key == Key.Enter)
            element.SetValue(IsEnterDownProperty, false);
    }

    private static void OnLostFocus(InputElement element, RoutedEventArgs e)
    {
        if (e.Source == element)
            element.SetValue(IsEnterDownProperty, false);
    }
}