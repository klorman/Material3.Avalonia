using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class HoverState
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(HoverState));

    private static readonly ConditionalWeakTable<Control, HoverState> States = new();
    private readonly Control _control;
    private bool _inside;
    private bool _touch;
    private bool _retained;

    static HoverState()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, _) =>
            States.GetValue(control, target => new HoverState(target)).Update());
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    public static void Retain(Control control, bool value)
    {
        if (!value && !States.TryGetValue(control, out _))
            return;
        var state = States.GetValue(control, target => new HoverState(target));
        state._retained = value;
        state.Update();
    }

    private HoverState(Control control)
    {
        _control = control;
        control.PointerEntered += OnPointer;
        control.PointerExited += OnPointer;
        control.AddHandler(InputElement.PointerMovedEvent, OnPointer, handledEventsToo: true);
        control.AddHandler(InputElement.PointerPressedEvent, OnPointer, handledEventsToo: true);
        control.AddHandler(InputElement.PointerReleasedEvent, OnPointer, handledEventsToo: true);
        control.PropertyChanged += OnPropertyChanged;
        control.DetachedFromVisualTree += OnDetached;
    }

    private void OnPointer(object? sender, PointerEventArgs e)
    {
        _touch = e.Pointer.Type == PointerType.Touch;
        var root = TopLevel.GetTopLevel(_control);
        var hit = !_touch && root is not null &&
                  (e.RoutedEvent != InputElement.PointerExitedEvent || e.Pointer.Captured is not null)
            ? root.InputHitTest(e.GetPosition(root)) as Visual
            : null;
        _inside = hit == _control || hit?.GetVisualAncestors().Contains(_control) == true;
        Update();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty ||
            e.Property == Visual.IsVisibleProperty)
            Update();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _inside = _retained = false;
        _control.Classes.Remove("m3-hovered");
    }

    private void Update()
    {
        var hovered = _control.IsEffectivelyEnabled && _control.IsVisible && _control.IsAttachedToVisualTree() &&
                      (GetIsEnabled(_control) || _retained) &&
                      (_retained || !_touch && _inside);
        _control.Classes.Set("m3-hovered", hovered);
    }
}