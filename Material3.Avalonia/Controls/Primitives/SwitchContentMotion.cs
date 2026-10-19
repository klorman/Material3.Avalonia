using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class SwitchContentMotion : Panel
{
    public static readonly StyledProperty<double> TargetSizeProperty =
        AvaloniaProperty.Register<SwitchContentMotion, double>(nameof(TargetSize));

    public static readonly StyledProperty<double> TrackWidthProperty =
        AvaloniaProperty.Register<SwitchContentMotion, double>(nameof(TrackWidth));

    public static readonly StyledProperty<double> TrackHeightProperty =
        AvaloniaProperty.Register<SwitchContentMotion, double>(nameof(TrackHeight));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<SwitchContentMotion, double>(nameof(Gap));

    public static readonly StyledProperty<SwitchLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<SwitchContentMotion, SwitchLabelPlacement>(nameof(LabelPlacement));

    public static readonly DirectProperty<SwitchContentMotion, double> CurrentTargetSizeProperty =
        AvaloniaProperty.RegisterDirect<SwitchContentMotion, double>(nameof(CurrentTargetSize),
            x => x.CurrentTargetSize);

    private static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<SwitchContentMotion, double>("Progress", 1);

    private Geometry _from;
    private Geometry _target;
    private bool _arranged;
    private bool _hasContent;
    private double _contentWidth;
    private double _currentTargetSize;
    private readonly RectangleGeometry _labelClip = new();

    private readonly record struct Geometry(double Width, double Height, double Target)
    {
        public Geometry Interpolate(Geometry target, double p) => new(Width + (target.Width - Width) * p,
            Height + (target.Height - Height) * p, Target + (target.Target - Target) * p);
    }

    static SwitchContentMotion() => AffectsMeasure<SwitchContentMotion>(TargetSizeProperty, TrackWidthProperty,
        TrackHeightProperty, GapProperty, LabelPlacementProperty, ProgressProperty);

    public SwitchContentMotion()
    {
        Transitions =
        [
            new SpringDoubleTransition
                { Property = ProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast }
        ];
    }

    public double TargetSize
    {
        get => GetValue(TargetSizeProperty);
        set => SetValue(TargetSizeProperty, value);
    }

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

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public SwitchLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public double CurrentTargetSize
    {
        get => _currentTargetSize;
        private set => SetAndRaise(CurrentTargetSizeProperty, ref _currentTargetSize, value);
    }

    internal double IndicatorScale => TrackHeight > 0 ? Math.Clamp(CurrentTargetSize / TrackHeight, 0, 1) : 0;
    private Geometry Current => _from.Interpolate(_target, GetValue(ProgressProperty));

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var owner = TemplatedParent as ToggleSwitch;
        _hasContent = owner is not null &&
                      (owner.Content is not null || owner.OnContent is not null || owner.OffContent is not null);
        var slot = _hasContent ? TrackWidth + Gap : 0;
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - slot), double.PositiveInfinity));
        var content = _hasContent ? Children[1].DesiredSize : default;
        _contentWidth = content.Width;
        var scale = TrackHeight > 0 ? Math.Clamp(TargetSize / TrackHeight, 0, 1) : 0;
        var target = new Geometry(_hasContent ? slot + content.Width : Math.Max(TargetSize, TrackWidth * scale),
            Math.Max(TargetSize, content.Height), TargetSize);
        var immediate = !_arranged || MotionSettings.ReduceMotion || TopLevel.GetTopLevel(this) is null;
        if (immediate || target != _target)
        {
            _from = immediate ? target : Current;
            _target = target;
            var transitions = Transitions;
            Transitions = null;
            SetValue(ProgressProperty, immediate ? 1 : 0);
            Transitions = transitions;
            if (!immediate) SetValue(ProgressProperty, 1);
        }

        var current = Current;
        CurrentTargetSize = Math.Max(0, current.Target);
        Children[0].Measure(new Size(Math.Max(0, TrackWidth * IndicatorScale),
            Math.Max(0, TrackHeight * IndicatorScale)));
        return new Size(
            Math.Max(0,
                owner is { HorizontalAlignment: HorizontalAlignment.Stretch } ||
                owner is not null && double.IsFinite(owner.Width)
                    ? target.Width
                    : current.Width),
            Math.Max(0,
                owner is { VerticalAlignment: VerticalAlignment.Stretch } ||
                owner is not null && double.IsFinite(owner.Height)
                    ? target.Height
                    : current.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var scale = IndicatorScale;
        var width = Math.Max(0, TrackWidth * scale);
        var height = Math.Max(0, TrackHeight * scale);
        var slot = _hasContent ? TrackWidth : finalSize.Width;
        var leading = LabelPlacement == SwitchLabelPlacement.Leading;
        var switchX = _hasContent && leading ? finalSize.Width - slot : 0;
        Children[0].Arrange(new Rect(switchX + (slot - width) / 2, (finalSize.Height - height) / 2, width, height));
        var labelX = leading ? 0 : slot + Gap;
        var visibleWidth = Math.Max(0, finalSize.Width - slot - (_hasContent ? Gap : 0));
        _labelClip.Rect = new Rect(0, 0, visibleWidth, finalSize.Height);
        Children[1].Clip = _labelClip;
        Children[1].Arrange(new Rect(labelX, 0, Math.Max(visibleWidth, _contentWidth), finalSize.Height));
        _arranged = true;
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _arranged = false;
        base.OnDetachedFromVisualTree(e);
    }
}