using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class FabContentMotion : Panel
{
    private const double Epsilon = .01;

    public static readonly StyledProperty<bool> IsExtendedProperty =
        AvaloniaProperty.Register<FabContentMotion, bool>(nameof(IsExtended));

    public static readonly StyledProperty<bool> HasIconProperty =
        AvaloniaProperty.Register<FabContentMotion, bool>(nameof(HasIcon));

    public static readonly StyledProperty<double> CollapsedWidthProperty =
        AvaloniaProperty.Register<FabContentMotion, double>(nameof(CollapsedWidth));

    public static readonly StyledProperty<double> ExtendedMinimumWidthProperty =
        AvaloniaProperty.Register<FabContentMotion, double>(nameof(ExtendedMinimumWidth));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<FabContentMotion, double>(nameof(Gap));

    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<FabContentMotion, Thickness>(nameof(ContentPadding));

    public static readonly StyledProperty<IBrush?> IconBrushProperty =
        AvaloniaProperty.Register<FabContentMotion, IBrush?>(nameof(IconBrush));

    public static readonly DirectProperty<FabContentMotion, IBrush?> EffectiveIconBrushProperty =
        AvaloniaProperty.RegisterDirect<FabContentMotion, IBrush?>(nameof(EffectiveIconBrush),
            x => x.EffectiveIconBrush);

    private static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<FabContentMotion, double>("Progress", 1);

    private static readonly StyledProperty<double> LabelOpacityProperty =
        AvaloniaProperty.Register<FabContentMotion, double>("LabelOpacity", 1);

    private FloatingActionButton? _owner;
    private IBrush? _effectiveIconBrush;
    private Geometry _from;
    private Geometry _target;
    private bool _arranged;
    private bool _clipUntilTarget;
    private bool _isWidthConstrained;

    private readonly record struct Geometry(double Width, double IconX, double LabelX)
    {
        public Geometry Interpolate(Geometry target, double p) => new(
            Width + (target.Width - Width) * p,
            IconX + (target.IconX - IconX) * p,
            LabelX + (target.LabelX - LabelX) * p);
    }

    static FabContentMotion()
    {
        AffectsMeasure<FabContentMotion>(IsExtendedProperty, HasIconProperty, CollapsedWidthProperty,
            ExtendedMinimumWidthProperty,
            GapProperty, ContentPaddingProperty, ProgressProperty);
        AffectsArrange<FabContentMotion>(LabelOpacityProperty);
    }

    public FabContentMotion()
    {
        ClipToBounds = true;
        Transitions =
        [
            new SpringDoubleTransition
                { Property = ProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = LabelOpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast }
        ];
    }

    public bool IsExtended
    {
        get => GetValue(IsExtendedProperty);
        set => SetValue(IsExtendedProperty, value);
    }

    public bool HasIcon
    {
        get => GetValue(HasIconProperty);
        set => SetValue(HasIconProperty, value);
    }

    public double CollapsedWidth
    {
        get => GetValue(CollapsedWidthProperty);
        set => SetValue(CollapsedWidthProperty, value);
    }

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public double ExtendedMinimumWidth
    {
        get => GetValue(ExtendedMinimumWidthProperty);
        set => SetValue(ExtendedMinimumWidthProperty, value);
    }

    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public IBrush? IconBrush
    {
        get => GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public IBrush? EffectiveIconBrush
    {
        get => _effectiveIconBrush;
        private set => SetAndRaise(EffectiveIconBrushProperty, ref _effectiveIconBrush, value);
    }

    private Geometry Current => _from.Interpolate(_target, GetValue(ProgressProperty));

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as FloatingActionButton;
        if (_owner is not null)
            _owner.PropertyChanged += OnOwnerPropertyChanged;
        UpdateIconBrush();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null)
            _owner.PropertyChanged -= OnOwnerPropertyChanged;
        _owner = null;
        _arranged = false;
        _clipUntilTarget = false;
        _isWidthConstrained = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconBrushProperty)
            UpdateIconBrush();
        if (change.Property == IsExtendedProperty)
            SetAnimated(LabelOpacityProperty, IsExtended ? 1 : 0, _arranged);
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingActionButton.IsVisibleProperty)
        {
            _arranged = false;
            // Reappearing at the same bounds can otherwise reuse cached layout without arranging this panel.
            if (_owner is { IsVisible: true })
                InvalidateMeasure();
        }

        if (e.Property == FloatingActionButton.ForegroundProperty)
            UpdateIconBrush();
        if (e.Property == FloatingActionButton.HorizontalContentAlignmentProperty)
            InvalidateMeasure();
        if (e.Property == FloatingActionButton.VerticalContentAlignmentProperty)
            InvalidateArrange();
    }

    private void UpdateIconBrush() => EffectiveIconBrush =
        _owner is not null && _owner.GetDiagnostic(FloatingActionButton.ForegroundProperty).Priority <=
        BindingPriority.LocalValue
            ? _owner.Foreground
            : IconBrush;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2)
            return default;
        var icon = Children[0];
        var label = Children[1];
        icon.IsVisible = HasIcon;
        var padding = ContentPadding;
        var childSize = new Size(double.PositiveInfinity,
            Math.Max(0, availableSize.Height - padding.Top - padding.Bottom));
        icon.Measure(childSize);
        label.Measure(childSize);
        var iconWidth = HasIcon ? icon.DesiredSize.Width : 0;
        var labelX = padding.Left + (HasIcon ? iconWidth + Gap : 0);
        var contentWidth = labelX + label.DesiredSize.Width + padding.Right;
        var width = IsExtended
            ? Math.Max(Math.Max(CollapsedWidth, ExtendedMinimumWidth), contentWidth)
            : CollapsedWidth;
        var extendedOffset = IsExtended ? ContentOffset(width - contentWidth) : 0;
        var target = new Geometry(width,
            IsExtended ? padding.Left + extendedOffset : (width - iconWidth) / 2,
            IsExtended ? labelX + extendedOffset : width - padding.Right);
        _isWidthConstrained = double.IsFinite(availableSize.Width) && width > availableSize.Width + Epsilon;
        var animate = _arranged && IsEffectivelyVisible && TopLevel.GetTopLevel(this) is not null &&
                      !MotionSettings.ReduceMotion;
        if (target != _target || !animate)
        {
            _clipUntilTarget = animate && target.Width > Current.Width + Epsilon;
            _from = animate ? Current : target;
            _target = target;
            SetAnimated(ProgressProperty, animate ? 0 : 1, false);
            if (animate)
                SetValue(ProgressProperty, 1d);
        }

        if (!animate)
            SetAnimated(LabelOpacityProperty, IsExtended ? 1 : 0, false);

        if (_clipUntilTarget && Current.Width >= _target.Width - Epsilon)
            _clipUntilTarget = false;
        ClipToBounds = _isWidthConstrained || _clipUntilTarget || !IsExtended;

        return new Size(Math.Max(0, Current.Width),
            Math.Max(HasIcon ? icon.DesiredSize.Height : 0, IsExtended ? label.DesiredSize.Height : 0) + padding.Top +
            padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2)
            return finalSize;
        var current = Current;
        var remaining = Math.Max(0, finalSize.Width - current.Width);
        var offset = ContentOffset(remaining);
        var icon = Children[0];
        var label = Children[1];
        var iconX = offset + current.IconX;
        icon.Arrange(new Rect(iconX,
            ContentY(icon.DesiredSize.Height, finalSize.Height),
            icon.DesiredSize.Width, icon.DesiredSize.Height));
        var labelX = offset + current.LabelX;
        var labelWidth = Math.Max(0, finalSize.Width - labelX - ContentPadding.Right);
        var arrangedLabelWidth = Math.Max(label.DesiredSize.Width, labelWidth);
        if (ClipToBounds)
        {
            var clipRect = new Rect(0, 0, labelWidth, finalSize.Height);
            // Replacing the clip forwards the new platform geometry to Avalonia's compositor.
            if (label.Clip is not RectangleGeometry clip || clip.Rect != clipRect)
                label.Clip = new RectangleGeometry(clipRect);
        }
        else
            label.Clip = null;

        label.Opacity = Math.Clamp(GetValue(LabelOpacityProperty), 0, 1);
        label.Arrange(new Rect(labelX,
            ContentY(label.DesiredSize.Height, finalSize.Height), arrangedLabelWidth, label.DesiredSize.Height));
        _arranged = true;
        return finalSize;
    }

    private double ContentOffset(double remaining) =>
        (_owner?.HorizontalContentAlignment ?? HorizontalAlignment.Center) switch
        {
            HorizontalAlignment.Right => remaining,
            HorizontalAlignment.Center => remaining / 2,
            _ => 0
        };

    private double ContentY(double height, double availableHeight) => ContentPadding.Top +
                                                                      (_owner?.VerticalContentAlignment switch
                                                                      {
                                                                          VerticalAlignment.Top => 0,
                                                                          VerticalAlignment.Bottom => availableHeight -
                                                                              ContentPadding.Top -
                                                                              ContentPadding.Bottom - height,
                                                                          _ => (availableHeight - ContentPadding.Top -
                                                                              ContentPadding.Bottom - height) / 2
                                                                      });

    private void SetAnimated(StyledProperty<double> property, double value, bool animate)
    {
        var transitions = Transitions;
        if (!animate || MotionSettings.ReduceMotion)
            Transitions = null;
        SetValue(property, value);
        Transitions = transitions;
    }
}