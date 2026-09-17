using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class MenuBarPresentation
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Menu, bool>("IsEnabled", typeof(MenuBarPresentation));

    private static readonly ConditionalWeakTable<Menu, MenuBarPresentation> Menus = new();
    private readonly Menu _menu;
    private Control? _returnFocus;
    private int _generation;
    private Point? _lastPointerPosition;

    public static bool GetIsEnabled(Menu menu) => menu.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Menu menu, bool value) => menu.SetValue(IsEnabledProperty, value);

    static MenuBarPresentation() => IsEnabledProperty.Changed.AddClassHandler<Menu>((menu, change) =>
    {
        if (change.GetNewValue<bool>()) Menus.GetValue(menu, m => new MenuBarPresentation(m));
    });

    private MenuBarPresentation(Menu menu)
    {
        _menu = menu;
        menu.AddHandler(InputElement.GotFocusEvent, OnFocus, RoutingStrategies.Bubble, true);
        menu.AddHandler(InputElement.PointerPressedEvent, (_, _) => RememberFocus(
            TopLevel.GetTopLevel(menu)?.FocusManager?.GetFocusedElement()), RoutingStrategies.Tunnel, true);
        menu.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
        {
            var position = e.GetPosition(menu);
            if (_lastPointerPosition != position) MenuPopupPresentation.SetInputMode(menu, false);
            _lastPointerPosition = position;
        }, RoutingStrategies.Tunnel, true);
        menu.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) =>
        {
            if (menu.IsOpen) e.Handled = true;
        }, RoutingStrategies.Bubble);
        menu.Opened += (_, _) => _generation++;
        menu.Closed += OnClosed;
        menu.DetachedFromVisualTree += (_, _) =>
        {
            _generation++;
            _returnFocus = null;
        };
    }

    private bool Owns(IInputElement? element) => element is Control control &&
                                                 (control == _menu || _menu.IsLogicalAncestorOf(control));

    private void RememberFocus(IInputElement? previous)
    {
        if (!Owns(previous) && previous is Control control) _returnFocus = control;
    }

    private void OnFocus(object? sender, FocusChangedEventArgs e) => RememberFocus(e.OldFocusedElement);

    private void OnClosed(object? sender, RoutedEventArgs e)
    {
        if (e.Source != _menu) return;
        var previous = _returnFocus;
        var generation = ++_generation;
        _returnFocus = null;
        // Let the native close and the activating command finish before restoring the editor.
        Dispatcher.UIThread.Post(() =>
        {
            if (generation != _generation || _menu.IsOpen || previous?.IsAttachedToVisualTree() != true ||
                !previous.IsEffectivelyEnabled || !previous.IsEffectivelyVisible ||
                TopLevel.GetTopLevel(_menu) is not { } root ||
                root is Window { IsActive: false }) return;
            var focused = root.FocusManager?.GetFocusedElement();
            if (focused is not null && !Owns(focused)) return;
            MenuFocusRestore.SuppressAutomaticScroll(previous);
            previous.Focus();
        }, DispatcherPriority.Input);
    }
}