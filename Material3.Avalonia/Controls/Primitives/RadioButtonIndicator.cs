using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class RadioButtonIndicator : Control
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, bool>(nameof(IsSelected));

    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, IBrush?>(nameof(Brush));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, double>(nameof(IconSize));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, double>(nameof(StrokeThickness));

    public static readonly StyledProperty<double> DotSizeProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, double>(nameof(DotSize));

    private static readonly StyledProperty<double> SelectionProgressProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, double>("SelectionProgress");

    private static readonly StyledProperty<double> DotOpacityProperty =
        AvaloniaProperty.Register<RadioButtonIndicator, double>("DotOpacity");

    private bool _arranged;

    static RadioButtonIndicator()
    {
        AffectsRender<RadioButtonIndicator>(BrushProperty, IconSizeProperty,
            StrokeThicknessProperty, DotSizeProperty, SelectionProgressProperty, DotOpacityProperty);
    }

    public RadioButtonIndicator()
    {
        IsHitTestVisible = false;
        UseLayoutRounding = false;
        Transitions =
        [
            new SpringDoubleTransition
            {
                Property = SelectionProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast
            },
            new SpringDoubleTransition
            {
                Property = DotOpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast
            },
            new SpringBrushTransition
                { Property = BrushProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast }
        ];
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double DotSize
    {
        get => GetValue(DotSizeProperty);
        set => SetValue(DotSizeProperty, value);
    }

    internal double IndicatorScale => IconSize > 0
        ? Math.Clamp(Math.Min(Bounds.Width, Bounds.Height) / IconSize, 0, 1)
        : 0;

    internal double DotScale => 0.5 + 0.5 * Math.Clamp(GetValue(SelectionProgressProperty), 0, 1);
    internal double DotOpacity => Math.Clamp(GetValue(DotOpacityProperty), 0, 1);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = IndicatorScale;
        if (Brush is null || scale <= 0 || IconSize <= 0) return;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var stroke = Math.Clamp(StrokeThickness, 0, IconSize) * scale;
        var radius = (IconSize * scale - stroke) / 2;
        context.DrawEllipse(null, new Pen(Brush, stroke), center, radius, radius);
        if (DotOpacity <= 0) return;

        // The spec videos reveal the dot at roughly half size; the ring never grows with it.
        var dotRadius = Math.Clamp(DotSize, 0, IconSize) * scale * DotScale / 2;
        using (context.PushOpacity(DotOpacity))
            context.DrawEllipse(Brush, null, center, dotRadius, dotRadius);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty || change.Property == IsEffectivelyEnabledProperty)
            UpdateSelection();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!_arranged) UpdateSelection();
        _arranged = true;
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _arranged = false;
        UpdateSelection();
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateSelection()
    {
        var immediate = !_arranged || !IsEffectivelyEnabled || MotionSettings.ReduceMotion;
        var transitions = Transitions;
        if (immediate) Transitions = null;
        SetValue(SelectionProgressProperty, IsSelected ? 1 : 0);
        SetValue(DotOpacityProperty, IsSelected ? 1 : 0);
        if (immediate) Transitions = transitions;
    }
}