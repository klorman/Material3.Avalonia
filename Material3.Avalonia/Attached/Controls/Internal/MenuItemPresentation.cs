using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal enum MenuItemPosition
{
    Single,
    First,
    Middle,
    Last,
    Standalone
}

internal sealed class MenuItemPresentation
{
    internal static readonly AttachedProperty<bool> DisplayCheckedProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("DisplayChecked", typeof(MenuItemPresentation));

    internal static bool GetDisplayChecked(MenuItem item) => item.GetValue(DisplayCheckedProperty);

    internal static void SynchronizeSelection(MenuItem item)
    {
        if (!Items.TryGetValue(item, out var presentation)) return;
        presentation._selectionGeneration++;
        using var transitions = GetDisplayChecked(item) != item.IsChecked
            ? item.SetValue(Animatable.TransitionsProperty, null, BindingPriority.Animation)
            : null;
        item.SetValue(DisplayCheckedProperty, item.IsChecked);
        presentation._checkmark?.Snap();
    }

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("IsEnabled", typeof(MenuItemPresentation));

    public static readonly AttachedProperty<MenuItemPosition> PositionProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, MenuItemPosition>("Position", typeof(MenuItemPresentation));

    public static readonly AttachedProperty<Thickness> LeadingGapProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, Thickness>("LeadingGap", typeof(MenuItemPresentation));

    public static readonly AttachedProperty<Thickness> TrailingGapProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, Thickness>("TrailingGap", typeof(MenuItemPresentation));

    public static readonly AttachedProperty<double> LeadingIconSizeProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, double>("LeadingIconSize", typeof(MenuItemPresentation));

    public static readonly AttachedProperty<double> TrailingIconSizeProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, double>("TrailingIconSize", typeof(MenuItemPresentation));

    public static Thickness GetLeadingGap(MenuItem item) => item.GetValue(LeadingGapProperty);
    public static void SetLeadingGap(MenuItem item, Thickness value) => item.SetValue(LeadingGapProperty, value);
    public static Thickness GetTrailingGap(MenuItem item) => item.GetValue(TrailingGapProperty);
    public static void SetTrailingGap(MenuItem item, Thickness value) => item.SetValue(TrailingGapProperty, value);
    public static double GetLeadingIconSize(MenuItem item) => item.GetValue(LeadingIconSizeProperty);
    public static void SetLeadingIconSize(MenuItem item, double value) => item.SetValue(LeadingIconSizeProperty, value);
    public static double GetTrailingIconSize(MenuItem item) => item.GetValue(TrailingIconSizeProperty);

    public static void SetTrailingIconSize(MenuItem item, double value) =>
        item.SetValue(TrailingIconSizeProperty, value);

    public static readonly AttachedProperty<Thickness> RowSpacingProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, Thickness>("RowSpacing", typeof(MenuItemPresentation));

    public static Thickness GetRowSpacing(MenuItem item) => item.GetValue(RowSpacingProperty);
    public static void SetRowSpacing(MenuItem item, Thickness value) => item.SetValue(RowSpacingProperty, value);

    private static readonly ConditionalWeakTable<MenuItem, MenuItemPresentation> Items = new();

    internal static void RefreshMotion(MenuItem item)
    {
        if (Items.TryGetValue(item, out var presentation)) presentation.UpdateTransitions();
    }

    public static bool GetIsEnabled(MenuItem item) => item.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(MenuItem item, bool value) => item.SetValue(IsEnabledProperty, value);
    public static void SetPosition(MenuItem item, MenuItemPosition value) => item.SetValue(PositionProperty, value);

    static MenuItemPresentation() => IsEnabledProperty.Changed.AddClassHandler<MenuItem>((item, change) =>
    {
        if (change.GetNewValue<bool>()) Items.GetValue(item, i => new MenuItemPresentation(i));
    });

    private readonly MenuItem _item;
    private MenuPopupPresentation? _submenu;
    private Control? _row;
    private Popup? _popup;
    private InkRipple? _ripple;
    private StateLayer? _state;
    private IDisposable? _shape;
    private MenuItemPosition? _shapePosition;
    private bool _shapeChecked;
    private bool? _animated;
    private bool? _reduced;
    private MenuItemColors? _colors;
    private MenuCheckmarkPresentation? _checkmark;
    private IPointer? _pressedPointer;
    private TopLevel? _pressRoot;
    private int _selectionGeneration;
    private bool _spacePressed;
    private bool _dispatchingKey;

    private MenuItemPresentation(MenuItem item)
    {
        _item = item;
        item.SetValue(DisplayCheckedProperty, item.IsChecked);
        item.TemplateApplied += OnTemplate;
        item.PropertyChanged += OnChanged;
        item.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
        item.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Bubble);
        item.LostFocus += (_, _) => _spacePressed = false;
        item.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        item.AddHandler(InputElement.PointerReleasedEvent, OnReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        item.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        item.PointerExited += OnReleased;
        item.DetachedFromVisualTree += (_, _) =>
        {
            _submenu?.Dispose();
            _submenu = null;
            _spacePressed = false;
            SynchronizeSelection(item);
            EndPress();
            SetPosition(item, MenuItemPosition.Single);
        };
        item.AttachedToVisualTree += (_, _) => Configure();
        UpdateShape();
        UpdateTrailingText();
    }

    private void OnTemplate(object? sender, TemplateAppliedEventArgs e)
    {
        SynchronizeSelection(_item);
        _checkmark?.Dispose();
        _checkmark = e.NameScope.Find<Control>("PART_Label") is not null
            ? new MenuCheckmarkPresentation(_item, e.NameScope)
            : null;
        _colors?.Dispose();
        _colors = new MenuItemColors(_item, e.NameScope);
        _row = e.NameScope.Find<Control>("PART_Row");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        if (_ripple is not null) _ripple.PressEnded -= EndPress;
        _ripple = e.NameScope.Find<InkRipple>("PART_Ripple");
        if (_ripple is not null) _ripple.PressEnded += EndPress;
        _state = e.NameScope.Find<StateLayer>("PART_StateLayer");
        _animated = null;
        Configure();
    }

    private void Configure()
    {
        var popup = _popup;
        if (popup is not null && _item.HasSubMenu && _submenu?.Popup != popup)
        {
            _submenu?.Dispose();
            _submenu = new MenuPopupPresentation(_item, popup, null);
            _submenu.SetOpen(_item.IsSubMenuOpen);
        }

        if (_ripple is { } ripple)
        {
            ripple.AcceptActivation = () =>
                !_dispatchingKey && MenuAssist.GetIsAnimationEnabled(_item) && !MotionSettings.ReduceMotion;
            ripple.AcceptPress = e =>
                OwnsPoint(e) && IsPrimaryPress(e) && _item.IsEffectivelyEnabled &&
                MenuAssist.GetIsAnimationEnabled(_item) &&
                !MotionSettings.ReduceMotion;
        }

        UpdateTransitions();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_dispatchingKey || e.Source != _item || !_item.IsFocused || !_item.IsEffectivelyEnabled ||
            e.KeyModifiers != KeyModifiers.None) return;
        if (e.Key == Key.Space)
        {
            _spacePressed = true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape) _spacePressed = false;
        if (_item.FlowDirection != FlowDirection.RightToLeft || e.Key is not (Key.Left or Key.Right)) return;
        e.Handled = true;
        DispatchNativeKey(e.Key == Key.Left ? Key.Right : Key.Left);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Source != _item || e.Key != Key.Space || !_spacePressed) return;
        _spacePressed = false;
        e.Handled = true;
        if (!_item.IsFocused || !_item.IsEffectivelyEnabled || e.KeyModifiers != KeyModifiers.None) return;
        DispatchNativeKey(Key.Enter);
    }

    private void DispatchNativeKey(Key key)
    {
        // Avalonia's menu activation and selection APIs are internal; route the equivalent
        // key through its existing handler instead of duplicating toggle/command/close logic.
        _dispatchingKey = true;
        try
        {
            _item.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        }
        finally
        {
            _dispatchingKey = false;
        }
    }

    private bool OwnsPoint(PointerEventArgs e) => _row is not null && e.Source is Visual source &&
                                                  (source == _item || source == _row ||
                                                   source.GetVisualAncestors().Contains(_row)) &&
                                                  TopLevel.GetTopLevel(e.Source as Visual) ==
                                                  TopLevel.GetTopLevel(_row) &&
                                                  new Rect(_row.Bounds.Size).Contains(e.GetPosition(_row));

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!OwnsPoint(e) || !_item.IsEffectivelyEnabled || !IsPrimaryPress(e)) return;
        EndPress();
        _pressedPointer = e.Pointer;
        _pressRoot = TopLevel.GetTopLevel(_item);
        _pressRoot?.AddHandler(InputElement.PointerReleasedEvent, OnReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _pressRoot?.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, true);
        if (_pressRoot is WindowBase window) window.Deactivated += OnDeactivated;
        _item.Classes.Add("m3-menu-pressed");
    }

    private bool IsPrimaryPress(PointerPressedEventArgs e) =>
        InkRipple.IsPrimaryPress(e, _item);

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pressedPointer == e.Pointer) EndPress();
    }

    private void OnReleased(object? sender, PointerEventArgs e)
    {
        if (_pressedPointer == e.Pointer) EndPress();
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedPointer == e.Pointer && _row is not null &&
            !new Rect(_row.Bounds.Size).Contains(e.GetPosition(_row))) EndPress();
    }

    private void OnDeactivated(object? sender, EventArgs e) => EndPress();

    private void EndPress()
    {
        _item.Classes.Remove("m3-menu-pressed");
        _pressRoot?.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
        _pressRoot?.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
        if (_pressRoot is WindowBase window) window.Deactivated -= OnDeactivated;
        _pressRoot = null;
        _pressedPointer = null;
    }

    private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MenuItem.IsCheckedProperty)
        {
            // Native toggling precedes Closing; commit its appearance after that dispatch.
            var generation = ++_selectionGeneration;
            if (!_item.IsAttachedToVisualTree()) SynchronizeSelection(_item);
            else
                Dispatcher.UIThread.Post(() =>
                {
                    if (generation == _selectionGeneration && !MenuPopupPresentation.IsSelectionFrozen(_item))
                        _item.SetValue(DisplayCheckedProperty, _item.IsChecked);
                }, DispatcherPriority.Normal);
        }

        if (e.Property == PositionProperty || e.Property == DisplayCheckedProperty) UpdateShape();
        if (e.Property == MenuAssist.TrailingTextProperty || e.Property == MenuItem.InputGestureProperty)
            UpdateTrailingText();
        if (e.Property == MenuItem.IsSubMenuOpenProperty)
        {
            Configure();
            _submenu?.SetOpen(_item.IsSubMenuOpen);
        }

        if (e.Property == MenuAssist.IsAnimationEnabledProperty) UpdateTransitions();
        if (e.Property == InputElement.IsEffectivelyEnabledProperty && !_item.IsEffectivelyEnabled)
        {
            _spacePressed = false;
            EndPress();
            _ripple?.CancelPress();
        }
    }

    private void UpdateTrailingText()
    {
        var text = MenuAssist.GetTrailingText(_item);
        _item.Classes.Set("m3-trailing-text", text is not null);
        _item.Classes.Set("m3-has-trailing-text", text is null ? _item.InputGesture is not null : text.Length > 0);
    }

    private void UpdateTransitions()
    {
        var animated = MenuAssist.GetIsAnimationEnabled(_item);
        var reduced = MotionSettings.ReduceMotion;
        if (_animated == animated && _reduced == reduced) return;
        _animated = animated;
        _reduced = reduced;
        _item.SetValue(Animatable.TransitionsProperty, animated && !reduced
            ? new Transitions
            {
                new SpringCornerRadiusTransition
                {
                    Property = TemplatedControl.CornerRadiusProperty, Style = MotionStyle.Spatial,
                    Speed = MotionSpeed.Fast
                }
            }
            : null, BindingPriority.Style);
        if (_state is { } state)
            state.SetValue(Animatable.TransitionsProperty,
                animated
                    ? new Transitions
                    {
                        new SpringDoubleTransition
                            { Property = Visual.OpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast }
                    }
                    : null, BindingPriority.Style);
        if (!animated || reduced)
            _ripple?.CancelPress();
    }

    private void UpdateShape()
    {
        UpdateTransitions();
        var position = GetDisplayChecked(_item) ? MenuItemPosition.Single : _item.GetValue(PositionProperty);
        if (position == MenuItemPosition.Middle) position = MenuItemPosition.Single;
        if (_shapePosition == position && _shapeChecked == GetDisplayChecked(_item)) return;
        _shapePosition = position;
        _shapeChecked = GetDisplayChecked(_item);
        _shape?.Dispose();
        var key = GetDisplayChecked(_item)
            ? "MdCompMenusItemSelectedShape"
            : position switch
            {
                MenuItemPosition.First or MenuItemPosition.Standalone => "MdCompMenusItemFirstChildShape",
                MenuItemPosition.Last => "MdCompMenusItemLastChildShape",
                _ => "MdCompMenusItemShape"
            };
        var binding = new MultiBinding
        {
            Priority = BindingPriority.Style,
            Converter = new ItemCorners(GetDisplayChecked(_item) ? MenuItemPosition.Single : position)
        };
        binding.Bindings.Add(TokenBindingFactory.Create(key));
        binding.Bindings.Add(TokenBindingFactory.Create(position == MenuItemPosition.Last
            ? "MdCompMenusItemLastChildInnerCorner"
            : "MdCompMenusItemFirstChildInnerCorner"));
        if (position == MenuItemPosition.Standalone)
            binding.Bindings.Add(TokenBindingFactory.Create("MdCompMenusItemLastChildShape"));
        _shape = _item.Bind(TemplatedControl.CornerRadiusProperty, binding);
    }

    private sealed class ItemCorners(MenuItemPosition position) : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || values[0] is not CornerRadius outer || values[1] is not CornerRadius inner)
                return AvaloniaProperty.UnsetValue;
            return position switch
            {
                MenuItemPosition.Standalone when values.Count > 2 && values[2] is CornerRadius last =>
                    new CornerRadius(outer.TopLeft, outer.TopRight, last.BottomRight, last.BottomLeft),
                MenuItemPosition.First => new CornerRadius(outer.TopLeft, outer.TopRight, inner.BottomRight,
                    inner.BottomLeft),
                MenuItemPosition.Last => new CornerRadius(inner.TopLeft, inner.TopRight, outer.BottomRight,
                    outer.BottomLeft),
                _ => outer
            };
        }
    }
}