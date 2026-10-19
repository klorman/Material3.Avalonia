using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class RadioGroupNavigation
{
    private static readonly ConditionalWeakTable<Control, RadioGroupNavigation> States = new();
    private readonly Control _container;
    private IDisposable? _tabNavigation;
    private TopLevel? _root;
    private bool _inside;

    private RadioGroupNavigation(Control container)
    {
        _container = container;
        container.AttachedToVisualTree += OnAttached;
        container.DetachedFromVisualTree += OnDetached;
        container.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    public static void Update(Control container)
    {
        var state = States.GetValue(container, c => new RadioGroupNavigation(c));
        state.Detach();
        if (RadioButtonAssist.GetGroupNavigation(container)) state.Attach();
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (RadioButtonAssist.GetGroupNavigation(_container)) Attach();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Detach();

    private void Attach()
    {
        if (_root is not null || TopLevel.GetTopLevel(_container) is not { } root) return;
        _root = root;
        _tabNavigation = _container.SetValue(KeyboardNavigation.TabNavigationProperty,
            KeyboardNavigationMode.Once, BindingPriority.Style);
        root.AddHandler(InputElement.GotFocusEvent, OnFocus, RoutingStrategies.Bubble, true);
    }

    private void Detach()
    {
        _root?.RemoveHandler(InputElement.GotFocusEvent, OnFocus);
        _root = null;
        _inside = false;
        _tabNavigation?.Dispose();
        _tabNavigation = null;
    }

    private List<RadioButton> AvailableRadios() => _container.GetLogicalDescendants().OfType<RadioButton>()
        .Where(radio => radio.IsEffectivelyEnabled && radio.IsEffectivelyVisible && radio.Focusable &&
                        radio.IsAttachedToVisualTree() &&
                        radio.GetLogicalAncestors().OfType<Control>()
                            .FirstOrDefault(RadioButtonAssist.GetGroupNavigation) == _container)
        .ToList();

    private void OnFocus(object? sender, FocusChangedEventArgs e)
    {
        var inside = e.Source is Visual visual &&
                     (visual == _container || _container.IsVisualAncestorOf(visual));
        var entering = inside && !_inside;
        _inside = inside;
        if (!entering || e.NavigationMethod != NavigationMethod.Tab || e.Source is not RadioButton) return;
        var radios = AvailableRadios();
        if (!radios.Contains((RadioButton)e.Source)) return;
        var target = radios.FirstOrDefault(radio => radio.IsChecked == true) ??
                     (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? radios.LastOrDefault() : radios.FirstOrDefault());
        if (target is not null && target != e.Source)
            target.Focus(NavigationMethod.Tab, e.KeyModifiers);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_root is null || e.Handled || e.Source is not RadioButton current ||
            e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Up or Key.Down or Key.Left or Key.Right)) return;
        var radios = AvailableRadios().Where(radio => SameGroup(current, radio)).ToList();
        var index = radios.IndexOf(current);
        if (index < 0) return;
        var forward = e.Key is Key.Down or Key.Right;
        if (e.Key is Key.Left or Key.Right && _container.FlowDirection == FlowDirection.RightToLeft)
            forward = !forward;
        var target = radios[(index + (forward ? 1 : radios.Count - 1)) % radios.Count];
        e.Handled = true;
        if (target.Focus(NavigationMethod.Directional))
            target.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
    }

    private static bool SameGroup(RadioButton left, RadioButton right) => string.IsNullOrEmpty(left.GroupName)
        ? string.IsNullOrEmpty(right.GroupName) && left.Parent == right.Parent
        : left.GroupName == right.GroupName;
}