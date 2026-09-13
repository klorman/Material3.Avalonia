using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Material3.Avalonia.Attached.Controls.Internal;

namespace Material3.Avalonia.Controls.Primitives;

public enum RippleStackingMode
{
    All,
    LatestOnly
}

public class InkRipple : Control
{
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<InkRipple, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<InkRipple, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<bool> BoundedProperty =
        AvaloniaProperty.Register<InkRipple, bool>(nameof(Bounded), true);

    public static readonly StyledProperty<double> BaseOpacityProperty =
        AvaloniaProperty.Register<InkRipple, double>(nameof(BaseOpacity), 0.1);

    public static readonly StyledProperty<TimeSpan> GrowDurationProperty =
        AvaloniaProperty.Register<InkRipple, TimeSpan>(nameof(GrowDuration), TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<TimeSpan> FadeInDurationProperty =
        AvaloniaProperty.Register<InkRipple, TimeSpan>(nameof(FadeInDuration), TimeSpan.FromMilliseconds(100));

    public static readonly StyledProperty<TimeSpan> FadeOutDurationProperty =
        AvaloniaProperty.Register<InkRipple, TimeSpan>(nameof(FadeOutDuration), TimeSpan.FromMilliseconds(200));

    public static readonly StyledProperty<RippleStackingMode> StackingModeProperty =
        AvaloniaProperty.Register<InkRipple, RippleStackingMode>(nameof(StackingMode));

    public static readonly StyledProperty<Easing?> GrowEasingProperty =
        AvaloniaProperty.Register<InkRipple, Easing?>(nameof(GrowEasing), new SplineEasing());


    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public bool Bounded
    {
        get => GetValue(BoundedProperty);
        set => SetValue(BoundedProperty, value);
    }

    public double BaseOpacity
    {
        get => GetValue(BaseOpacityProperty);
        set => SetValue(BaseOpacityProperty, value);
    }

    public TimeSpan GrowDuration
    {
        get => GetValue(GrowDurationProperty);
        set => SetValue(GrowDurationProperty, value);
    }

    public TimeSpan FadeInDuration
    {
        get => GetValue(FadeInDurationProperty);
        set => SetValue(FadeInDurationProperty, value);
    }

    public TimeSpan FadeOutDuration
    {
        get => GetValue(FadeOutDurationProperty);
        set => SetValue(FadeOutDurationProperty, value);
    }

    public RippleStackingMode StackingMode
    {
        get => GetValue(StackingModeProperty);
        set => SetValue(StackingModeProperty, value);
    }

    public Easing? GrowEasing
    {
        get => GetValue(GrowEasingProperty);
        set => SetValue(GrowEasingProperty, value);
    }

    static InkRipple()
    {
        AffectsRender<InkRipple>(BrushProperty, CornerRadiusProperty);
    }

    public InkRipple()
    {
        IsHitTestVisible = false;
    }

    private readonly Dictionary<IPointer, Interaction> _presses = new();
    private InputElement? _host;
    private TopLevel? _root;
    private CompositionCustomVisual? _visual;
    private Interaction? _keyboardPress;
    private Key? _keyboardKey;
    private long _nextId;
    private int _interactionCommitGeneration;

    private sealed record Interaction(long Id, Point Center);

    internal Func<bool>? AcceptActivation { get; set; }
    internal event Action? PressEnded;
    internal Func<PointerPressedEventArgs, bool>? AcceptPress { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (ElementComposition.GetElementVisual(this) is { } visual)
        {
            _visual = visual.Compositor.CreateCustomVisual(new RippleVisual());
            ElementComposition.SetElementChildVisual(this, _visual);
            UpdateVisual();
        }

        if (TemplatedParent is not InputElement host) return;
        _host = host;
        _root = TopLevel.GetTopLevel(this);
        if (_root is WindowBase window) window.Deactivated += OnDeactivated;
        host.AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, true);
        host.AddHandler(PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble,
            true);
        host.AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Direct, true);
        host.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, true);
        host.AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, true);
        host.AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, true);
        host.PropertyChanged += OnHostChanged;
        _root?.AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, true);
        _root?.AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, true);
        _root?.AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_host is { } host)
        {
            host.RemoveHandler(PointerPressedEvent, OnPressed);
            host.RemoveHandler(PointerCaptureLostEvent, OnCaptureLost);
            host.RemoveHandler(PointerExitedEvent, OnPointerExited);
            host.RemoveHandler(KeyDownEvent, OnKeyDown);
            host.RemoveHandler(KeyUpEvent, OnKeyUp);
            host.RemoveHandler(PointerReleasedEvent, OnReleased);
            host.PropertyChanged -= OnHostChanged;
        }

        if (_root is WindowBase window) window.Deactivated -= OnDeactivated;
        _root?.RemoveHandler(KeyUpEvent, OnKeyUp);
        _root?.RemoveHandler(PointerReleasedEvent, OnReleased);
        _root?.RemoveHandler(PointerMovedEvent, OnMoved);
        CancelPress();
        ElementComposition.SetElementChildVisual(this, null);
        _visual = null;
        _host = null;
        _root = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty || change.Property == BrushProperty ||
            change.Property == CornerRadiusProperty || change.Property == BoundedProperty)
            UpdateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_visual is null) return;
        _visual.Size = new Vector(Bounds.Width, Bounds.Height);
        _visual.SendHandlerMessage(new RippleVisual.Appearance(Brush?.ToImmutable(), CornerRadius, Bounded));
    }

    private void OnDeactivated(object? sender, EventArgs e) => CancelPress();

    private void OnHostChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsEffectivelyEnabledProperty && _host?.IsEffectivelyEnabled == false) CancelPress();
        if (e.Property == IsFocusedProperty && _host?.IsFocused == false) EndKeyboard();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source != _host || _keyboardPress is not null || _host?.IsEffectivelyEnabled != true ||
            Brush is null || AcceptActivation?.Invoke() == false) return;
        var activates = _host is Button or global::Material3.Avalonia.Controls.Card { IsInteractive: true }
            ? e.Key is Key.Enter or Key.Space
            : _host is MenuItem item && (e.Key == Key.Enter ||
                                         e.Key == Key.Space && MenuItemPresentation.GetIsEnabled(item) &&
                                         e.KeyModifiers == KeyModifiers.None);
        if (!activates) return;
        _keyboardKey = e.Key;
        _keyboardPress = CreateRipple(new Rect(Bounds.Size).Center);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == _keyboardKey) EndKeyboard();
    }

    private void EndKeyboard()
    {
        if (_keyboardPress is not { } press) return;
        _keyboardPress = null;
        _keyboardKey = null;
        EndInteraction(press);
    }

    internal void CancelPress()
    {
        _presses.Clear();
        _keyboardPress = null;
        _keyboardKey = null;
        SendInteraction(RippleVisual.Clear.Instance);
        PressEnded?.Invoke();
    }

    internal static bool IsPrimaryPress(PointerPressedEventArgs e, Visual relativeTo)
    {
        var point = e.GetCurrentPoint(relativeTo);
        return point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed &&
               !point.Properties.IsBarrelButtonPressed && !point.Properties.IsEraser;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_host?.IsEffectivelyEnabled != true || Brush is null || !IsPrimaryPress(e, this) ||
            AcceptPress?.Invoke(e) == false) return;
        EndKeyboard();
        EndPointer(e.Pointer);
        _presses[e.Pointer] = CreateRipple(e.GetPosition(this));
    }

    private void SendInteraction(object message)
    {
        if (_visual is not { } visual) return;
        var generation = ++_interactionCommitGeneration;
        visual.SendHandlerMessage(message);
        var committed = visual.Compositor.RequestCommitAsync().ConfigureAwait(false).GetAwaiter();
        // Messages install animation-clock work after the batch. Wake idle render loops
        // with a following commit, without scheduling animation frames on the UI thread.
        committed.OnCompleted(() => Dispatcher.UIThread.Post(() =>
        {
            committed.GetResult();
            if (_visual == visual && generation == _interactionCommitGeneration)
                visual.Compositor.RequestCommitAsync();
        }));
    }

    private Interaction CreateRipple(Point position)
    {
        var interaction = new Interaction(++_nextId, position);
        // Custom Easing instances can be mutable: sample on the UI thread, never share them with the renderer.
        var easing = new double[1025];
        var grow = GrowEasing ?? new CubicEaseOut();
        for (var i = 0; i < easing.Length; i++) easing[i] = grow.Ease((double)i / (easing.Length - 1));
        var x = Math.Max(Math.Abs(position.X), Math.Abs(Bounds.Width - position.X));
        var y = Math.Max(Math.Abs(position.Y), Math.Abs(Bounds.Height - position.Y));
        SendInteraction(new RippleVisual.Start(interaction.Id, position, Math.Sqrt(x * x + y * y),
            BaseOpacity, global::Material3.Avalonia.Motion.MotionSettings.ReduceMotion ? TimeSpan.Zero : GrowDuration,
            FadeInDuration, easing, StackingMode == RippleStackingMode.LatestOnly));
        return interaction;
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndPointer(e.Pointer);
    private void OnReleased(object? sender, PointerEventArgs e) => EndPointer(e.Pointer);
    private void OnPointerExited(object? sender, PointerEventArgs e) => EndPointer(e.Pointer);

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_presses.TryGetValue(e.Pointer, out var press)) return;
        var point = e.GetPosition(this);
        if (!new Rect(Bounds.Size).Contains(point) || e.Pointer.Type == PointerType.Touch &&
            Math.Abs(point.X - press.Center.X) + Math.Abs(point.Y - press.Center.Y) > 8)
            EndPointer(e.Pointer);
    }

    private void EndPointer(IPointer pointer)
    {
        if (!_presses.Remove(pointer, out var press)) return;
        EndInteraction(press);
    }

    private void EndInteraction(Interaction press)
    {
        SendInteraction(new RippleVisual.End(press.Id, FadeOutDuration));
        PressEnded?.Invoke();
    }
}