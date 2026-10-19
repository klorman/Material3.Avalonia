using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class SwitchKnobHost : Panel
{
    public static readonly StyledProperty<double> TrackWidthProperty =
        AvaloniaProperty.Register<SwitchKnobHost, double>(nameof(TrackWidth));

    public static readonly StyledProperty<double> TrackHeightProperty =
        AvaloniaProperty.Register<SwitchKnobHost, double>(nameof(TrackHeight));

    public static readonly StyledProperty<double> TargetSizeProperty =
        AvaloniaProperty.Register<SwitchKnobHost, double>(nameof(TargetSize));

    public static readonly StyledProperty<double> StateLayerSizeProperty =
        AvaloniaProperty.Register<SwitchKnobHost, double>(nameof(StateLayerSize));

    private Control? _moving;
    private ToggleSwitch? _owner;
    private readonly TranslateTransform _translation = new();
    private bool _arranged;
    private double _travel;

    static SwitchKnobHost() => AffectsMeasure<SwitchKnobHost>(TrackWidthProperty, TrackHeightProperty,
        TargetSizeProperty, StateLayerSizeProperty);

    public double TrackWidth
    {
        get => GetValue(TrackWidthProperty);
        set => SetValue(TrackWidthProperty, value);
    }

    public double TrackHeight
    {
        get => GetValue(TrackHeightProperty);
        set => SetValue(TrackHeightProperty, value);
    }

    public double TargetSize
    {
        get => GetValue(TargetSizeProperty);
        set => SetValue(TargetSizeProperty, value);
    }

    public double StateLayerSize
    {
        get => GetValue(StateLayerSizeProperty);
        set => SetValue(StateLayerSizeProperty, value);
    }

    internal double Position => _translation.X;

    internal double CircleSize => TrackHeight > 0 && TargetSize > 0
        ? Math.Min(StateLayerSize, TargetSize) / Math.Min(1, TargetSize / TrackHeight)
        : 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(CircleSize, CircleSize));
        return new Size(Math.Max(0, TrackWidth - TrackHeight), Math.Max(0, TrackHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 1) return finalSize;
        if (_owner != TemplatedParent)
        {
            if (_owner is not null) _owner.PropertyChanged -= OnOwnerChanged;
            _owner = TemplatedParent as ToggleSwitch;
            if (_owner is not null) _owner.PropertyChanged += OnOwnerChanged;
        }

        var moving = Children[0];
        if (_moving != moving)
        {
            if (_moving is not null) _moving.PropertyChanged -= OnMovingChanged;
            _moving = moving;
            moving.RenderTransform = _translation;
            moving.PropertyChanged += OnMovingChanged;
        }

        var circle = Math.Max(0, CircleSize);
        moving.Arrange(new Rect((TrackHeight - circle) / 2, (finalSize.Height - circle) / 2, circle, circle));
        if (!_arranged || _travel != finalSize.Width)
        {
            _travel = finalSize.Width;
            if (TemplatedParent is ToggleSwitch owner && !owner.Classes.Contains(":dragging"))
            {
                var transitions = moving.Transitions;
                moving.Transitions = null;
                Canvas.SetLeft(moving, owner.IsChecked == true ? _travel : 0);
                moving.Transitions = transitions;
            }
        }

        UpdateTranslation();
        _arranged = true;
        return finalSize;
    }

    private void OnMovingChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Canvas.LeftProperty) UpdateTranslation();
    }

    private void OnOwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Native ToggleSwitch leaves the previous position when IsChecked becomes null.
        if (e.Property == ToggleButton.IsCheckedProperty && _owner?.IsChecked is null && _moving is not null)
            Canvas.SetLeft(_moving, 0);
    }

    private void UpdateTranslation()
    {
        // Native ToggleSwitch writes Canvas.Left; this non-Canvas parent keeps motion out of layout.
        if (_moving is not null && double.IsFinite(Canvas.GetLeft(_moving)))
            _translation.X = Canvas.GetLeft(_moving);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OnOwnerChanged;
        _owner = null;
        if (_moving is not null) _moving.PropertyChanged -= OnMovingChanged;
        _moving = null;
        _arranged = false;
        base.OnDetachedFromVisualTree(e);
    }
}