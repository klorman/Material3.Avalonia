using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class MenuPopupPresentation : IDisposable
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(MenuPopupPresentation));

    private static readonly ConditionalWeakTable<Control, RootRegistration> Roots = new();
    private static readonly List<WeakReference<MenuPopupPresentation>> OpenMenus = [];
    private static readonly ConditionalWeakTable<TopLevel, PointerAnchor> PointerAnchors = new();
    private static readonly ConditionalWeakTable<TopLevel, InputMode> InputModes = new();

    private sealed class InputMode
    {
        internal bool Keyboard;
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    static MenuPopupPresentation()
    {
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>((root, _) =>
            InputModes.GetOrCreateValue(root).Keyboard = true, RoutingStrategies.Tunnel, true);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>((root, _) =>
            InputModes.GetOrCreateValue(root).Keyboard = false, RoutingStrategies.Tunnel, true);
        Control.ContextRequestedEvent.AddClassHandler<TopLevel>((root, e) =>
        {
            if (!e.TryGetPosition(root, out var point)) return;
            var anchor = new PointerAnchor(root.PointToScreen(point));
            PointerAnchors.AddOrUpdate(root, anchor);
            Dispatcher.UIThread.Post(() =>
            {
                if (PointerAnchors.TryGetValue(root, out var current) && current == anchor)
                    PointerAnchors.Remove(root);
            }, DispatcherPriority.Send);
        }, RoutingStrategies.Tunnel);
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, change) =>
        {
            if (change.GetNewValue<bool>()) Roots.GetValue(control, c => new RootRegistration(c));
        });
        PopupFlyoutBase.IsOpenProperty.Changed.AddClassHandler<MenuFlyout>((flyout, change) =>
        {
            if (change.GetNewValue<bool>() && flyout.Popup.Child is Control child && GetIsEnabled(child))
                MenuFlyoutRegistration.Get(flyout).Connect();
        });
    }

    internal static MenuPopupPresentation? Find(Control control) =>
        Roots.TryGetValue(control, out var root) ? root.Presentation : null;

    internal static void Ensure(Control control) => Roots.GetValue(control, c => new RootRegistration(c)).Connect();

    private readonly Control _owner;
    internal Popup Popup { get; }
    private readonly Action? _closeRoot;
    private MenuSurface? _surface;
    private Control? _target;
    private bool _framePending;
    private bool _running;
    private readonly Stopwatch _clock = new();
    private double _from;
    private double _fromProgress;
    private double _to = 1;
    private double _activeFrom = 1;
    private double _activeTo = 1;
    private double _activeStarted;
    private bool _closing;
    private bool _selectionFrozen;
    private bool _permitClose;
    private bool _disposed;
    private bool _finished;
    private bool _desiredOpen;
    private int _generation;
    private double _animationStarted;
    private double _initialVelocity;
    private double _velocity;
    private SpringToken _spring;
    private bool _reduced;
    private Action? _afterExit;
    private Window? _window;
    private Control? _chain;
    private EventHandler<ResourcesChangedEventArgs>? _resourcesChanged;
    private Control? _returnFocus;
    private PixelPoint? _pointerAnchor;
    private bool _keyboardNavigation;
    private Point? _lastPointerPosition;
    private MenuPopupPresentation[]? _exitMembers;

    private sealed record PointerAnchor(PixelPoint Position);

    internal MenuPopupPresentation(Control owner, Popup popup, Action? closeRoot)
    {
        _owner = owner;
        Popup = popup;
        _closeRoot = closeRoot;
        popup.Opened += OnOpened;
        popup.Closed += OnClosed;
        owner.PropertyChanged += OnOwnerChanged;
        owner.AddHandler(InputElement.GotFocusEvent, OnFocus, handledEventsToo: true);
        owner.AddHandler(InputElement.LostFocusEvent, OnLostFocus, handledEventsToo: true);
        owner.AddHandler(InputElement.KeyDownEvent, OnKeyboardActivity, RoutingStrategies.Tunnel, true);
        owner.AddHandler(InputElement.PointerMovedEvent, OnPointerActivity, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerPressedEvent, OnPointerActivity, handledEventsToo: true);
        owner.AddHandler(InputElement.KeyDownEvent, BlockExitingInput, RoutingStrategies.Tunnel);
        owner.AddHandler(InputElement.KeyUpEvent, BlockExitingInput, RoutingStrategies.Tunnel);
        owner.AddHandler(InputElement.PointerPressedEvent, BlockExitingPointer, RoutingStrategies.Tunnel);
        owner.AddHandler(InputElement.PointerReleasedEvent, BlockExitingRelease, RoutingStrategies.Tunnel);
        _clock.Start();
        if (popup.IsOpen) OnOpened(null, EventArgs.Empty);
    }

    internal void SetOpen(bool open)
    {
        if (_disposed || _desiredOpen == open && !_closing || !open && _closing) return;
        _desiredOpen = open;
        if (open)
        {
            foreach (var weak in OpenMenus.ToArray())
                if (weak.TryGetTarget(out var menu) && menu != this && menu._exitMembers?.Contains(this) == true)
                    menu.RestoreExitBranch();
            _generation++;
            _closing = false;
            if (!Popup.IsOpen)
            {
                if (_owner is MenuItem && TopLevel.GetTopLevel(_owner) is { } parentHost)
                    Popup.SetValue(Popup.ShouldUseOverlayLayerProperty, parentHost is not PopupRoot,
                        BindingPriority.Style);
                UpdateAnchor();

                Popup.SetCurrentValue(Popup.IsOpenProperty, true);
            }
            else RestoreExitBranch();
        }
        else if (Popup.IsOpen) BeginExit(() => Popup.SetCurrentValue(Popup.IsOpenProperty, false));
    }

    internal void UpdateAnchor()
    {
        if (_owner is not MenuItem || Popup.CustomPopupPlacementCallback is not null ||
            Popup.Placement is not (PlacementMode.RightEdgeAlignedTop or PlacementMode.LeftEdgeAlignedTop)) return;
        var surface = _surface;
        if (surface is null) return;
        var left = _owner.FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft
            ? Popup.Placement == PlacementMode.RightEdgeAlignedTop
            : Popup.Placement == PlacementMode.LeftEdgeAlignedTop;
        if (surface.Bounds.Width > 0 && TopLevel.GetTopLevel(surface) is { } host &&
            (host is not PopupRoot || PopupGeometry.HasScreenCoordinates(host)))
        {
            var center = surface.PointToScreen(surface.SurfaceBounds.Center);
            var anchor = _owner.PointToScreen(new Rect(_owner.Bounds.Size).Center);
            left = center.X < anchor.X;
        }

        Popup.SetValue(Popup.HorizontalOffsetProperty,
            left ? surface.ContentPadding.Right - surface.AnchorGap : surface.AnchorGap - surface.ContentPadding.Left,
            BindingPriority.Style);
        Popup.SetValue(Popup.VerticalOffsetProperty, -surface.ContentPadding.Top, BindingPriority.Style);
    }

    internal static bool IsSelectionFrozen(MenuItem item) => OpenMenus.Any(weak =>
        weak.TryGetTarget(out var menu) && menu._selectionFrozen && menu.OwnsSource(item));

    private MenuPopupPresentation[] GetOpenBranch() => OpenMenus
        .Select(w => w.TryGetTarget(out var menu) ? menu : null)
        .OfType<MenuPopupPresentation>()
        .Where(menu => !menu._disposed && menu.Popup.IsOpen && menu._chain == _chain &&
                       (menu == this || _owner is MenuBase || _owner.IsLogicalAncestorOf(menu._owner)))
        .Append(this).Distinct().ToArray();

    private void SynchronizeSelection()
    {
        _selectionFrozen = false;
        foreach (var item in _surface?.ItemsPanel?.Children.OfType<MenuItem>() ??
                             Popup.Child?.GetVisualDescendants().OfType<MenuItem>() ?? [])
            MenuItemPresentation.SynchronizeSelection(item);
    }

    internal void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!e.Cancel && (_permitClose || !MenuAssist.GetIsAnimationEnabled(_owner)) &&
            Popup.Placement == PlacementMode.Pointer && _returnFocus is { } previous)
            MenuFocusRestore.SuppressAutomaticScroll(previous);
        if (_disposed || _permitClose || e.Cancel || !MenuAssist.GetIsAnimationEnabled(_owner)) return;
        e.Cancel = true;
        foreach (var member in GetOpenBranch()) member._selectionFrozen = true;
        if (_closing) return;
        var generation = ++_generation;
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed || generation != _generation) return;
            // ContextMenu sets Popup.IsOpen before raising Closing; keep the cancelled popup state consistent.
            if (Popup.Child?.IsAttachedToVisualTree() == true && !Popup.IsOpen)
                Popup.SetCurrentValue(Popup.IsOpenProperty, true);
            BeginExit(() =>
            {
                _permitClose = true;
                try
                {
                    _closeRoot?.Invoke();
                }
                finally
                {
                    _permitClose = false;
                }

                if (Popup.Child?.IsAttachedToVisualTree() == true)
                {
                    Popup.SetCurrentValue(Popup.IsOpenProperty, true);
                    RestoreExitBranch();
                }
            });
        });
    }

    private void BeginExit(Action completed)
    {
        var members = GetOpenBranch();
        _exitMembers = members;
        var generation = ++_generation;
        var remaining = members.Length;
        foreach (var member in members)
        {
            if (member != this) member._generation++;
            member._selectionFrozen = true;
            member._closing = true;
            member.RefreshKeyboardFocus();
            member._afterExit = () =>
            {
                if (--remaining == 0 && !_disposed && generation == _generation) completed();
            };
        }

        foreach (var member in members) member.Animate(false);
    }

    private void RestoreExitBranch()
    {
        var members = _exitMembers ?? [this];
        _exitMembers = null;
        foreach (var member in members)
            if (!member._disposed && member.Popup.Child?.IsAttachedToVisualTree() == true)
            {
                member._generation++;
                member.Animate(true);
            }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_disposed) return;
        _desiredOpen = true;
        _closing = false;
        _target = Popup.PlacementTarget;
        _pointerAnchor = Popup.Placement == PlacementMode.Pointer && TopLevel.GetTopLevel(_target) is { } root &&
                         PointerAnchors.TryGetValue(root, out var pointer)
            ? pointer.Position
            : null;
        _chain = _owner is MenuBase ? _owner : _owner.FindLogicalAncestorOfType<MenuBase>() ?? _owner;
        _surface = Popup.Child as MenuSurface ?? Popup.Child?.GetVisualDescendants().OfType<MenuSurface>()
            .FirstOrDefault();
        if (_surface is null)
        {
            (_owner as TemplatedControl)?.ApplyTemplate();
            (Popup.Child as TemplatedControl)?.ApplyTemplate();
            _surface = Popup.Child?.GetVisualDescendants().OfType<MenuSurface>()
                .FirstOrDefault();
        }

        SynchronizeSelection();
        if (_surface is not null)
        {
            _surface.Presentation = this;
            _surface.Progress = MotionSettings.ReduceMotion ||
                                !MenuAssist.GetIsAnimationEnabled(_owner)
                ? 1
                : 0;
            _surface.Opacity = MenuAssist.GetIsAnimationEnabled(_owner) ? 0 : 1;
        }

        if (_target is not null && _resourcesChanged is null)
        {
            _resourcesChanged = (_, args) => ((ILogical)Popup).NotifyResourcesChanged(args);
            _target.ResourcesChanged += _resourcesChanged;
            _window = _target.FindLogicalAncestorOfType<Window>() ?? TopLevel.GetTopLevel(_target) as Window;
            if (_window is not null) _window.Closed += OnWindowClosed;
        }

        OpenMenus.RemoveAll(w => !w.TryGetTarget(out var value) || value == this);
        _keyboardNavigation = OpenMenus.Any(w => w.TryGetTarget(out var menu) &&
                                                 menu._chain == _chain && menu._keyboardNavigation) ||
                              TopLevel.GetTopLevel(_target) is { } inputRoot &&
                              InputModes.TryGetValue(inputRoot, out var inputMode) && inputMode.Keyboard;
        OpenMenus.Add(new(this));
        if (_owner is MenuItem && !OwnsSource(TopLevel.GetTopLevel(Popup.Child)?.FocusManager?.GetFocusedElement()))
            SetActive(false);
        else Activate();
        var generation = ++_generation;
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed || generation != _generation || Popup.Child?.IsAttachedToVisualTree() != true) return;
            UpdateDirection();
            if (OwnsSource(TopLevel.GetTopLevel(Popup.Child)?.FocusManager?.GetFocusedElement())) Activate();
            RefreshKeyboardFocus();
            Animate(true);
        }, DispatcherPriority.Render);
    }

    private void UpdateDirection()
    {
        if (_surface is null) return;
        UpdateAnchor();
        if (_target is null) return;
        Rect? pointerRect = _pointerAnchor is { } pointer
            ? new Rect(_target.PointToClient(pointer), new Size(1, 1))
            : null;
        _surface.Direction = PopupGeometry.Resolve(_surface, _surface.SurfaceBounds, _target, Popup, pointerRect);
    }

    private void Animate(bool shown)
    {
        _finished = false;
        if (shown)
        {
            SynchronizeSelection();
            _closing = false;
            _afterExit = null;
            RefreshKeyboardFocus();
        }

        if (_surface is not null) _surface.IsHitTestVisible = shown;
        _from = _surface?.Opacity ?? (shown ? 0 : 1);
        _fromProgress = _surface?.Progress ?? (shown ? 0 : 1);
        _to = shown ? 1 : 0;
        _animationStarted = _clock.Elapsed.TotalSeconds;
        _initialVelocity = Math.Abs(_to - _fromProgress) > .00001 ? -_velocity / (_to - _fromProgress) : 0;
        _spring = MotionSettings.GlobalScheme.SpatialFast;
        _reduced = MotionSettings.ReduceMotion;
        RequestFrame();
        Tick();
    }

    private bool OwnsSource(object? source) => source is Visual visual && Popup.Child is { } child &&
                                               (visual == child || visual.GetVisualAncestors().Contains(child));

    private void OnFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_returnFocus is null && OwnsSource(e.Source) && !OwnsSource(e.OldFocusedElement))
            _returnFocus = e.OldFocusedElement as Control;
        if (OwnsSource(e.Source))
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
                SetKeyboardNavigation(true);
            Activate();
            RefreshKeyboardFocus();
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e) => RefreshKeyboardFocus();

    private void OnKeyboardActivity(object? sender, KeyEventArgs e)
    {
        if (!_closing && (e.Source == _owner || OwnsSource(e.Source)))
            SetKeyboardNavigation(true, e.Key != Key.Escape);
    }

    private void SetKeyboardNavigation(bool keyboard, bool refreshCurrent = true)
    {
        foreach (var weak in OpenMenus)
            if (weak.TryGetTarget(out var menu) && menu._chain == _chain)
            {
                menu._keyboardNavigation = keyboard;
                if (menu != this || refreshCurrent) menu.RefreshKeyboardFocus();
            }
    }

    private void RefreshKeyboardFocus()
    {
        foreach (var item in _surface?.ItemsPanel?.Children.OfType<MenuItem>() ??
                             Popup.Child?.GetVisualDescendants().OfType<MenuItem>() ?? [])
            item.Classes.Set("m3-menu-keyboard-focus", _keyboardNavigation && item.IsFocused &&
                                                       item.IsEffectivelyEnabled && !_closing);
    }

    private void OnPointerActivity(object? sender, PointerEventArgs e)
    {
        if (!_closing && OwnsSource(e.Source) &&
            (e.Pointer.Type != PointerType.Touch || e.RoutedEvent == InputElement.PointerPressedEvent))
        {
            var position = e.GetPosition(Popup.Child);
            if (_lastPointerPosition != position || e.RoutedEvent == InputElement.PointerPressedEvent)
                SetKeyboardNavigation(false);
            _lastPointerPosition = position;
            Activate();
        }
    }

    internal void Activate()
    {
        foreach (var weak in OpenMenus.ToArray())
            if (weak.TryGetTarget(out var other) && !other._disposed && other._chain == _chain)
                other.SetActive(other == this);
    }

    private void SetActive(bool active)
    {
        if (_activeTo == (active ? 1 : 0)) return;
        _activeFrom = _surface?.ActiveProgress ?? 1;
        _activeTo = active ? 1 : 0;
        _activeStarted = _clock.Elapsed.TotalSeconds;
        RequestFrame();
    }

    private void RequestFrame()
    {
        _running = true;
        if (_framePending || TopLevel.GetTopLevel(Popup.Child) is not { } top) return;
        _framePending = true;
        top.RequestAnimationFrame(_ =>
        {
            _framePending = false;
            if (!_running || _disposed) return;
            UpdateDirection();
            Tick();
            if (_running) RequestFrame();
        });
    }

    private void Tick()
    {
        if (_disposed) return;
        var enabled = MenuAssist.GetIsAnimationEnabled(_owner);
        var reduced = MotionSettings.ReduceMotion;
        var now = _clock.Elapsed.TotalSeconds;
        var spatial = MotionSettings.GlobalScheme.SpatialFast;
        var effects = MotionSettings.GlobalScheme.EffectsFast;
        if (_spring != spatial || _reduced != reduced)
        {
            _fromProgress = _surface?.Progress ?? _fromProgress;
            _from = _surface?.Opacity ?? _from;
            _animationStarted = now;
            _initialVelocity = Math.Abs(_to - _fromProgress) > .00001 ? -_velocity / (_to - _fromProgress) : 0;
            _spring = spatial;
            _reduced = reduced;
            if (_surface?.ItemsPanel is { } panel)
                foreach (var item in panel.Children.OfType<MenuItem>())
                    MenuItemPresentation.RefreshMotion(item);
        }

        var effectDuration = SpringDuration.ComputeSeconds(effects.Stiffness, effects.Damping, true);
        var spatialDuration = SpringDuration.ComputeSeconds(spatial.Stiffness, spatial.Damping, false);
        var elapsed = now - _animationStarted;
        var spatialValue = SpringAnalytic.Evaluate(elapsed, spatial.Stiffness, spatial.Damping, _initialVelocity);
        var nextSpatialValue =
            SpringAnalytic.Evaluate(elapsed + .016, spatial.Stiffness, spatial.Damping, _initialVelocity);
        var distance = Math.Max(1,
            (_surface?.MotionDistance ?? 0) * (TopLevel.GetTopLevel(Popup.Child)?.RenderScaling ?? 1));
        var spatialDone = reduced || elapsed >= spatialDuration &&
            Math.Abs(1 - spatialValue) * distance < .25 && Math.Abs(nextSpatialValue - spatialValue) * distance < .25;
        var done = !enabled || elapsed >= effectDuration && spatialDone;
        _velocity = done || reduced ? 0 : (_to - _fromProgress) * (nextSpatialValue - spatialValue) / .016;
        var activeDone = !enabled || reduced || now - _activeStarted >= spatialDuration;
        if (_surface is not null)
        {
            var opacity = done
                ? _to
                : _from + (_to - _from) *
                Math.Clamp(SpringAnalytic.Evaluate(elapsed, effects.Stiffness, effects.Damping, 0), 0, 1);
            _surface.Opacity = opacity;
            _surface.Progress = reduced ? 1 :
                done ? _to :
                _fromProgress + (_to - _fromProgress) *
                spatialValue;
            _surface.ActiveProgress = activeDone
                ? _activeTo
                : _activeFrom + (_activeTo - _activeFrom) *
                SpringAnalytic.Evaluate(now - _activeStarted, spatial.Stiffness, spatial.Damping, 0);
        }

        if (done && activeDone) _running = false;
        if (done && !_finished)
        {
            _finished = true;
            var completed = _afterExit;
            _afterExit = null;
            completed?.Invoke();
        }
    }

    private void OnOwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MenuAssist.IsAnimationEnabledProperty) Tick();
    }

    private void BlockExitingInput(object? sender, KeyEventArgs e)
    {
        if (_closing) e.Handled = true;
    }

    private void BlockExitingPointer(object? sender, PointerPressedEventArgs e)
    {
        if (_closing) e.Handled = true;
    }

    private void BlockExitingRelease(object? sender, PointerReleasedEventArgs e)
    {
        if (_closing) e.Handled = true;
    }

    private void OnWindowClosed(object? sender, EventArgs e) => Dispose();

    private void OnClosed(object? sender, EventArgs e)
    {
        var completedExit = _closing ? _afterExit : null;
        SynchronizeSelection();
        PopupGeometry.Forget(Popup);
        _generation++;
        _desiredOpen = false;
        _closing = false;
        _running = false;
        _velocity = 0;
        _afterExit = null;
        _returnFocus = null;
        _keyboardNavigation = false;
        _lastPointerPosition = null;
        _exitMembers = null;
        RefreshKeyboardFocus();
        if (_surface is not null)
        {
            _surface.Progress = 0;
            _surface.Opacity = 0;
        }

        OpenMenus.RemoveAll(w => !w.TryGetTarget(out var value) || value == this);
        foreach (var weak in OpenMenus.AsEnumerable().Reverse())
            if (weak.TryGetTarget(out var other) && other._chain == _chain)
            {
                other.Activate();
                break;
            }

        completedExit?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OnClosed(null, EventArgs.Empty);
        if (_target is not null && _resourcesChanged is not null) _target.ResourcesChanged -= _resourcesChanged;
        if (_window is not null) _window.Closed -= OnWindowClosed;
        _resourcesChanged = null;
        Popup.Opened -= OnOpened;
        Popup.Closed -= OnClosed;
        _owner.PropertyChanged -= OnOwnerChanged;
        _owner.RemoveHandler(InputElement.GotFocusEvent, OnFocus);
        _owner.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
        _owner.RemoveHandler(InputElement.KeyDownEvent, OnKeyboardActivity);
        _owner.RemoveHandler(InputElement.PointerMovedEvent, OnPointerActivity);
        _owner.RemoveHandler(InputElement.PointerPressedEvent, OnPointerActivity);
        _owner.RemoveHandler(InputElement.KeyDownEvent, BlockExitingInput);
        _owner.RemoveHandler(InputElement.KeyUpEvent, BlockExitingInput);
        _owner.RemoveHandler(InputElement.PointerPressedEvent, BlockExitingPointer);
        _owner.RemoveHandler(InputElement.PointerReleasedEvent, BlockExitingRelease);
        if (_surface is not null) _surface.Presentation = null;
        _running = false;
    }

    private sealed class RootRegistration
    {
        private readonly Control _control;
        internal MenuPopupPresentation? Presentation { get; private set; }

        public RootRegistration(Control control)
        {
            _control = control;
            control.AttachedToVisualTree += (_, _) => Connect();
            control.DetachedFromVisualTree += (_, _) => Disconnect();
            if (control is TemplatedControl templated) templated.TemplateApplied += (_, _) => Connect();
            Connect();
        }

        internal void Connect()
        {
            var popup = _control.FindLogicalAncestorOfType<Popup>();
            if (popup is null || Presentation?.Popup == popup) return;
            Disconnect();
            Presentation = new MenuPopupPresentation(_control, popup, () => ((MenuBase)_control).Close());
            if (_control is ContextMenu context) context.Closing += Presentation.OnClosing;
        }

        private void Disconnect()
        {
            if (Presentation is null) return;
            if (_control is ContextMenu context) context.Closing -= Presentation.OnClosing;
            Presentation.Dispose();
            Presentation = null;
        }
    }
}