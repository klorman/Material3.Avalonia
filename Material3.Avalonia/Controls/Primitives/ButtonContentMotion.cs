using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class ButtonContentMotion : Panel
{
    private const double Epsilon = 0.01;

    public static readonly StyledProperty<bool> IsIconPresentedProperty =
        AvaloniaProperty.Register<ButtonContentMotion, bool>(nameof(IsIconPresented));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<ButtonContentMotion, double>(nameof(Gap));

    private static readonly StyledProperty<double> AnimatedWidthProperty =
        AvaloniaProperty.Register<ButtonContentMotion, double>(nameof(AnimatedWidth));

    private static readonly StyledProperty<double> IconProgressProperty =
        AvaloniaProperty.Register<ButtonContentMotion, double>(nameof(IconProgress));

    private static readonly StyledProperty<double> IconOpacityProperty =
        AvaloniaProperty.Register<ButtonContentMotion, double>(nameof(IconOpacity));

    private bool _attached;
    private bool _hasArranged;
    private bool _clipUntilTarget;
    private bool _isWidthConstrained;
    private double _targetWidth;

    static ButtonContentMotion()
    {
        AffectsMeasure<ButtonContentMotion>(AnimatedWidthProperty, IsIconPresentedProperty, GapProperty);
        AffectsArrange<ButtonContentMotion>(IconProgressProperty, IconOpacityProperty, GapProperty);
    }

    public ButtonContentMotion()
    {
        ClipToBounds = true;
        Transitions =
        [
            new SpringDoubleTransition
            {
                Property = AnimatedWidthProperty,
                Style = MotionStyle.Spatial,
                Speed = MotionSpeed.Fast
            },
            new SpringDoubleTransition
            {
                Property = IconProgressProperty,
                Style = MotionStyle.Spatial,
                Speed = MotionSpeed.Fast
            },
            new SpringDoubleTransition
            {
                Property = IconOpacityProperty,
                Style = MotionStyle.Effects,
                Speed = MotionSpeed.Fast
            }
        ];
    }

    public bool IsIconPresented
    {
        get => GetValue(IsIconPresentedProperty);
        set => SetValue(IsIconPresentedProperty, value);
    }

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    private double AnimatedWidth
    {
        get => GetValue(AnimatedWidthProperty);
        set => SetValue(AnimatedWidthProperty, value);
    }

    private double IconProgress
    {
        get => GetValue(IconProgressProperty);
        set => SetValue(IconProgressProperty, value);
    }

    private double IconOpacity
    {
        get => GetValue(IconOpacityProperty);
        set => SetValue(IconOpacityProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        SetIconImmediately(IsIconPresented ? 1 : 0);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _hasArranged = false;
        _clipUntilTarget = false;
        _isWidthConstrained = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsIconPresentedProperty && _attached)
        {
            var target = IsIconPresented ? 1 : 0;
            IconProgress = target;
            IconOpacity = target;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2)
            return default;

        var iconSlot = Children[0];
        var content = Children[1];
        iconSlot.Measure(availableSize.WithWidth(double.PositiveInfinity));
        content.Measure(availableSize.WithWidth(double.PositiveInfinity));

        var iconExtent = IsIconPresented ? iconSlot.DesiredSize.Width + Gap : 0;
        var target = iconExtent + content.DesiredSize.Width;
        _isWidthConstrained = double.IsFinite(availableSize.Width) && target > availableSize.Width + Epsilon;
        if (!_hasArranged || MotionSettings.ReduceMotion || TopLevel.GetTopLevel(this) is null)
        {
            SetWidthImmediately(target);
            _clipUntilTarget = false;
        }
        else if (Math.Abs(target - _targetWidth) > Epsilon)
        {
            _clipUntilTarget = target > AnimatedWidth + Epsilon;
            _targetWidth = target;
            AnimatedWidth = target;
        }

        if (_clipUntilTarget && AnimatedWidth >= _targetWidth - Epsilon)
            _clipUntilTarget = false;
        ClipToBounds = _isWidthConstrained || _clipUntilTarget;

        var height = Math.Max(iconSlot.DesiredSize.Height, content.DesiredSize.Height);
        return new Size(Math.Max(0, AnimatedWidth), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2)
            return finalSize;

        var iconSlot = Children[0];
        var content = Children[1];
        var progress = Math.Max(0, IconProgress);
        var iconWidth = iconSlot.DesiredSize.Width * progress;
        var gap = Gap * progress;
        var contentWidth = Math.Max(content.DesiredSize.Width, finalSize.Width - iconWidth - gap);
        var contentX = Math.Min(finalSize.Width, iconWidth + gap);

        iconSlot.Opacity = Math.Clamp(IconOpacity, 0, 1);
        iconSlot.Arrange(new Rect(0, (finalSize.Height - iconSlot.DesiredSize.Height) / 2,
            iconWidth, iconSlot.DesiredSize.Height));
        content.Arrange(new Rect(contentX, (finalSize.Height - content.DesiredSize.Height) / 2,
            contentWidth, content.DesiredSize.Height));
        _hasArranged = true;
        return finalSize;
    }

    private void SetWidthImmediately(double width)
    {
        _targetWidth = width;
        SetImmediately(AnimatedWidthProperty, width);
    }

    private void SetIconImmediately(double value)
    {
        SetImmediately(IconProgressProperty, value);
        SetImmediately(IconOpacityProperty, value);
    }

    private void SetImmediately(AvaloniaProperty<double> property, double value)
    {
        var transitions = Transitions;
        Transitions = null;
        SetValue(property, value);
        Transitions = transitions;
    }
}