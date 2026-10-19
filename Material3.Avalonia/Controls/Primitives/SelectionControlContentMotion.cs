using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class SelectionControlContentMotion : Panel
{
    public static readonly StyledProperty<double> TargetSizeProperty =
        AvaloniaProperty.Register<SelectionControlContentMotion, double>(nameof(TargetSize));

    public static readonly StyledProperty<double> StateLayerSizeProperty =
        AvaloniaProperty.Register<SelectionControlContentMotion, double>(nameof(StateLayerSize));

    public static readonly StyledProperty<double> IndicatorSlotWidthProperty =
        AvaloniaProperty.Register<SelectionControlContentMotion, double>(nameof(IndicatorSlotWidth));

    public static readonly StyledProperty<bool> HasContentProperty =
        AvaloniaProperty.Register<SelectionControlContentMotion, bool>(nameof(HasContent));

    private static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<SelectionControlContentMotion, double>("Progress", 1);

    private Geometry _from;
    private Geometry _target;
    private bool _arranged;
    private double _contentWidth;

    private readonly record struct Geometry(double Width, double Height, double Slot, double Circle)
    {
        public Geometry Interpolate(Geometry target, double progress) => new(
            Width + (target.Width - Width) * progress,
            Height + (target.Height - Height) * progress,
            Slot + (target.Slot - Slot) * progress,
            Circle + (target.Circle - Circle) * progress);
    }

    static SelectionControlContentMotion()
    {
        AffectsMeasure<SelectionControlContentMotion>(TargetSizeProperty, StateLayerSizeProperty,
            IndicatorSlotWidthProperty, HasContentProperty, ProgressProperty);
    }

    public SelectionControlContentMotion()
    {
        ClipToBounds = true;
        Transitions =
        [
            new SpringDoubleTransition
            {
                Property = ProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast
            }
        ];
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

    public double IndicatorSlotWidth
    {
        get => GetValue(IndicatorSlotWidthProperty);
        set => SetValue(IndicatorSlotWidthProperty, value);
    }

    public bool HasContent
    {
        get => GetValue(HasContentProperty);
        set => SetValue(HasContentProperty, value);
    }

    private Geometry Current => _from.Interpolate(_target, GetValue(ProgressProperty));

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var slot = Math.Max(0, HasContent ? IndicatorSlotWidth : TargetSize);
        var circle = Math.Max(0, Math.Min(StateLayerSize, TargetSize));
        Children[0].Measure(new Size(circle, circle));
        // Wrapping uses the destination constraint, never the animated width.
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - slot), double.PositiveInfinity));
        var content = HasContent ? Children[1].DesiredSize : default;
        _contentWidth = content.Width;
        var target = new Geometry(slot + content.Width, Math.Max(TargetSize, content.Height), slot, circle);
        var owner = TemplatedParent as Control;
        var immediate = !_arranged || MotionSettings.ReduceMotion || TopLevel.GetTopLevel(this) is null;
        if (immediate || target != _target)
        {
            var current = Current;
            _from = immediate ? target : current;
            _target = target;
            SetProgressImmediately(immediate ? 1 : 0);
            if (!immediate) SetValue(ProgressProperty, 1);
        }

        var geometry = Current;
        var width = owner is { HorizontalAlignment: HorizontalAlignment.Stretch } ||
                    owner is not null && double.IsFinite(owner.Width)
            ? target.Width
            : geometry.Width;
        var height = owner is { VerticalAlignment: VerticalAlignment.Stretch } ||
                     owner is not null && double.IsFinite(owner.Height)
            ? target.Height
            : geometry.Height;
        return new Size(Math.Max(0, width), Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var geometry = Current;
        var circle = Math.Max(0, geometry.Circle);
        var slot = Math.Max(0, geometry.Slot);
        Children[0].Arrange(new Rect((slot - circle) / 2, (finalSize.Height - circle) / 2, circle, circle));
        var contentWidth = Math.Max(_contentWidth, finalSize.Width - slot);
        var contentHeight = Children[1].DesiredSize.Height;
        Children[1].Arrange(new Rect(slot, 0, Math.Max(0, contentWidth), Math.Max(finalSize.Height, contentHeight)));
        _arranged = true;
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _arranged = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void SetProgressImmediately(double value)
    {
        var transitions = Transitions;
        Transitions = null;
        SetValue(ProgressProperty, value);
        Transitions = transitions;
    }
}