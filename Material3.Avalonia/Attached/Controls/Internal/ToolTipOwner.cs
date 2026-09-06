using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class ToolTipOwner
{
    private readonly Control _owner;
    private List<IDisposable>? _defaults;
    private IDisposable? _showTimer;
    private IDisposable? _hideTimer;
    private IDisposable? _exitTimer;
    private Flyout? _flyout;
    private Flyout? _programmaticFlyout;
    private FlyoutRegistration? _registration;
    private Popup? _popup;
    private Control? _presenter;
    private TopLevel? _topLevel;
    private ToolTipCoordinator? _coordinator;
    private bool _connected;
    private bool _refreshing;
    private bool _pointerOver;
    private bool _keyboardFocused;
    private bool _openedByHover;
    private bool _suppressed;
    private bool _isOpening;
    private bool _listeningRoot;
    private bool _listeningDisabledPointer;
    private int _lifecycle;
    private Popup? _passThroughPopup;
    private bool _resetInputPassThrough;
    private bool _resetDismissPassThrough;
    private bool _openedWithKeyboard;
    private readonly List<ScrollViewer> _scrollHosts = [];
    private bool _placementUpdateQueued;

    public ToolTipOwner(Control owner)
    {
        _owner = owner;
        owner.AttachedToVisualTree += OnAttached;
        owner.DetachedFromVisualTree += OnDetached;
    }

    private bool IsRich => _registration?.IsRich == true;
    private bool IsTransient => !IsRich || _flyout?.ShowMode == FlyoutShowMode.Transient;
    private bool IsOpen => IsRich ? _flyout?.IsOpen == true && _flyout.Target == _owner : ToolTip.GetIsOpen(_owner);

    private bool Allows(ToolTipTriggers trigger) => (ToolTipAssist.GetShowTriggers(_owner) & trigger) != 0;

    private bool CanTrigger => _pointerOver && Allows(ToolTipTriggers.Hover) ||
                               _keyboardFocused && Allows(ToolTipTriggers.Focus);

    private void UpdateRetainedHover() => HoverState.Retain(_owner,
        IsOpen && IsTransient && _openedByHover && ToolTipAssist.GetKeepAnchorHovered(_owner));

    private bool IsInRegion => _pointerOver || _keyboardFocused ||
                               _presenter?.IsPointerOver == true || _presenter?.IsKeyboardFocusWithin == true;

    public void Refresh()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            if (!ToolTipAssist.GetIsMaterialBehaviorEnabled(_owner) || !_owner.IsAttachedToVisualTree())
            {
                Disconnect();
                return;
            }

            var flyout = _programmaticFlyout ?? ToolTipAssist.GetFlyout(_owner);
            if (_flyout != flyout)
            {
                Close();
                if (_registration is not null)
                {
                    _registration.Changed -= OnFlyoutChanged;
                    _registration.Configure(this, false);
                }

                _flyout = flyout;
                _registration = flyout is null ? null : FlyoutRegistration.Get(flyout);
                if (_registration is not null)
                    _registration.Changed += OnFlyoutChanged;
            }

            _registration?.Configure(this, true);
            if (ToolTip.GetTip(_owner) is null && !IsRich)
            {
                Disconnect(keepRegistration: true);
                return;
            }

            if (!_connected)
            {
                _connected = true;
                _topLevel = TopLevel.GetTopLevel(_owner);
                _coordinator = _topLevel is null ? null : ToolTipCoordinator.Get(_topLevel);
                _owner.PointerEntered += OnPointerEntered;
                _owner.PointerExited += OnPointerExited;
                _owner.GotFocus += OnGotFocus;
                _owner.LostFocus += OnLostFocus;
                _owner.AddHandler(InputElement.HoldingEvent, OnHolding);
                _owner.PropertyChanged += OnOwnerPropertyChanged;
                _defaults = new List<IDisposable>
                {
                    // Keep the application's service binding underneath Material's hover controller.
                    _owner.SetValue(ToolTip.ServiceEnabledProperty, false, BindingPriority.Animation)!
                };
            }

            UpdateDisabledPointer();
            if (!IsTransient)
                CancelTimers();
        }
        finally
        {
            _refreshing = false;
        }
    }

    public bool PrepareFlyout(Flyout flyout)
    {
        if (_flyout != flyout)
        {
            _programmaticFlyout = flyout;
            Refresh();
        }

        if (!_connected)
            return true;

        if (!_isOpening && _coordinator?.Activate(this) == false)
            return false;

        _openedWithKeyboard = _keyboardFocused && flyout.ShowMode == FlyoutShowMode.Standard;

        // Flyout copies these properties before Opening, so configure the actual popup here.
        if (IsTransient && _topLevel is not null &&
            !flyout.IsSet(PopupFlyoutBase.OverlayInputPassThroughElementProperty) &&
            flyout.Popup.OverlayInputPassThroughElement is null)
        {
            flyout.Popup.SetCurrentValue(Popup.OverlayInputPassThroughElementProperty, _topLevel);
            _resetInputPassThrough = true;
        }

        if (IsTransient && !flyout.IsSet(PopupFlyoutBase.OverlayDismissEventPassThroughProperty))
        {
            flyout.Popup.SetCurrentValue(Popup.OverlayDismissEventPassThroughProperty, true);
            _resetDismissPassThrough = true;
        }

        _passThroughPopup = flyout.Popup;
        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(_passThroughPopup, flyout.Popup) && !flyout.IsOpen)
                Closed();
        });
        ListenToRoot();
        return true;
    }

    public void AttachPresentation(Popup popup, Control presenter)
    {
        if (!_connected || presenter is FlyoutPresenter && !IsRich)
            return;

        DetachPresentation();
        _popup = popup;
        _presenter = presenter;
        popup.Closed += OnPopupClosed;
        popup.Opened += OnPopupOpened;
        presenter.PointerEntered += OnPopupPointerEntered;
        presenter.PointerExited += OnPopupPointerExited;
        presenter.GotFocus += OnPopupGotFocus;
        presenter.LostFocus += OnPopupLostFocus;
        presenter.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        ListenToRoot();
        _coordinator?.Activate(this);
    }

    public bool Close()
    {
        CancelTimers();
        if (_flyout?.Target == _owner && (IsRich || _presenter is FlyoutPresenter))
            _flyout.Hide();
        if (ToolTip.GetIsOpen(_owner))
            _owner.SetCurrentValue(ToolTip.IsOpenProperty, false);

        if (IsOpen)
        {
            if (_presenter is not null)
                ToolTipPresentation.SetIsPresented(_presenter, true);
            return false;
        }

        Closed();
        return true;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Refresh();
    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Disconnect();
    private void OnFlyoutChanged() => Refresh();

    private void Disconnect(bool keepRegistration = false)
    {
        if (_connected)
            Close();
        else
            Closed();
        if (_defaults is not null)
            foreach (var subscription in _defaults)
                subscription.Dispose();
        _defaults = null;
        if (_connected)
        {
            _owner.PointerEntered -= OnPointerEntered;
            _owner.PointerExited -= OnPointerExited;
            _owner.GotFocus -= OnGotFocus;
            _owner.LostFocus -= OnLostFocus;
            _owner.RemoveHandler(InputElement.HoldingEvent, OnHolding);
            _owner.PropertyChanged -= OnOwnerPropertyChanged;
            _connected = false;
        }

        _pointerOver = false;
        _keyboardFocused = false;
        _suppressed = false;
        StopDisabledPointer();
        _topLevel = null;
        _coordinator = null;
        if (!keepRegistration)
        {
            if (_registration is not null)
            {
                _registration.Changed -= OnFlyoutChanged;
                _registration.Configure(this, false);
            }

            _registration = null;
            _flyout = _programmaticFlyout = null;
        }
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ToolTipAssist.KeepAnchorHoveredProperty)
            UpdateRetainedHover();
        else if (e.Property == ToolTipAssist.ShowTriggersProperty)
        {
            if (!CanTrigger)
            {
                _showTimer?.Dispose();
                _showTimer = null;
            }
        }
        else if (e.Property == ToolTip.IsOpenProperty && !e.GetNewValue<bool>())
            Closed();
        else if (e.Property == InputElement.IsEffectivelyEnabledProperty ||
                 e.Property == ToolTip.ShowOnDisabledProperty)
        {
            UpdateDisabledPointer();
            if (!_owner.IsEffectivelyEnabled && !ToolTip.GetShowOnDisabled(_owner))
                Close();
        }
    }

    private void UpdateDisabledPointer()
    {
        if (!_owner.IsEffectivelyEnabled && ToolTip.GetShowOnDisabled(_owner) && _topLevel is not null)
        {
            if (_listeningDisabledPointer)
                return;
            // Disabled controls do not receive routed pointer events; use Avalonia's inclusive hit test.
            _topLevel.AddHandler(InputElement.PointerMovedEvent, OnDisabledPointerMoved, RoutingStrategies.Tunnel,
                handledEventsToo: true);
            _topLevel.PointerExited += OnPointerExited;
            _listeningDisabledPointer = true;
        }
        else
            StopDisabledPointer();
    }

    private void StopDisabledPointer()
    {
        if (_listeningDisabledPointer && _topLevel is not null)
        {
            _topLevel.RemoveHandler(InputElement.PointerMovedEvent, OnDisabledPointerMoved);
            _topLevel.PointerExited -= OnPointerExited;
        }

        _listeningDisabledPointer = false;
    }

    private void OnDisabledPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_topLevel is null || e.Pointer.Type == PointerType.Touch)
            return;
        var hit = _topLevel.InputHitTest(e.GetPosition(_topLevel), enabledElementsOnly: false) as Visual;
        var over = hit == _owner || hit?.GetVisualAncestors().Contains(_owner) == true;
        if (over == _pointerOver)
            return;
        if (over)
            OnPointerEntered(sender, e);
        else
            OnPointerExited(sender, e);
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Touch)
            return;

        _pointerOver = true;
        _suppressed = false;
        EnterRegion(Allows(ToolTipTriggers.Hover));
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _pointerOver = false;
        _suppressed = false;
        LeaveRegion();
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        _keyboardFocused = e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional;
        if (_keyboardFocused && !_suppressed)
            EnterRegion(Allows(ToolTipTriggers.Focus));
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        _keyboardFocused = false;
        _suppressed = false;
        LeaveRegion();
    }

    private void OnHolding(object? sender, HoldingRoutedEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse || !IsTransient || !Allows(ToolTipTriggers.LongPress))
            return;

        if (_owner is TextBox or SelectableTextBlock ||
            e.Source is Visual source && source.GetVisualAncestors().Any(x => x is TextBox or SelectableTextBlock))
            return;

        if (e.HoldingState == HoldingState.Started && _owner.ContextMenu is null && _owner.ContextFlyout is null)
        {
            _suppressed = false;
            Show();
        }
        else if (e.HoldingState is HoldingState.Completed or HoldingState.Canceled)
            LeaveRegion();
    }

    private void EnterRegion(bool canOpen = false)
    {
        _hideTimer?.Dispose();
        _hideTimer = null;
        _exitTimer?.Dispose();
        _exitTimer = null;
        if (_presenter is not null)
            ToolTipPresentation.SetIsPresented(_presenter, true);

        if (!canOpen || !IsTransient || IsOpen || _suppressed || _showTimer is not null ||
            IsRich && ToolTipAssist.GetFlyout(_owner) != _flyout)
            return;

        var delay = _keyboardFocused || _coordinator?.ShouldShowImmediately(ToolTip.GetBetweenShowDelay(_owner)) == true
            ? 0
            : Math.Max(0, ToolTip.GetShowDelay(_owner));
        ListenToRoot();
        if (delay == 0)
            Show();
        else
            _showTimer = DispatcherTimer.RunOnce(() =>
            {
                _showTimer = null;
                if (CanTrigger)
                    Show();
            }, TimeSpan.FromMilliseconds(delay));
    }

    private void Show()
    {
        if (!_connected || _suppressed || !_owner.IsEffectivelyEnabled && !ToolTip.GetShowOnDisabled(_owner))
            return;
        if (_coordinator?.Activate(this) == false)
            return;

        _openedByHover = _pointerOver && Allows(ToolTipTriggers.Hover);
        _isOpening = true;
        try
        {
            if (IsRich)
                _flyout!.ShowAt(_owner);
            else
                _owner.SetCurrentValue(ToolTip.IsOpenProperty, true);
        }
        finally
        {
            _isOpening = false;
        }

        if (!IsOpen)
            Closed();
        else
            UpdateRetainedHover();
    }

    private void LeaveRegion()
    {
        // Focus and pointer may cross between two native popup roots in consecutive input events.
        var lifecycle = _lifecycle;
        Dispatcher.UIThread.Post(() =>
        {
            if (lifecycle != _lifecycle || !_connected || IsInRegion)
                return;

            _showTimer?.Dispose();
            _showTimer = null;
            if (!IsOpen)
            {
                Closed();
                return;
            }

            if (!IsTransient || _hideTimer is not null)
                return;

            _hideTimer =
                DispatcherTimer.RunOnce(BeginExit, TimeSpan.FromMilliseconds(ToolTipAssist.GetHideDelay(_owner)));
        }, DispatcherPriority.Input);
    }

    private void BeginExit()
    {
        _hideTimer = null;
        if (IsInRegion || !IsOpen)
            return;

        if (_presenter is null || MotionSettings.ReduceMotion)
        {
            Close();
            return;
        }

        ToolTipPresentation.SetIsPresented(_presenter, false);
        var spring = MotionSettings.GlobalScheme.Resolve(MotionStyle.Spatial, MotionSpeed.Fast);
        var duration = SpringDuration.ComputeSeconds(spring.Stiffness, spring.Damping, effects: false);
        _exitTimer = DispatcherTimer.RunOnce(() => Close(), TimeSpan.FromSeconds(duration));
    }

    private void OnPopupPointerEntered(object? sender, PointerEventArgs e) => EnterRegion();
    private void OnPopupPointerExited(object? sender, PointerEventArgs e) => LeaveRegion();
    private void OnPopupGotFocus(object? sender, FocusChangedEventArgs e) => EnterRegion();
    private void OnPopupLostFocus(object? sender, RoutedEventArgs e) => LeaveRegion();
    private void OnPopupClosed(object? sender, EventArgs e) => Closed();

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (!_openedWithKeyboard || !_owner.IsKeyboardFocusWithin || _presenter is null)
            return;
        // Set the navigation method before Flyout's default Focus() call targets the same element.
        var target = _presenter.Focusable ? _presenter : FocusManager.FindFirstFocusableElement(_presenter);
        target?.Focus(NavigationMethod.Tab);
    }

    private void OnAncestorScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source != sender || e.OffsetDelta == default)
            return;
        _suppressed = true;
        Close();
    }

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e)
    {
        if (_placementUpdateQueued || !IsRich || _popup is null)
            return;
        _placementUpdateQueued = true;
        var lifecycle = _lifecycle;
        Dispatcher.UIThread.Post(() =>
        {
            _placementUpdateQueued = false;
            if (lifecycle == _lifecycle && IsOpen && _popup is { } popup)
                _registration?.RefreshPlacement(popup);
        }, DispatcherPriority.Render);
    }

    private void ListenToRoot()
    {
        if (_topLevel is null || _listeningRoot)
            return;
        _listeningRoot = true;
        _owner.ResourcesChanged += OnResourcesChanged;
        foreach (var scroll in _owner.GetVisualAncestors().OfType<ScrollViewer>())
        {
            _scrollHosts.Add(scroll);
            scroll.ScrollChanged += OnAncestorScrollChanged;
        }

        _topLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _topLevel.AddHandler(InputElement.PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        if (e.Key == Key.Escape && (IsOpen || _showTimer is not null))
        {
            _suppressed = true;
            var restoreFocus = _presenter?.IsKeyboardFocusWithin == true;
            Close();
            if (restoreFocus)
                _owner.Focus(NavigationMethod.Tab);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && IsRich && IsOpen && _presenter is not null &&
                 _topLevel?.FocusManager is { } focus)
        {
            var current = focus.GetFocusedElement();
            var backwards = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (_owner.IsKeyboardFocusWithin && !backwards)
                e.Handled = FocusManager.FindFirstFocusableElement(_presenter)
                    ?.Focus(NavigationMethod.Tab, e.KeyModifiers) == true;
            else if (current is not null && _presenter.IsKeyboardFocusWithin &&
                     current == (backwards
                         ? FocusManager.FindFirstFocusableElement(_presenter)
                         : FocusManager.FindLastFocusableElement(_presenter)))
            {
                var next = backwards
                    ? _owner
                    : focus.FindNextElement(NavigationDirection.Next,
                        new FindNextElementOptions { FocusedElement = _owner });
                _suppressed = true;
                Close();
                e.Handled = (next ?? _owner).Focus(NavigationMethod.Tab, e.KeyModifiers);
            }
        }
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual source && _presenter is not null &&
            (source == _presenter || source.GetVisualAncestors().Contains(_presenter)))
            return;

        _suppressed = true;
        Close();
    }

    private void Closed()
    {
        _lifecycle++;
        _openedWithKeyboard = false;
        foreach (var scroll in _scrollHosts)
            scroll.ScrollChanged -= OnAncestorScrollChanged;
        _scrollHosts.Clear();
        _openedByHover = false;
        HoverState.Retain(_owner, false);
        CancelTimers();
        DetachPresentation();
        _coordinator?.Closed(this);
        if (_passThroughPopup is { } popup)
        {
            if (_resetInputPassThrough && ReferenceEquals(popup.OverlayInputPassThroughElement, _topLevel))
                popup.SetCurrentValue(Popup.OverlayInputPassThroughElementProperty, null);
            if (_resetDismissPassThrough && popup.OverlayDismissEventPassThrough)
                popup.SetCurrentValue(Popup.OverlayDismissEventPassThroughProperty, false);
        }

        _passThroughPopup = null;
        _resetInputPassThrough = _resetDismissPassThrough = false;
        if (_listeningRoot && _topLevel is not null)
        {
            _owner.ResourcesChanged -= OnResourcesChanged;
            _topLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            _topLevel.RemoveHandler(InputElement.PointerPressedEvent, OnRootPointerPressed);
        }

        _listeningRoot = false;
        if (_programmaticFlyout is not null && _programmaticFlyout == _flyout && !_refreshing)
        {
            _programmaticFlyout = null;
            // Popup.Closed can run before Flyout finishes clearing its target and open state.
            Dispatcher.UIThread.Post(Refresh);
        }
    }

    private void DetachPresentation()
    {
        if (_popup is not null)
        {
            _popup.Closed -= OnPopupClosed;
            _popup.Opened -= OnPopupOpened;
        }

        if (_presenter is not null)
        {
            _presenter.PointerEntered -= OnPopupPointerEntered;
            _presenter.PointerExited -= OnPopupPointerExited;
            _presenter.GotFocus -= OnPopupGotFocus;
            _presenter.LostFocus -= OnPopupLostFocus;
            _presenter.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        _popup = null;
        _presenter = null;
    }

    private void CancelTimers()
    {
        _showTimer?.Dispose();
        _hideTimer?.Dispose();
        _exitTimer?.Dispose();
        _showTimer = _hideTimer = _exitTimer = null;
    }
}