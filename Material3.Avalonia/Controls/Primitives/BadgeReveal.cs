using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class BadgeReveal : Decorator
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<BadgeReveal, double>(nameof(Progress), 1);

    public static readonly StyledProperty<bool> IsLargeProperty =
        AvaloniaProperty.Register<BadgeReveal, bool>(nameof(IsLarge));

    public static readonly StyledProperty<double> RenderedWidthProperty =
        AvaloniaProperty.Register<BadgeReveal, double>(nameof(RenderedWidth));

    private readonly SpringDoubleTransition _widthTransition = new()
        { Property = RenderedWidthProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast, Damping = 1 };

    private double _targetWidth = double.NaN;

    public double RenderedWidth
    {
        get => GetValue(RenderedWidthProperty);
        set => SetValue(RenderedWidthProperty, value);
    }

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<BadgeReveal>();

    public static readonly StyledProperty<BackgroundSizing> BackgroundSizingProperty =
        Border.BackgroundSizingProperty.AddOwner<BadgeReveal>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<BadgeReveal>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<BadgeReveal>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<BadgeReveal>();

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsLarge
    {
        get => GetValue(IsLargeProperty);
        set => SetValue(IsLargeProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public BackgroundSizing BackgroundSizing
    {
        get => GetValue(BackgroundSizingProperty);
        set => SetValue(BackgroundSizingProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public event EventHandler? Hidden;

    static BadgeReveal()
    {
        AffectsMeasure<BadgeReveal>(BorderThicknessProperty);
        AffectsRender<BadgeReveal>(BackgroundProperty, BackgroundSizingProperty, BorderBrushProperty,
            BorderThicknessProperty, CornerRadiusProperty);
    }

    public BadgeReveal()
    {
        Transitions =
        [
            // Both ring boundaries share one critically damped spring so the hole cannot reopen.
            new SpringDoubleTransition
                { Property = ProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast, Damping = 1 },
            _widthTransition
        ];
    }

    public void SetPresented(bool presented, bool animate)
    {
        if (!animate || MotionSettings.ReduceMotion)
        {
            var transitions = Transitions;
            Transitions = null;
            Progress = presented ? 1 : 0;
            Transitions = transitions;
        }
        else
            Progress = presented ? 1 : 0;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsLargeProperty)
            _targetWidth = double.NaN;
        if (change.Property == ProgressProperty || change.Property == BoundsProperty ||
            change.Property == CornerRadiusProperty || change.Property == IsLargeProperty ||
            change.Property == RenderedWidthProperty)
        {
            UpdateClip();
            InvalidateVisual();
            if (change.Property == ProgressProperty && Progress == 0)
                Hidden?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var inset = Padding + BorderThickness;
        Child?.Measure(availableSize.Deflate(inset));
        return (Child?.DesiredSize ?? default).Inflate(inset);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize).Deflate(Padding + BorderThickness));
        if (_targetWidth != finalSize.Width)
        {
            var animate = IsLarge && !double.IsNaN(_targetWidth) && Progress > 0 &&
                          IsEffectivelyVisible && !MotionSettings.ReduceMotion;
            _targetWidth = finalSize.Width;
            if (!animate)
                Transitions?.Remove(_widthTransition);
            RenderedWidth = finalSize.Width;
            if (!animate)
                Transitions?.Add(_widthTransition);
        }

        UpdateClip();
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _targetWidth = double.NaN;
    }

    private Rect SurfaceRect => new(0, 0, Math.Max(0, RenderedWidth), Bounds.Height);

    private void UpdateClip()
    {
        // Reveal the complete capsule through a straight edge without reshaping its corners.
        Clip = IsLarge
            ? new RectangleGeometry(new Rect(0, 0,
                SurfaceRect.Width * Math.Clamp(Progress, 0, 1), SurfaceRect.Height))
            : null;
        if (Child is null)
            return;
        Child.Clip = IsLarge
            ? Geometry(SurfaceRect.Translate(-Child.Bounds.Position), CornerRadius)
            : null;
    }

    public override void Render(DrawingContext context)
    {
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress == 0)
            return;

        var rect = IsLarge ? SurfaceRect : new Rect(Bounds.Size);
        if (!IsLarge && progress < 1)
        {
            var center = rect.Center;
            var outerScale = 2 - progress;
            var innerScale = 2 * (1 - progress);
            var outer = new Rect(center.X - rect.Width * outerScale / 2, center.Y - rect.Height * outerScale / 2,
                rect.Width * outerScale, rect.Height * outerScale);
            var inner = new Rect(center.X - rect.Width * innerScale / 2, center.Y - rect.Height * innerScale / 2,
                rect.Width * innerScale, rect.Height * innerScale);
            var ring = new CombinedGeometry(GeometryCombineMode.Exclude,
                Geometry(outer, Scale(CornerRadius, outerScale)), Geometry(inner, Scale(CornerRadius, innerScale)));
            using (context.PushGeometryClip(ring))
                DrawSurface(context, outer, Scale(CornerRadius, outerScale));
            return;
        }

        DrawSurface(context, rect, CornerRadius);
    }

    private void DrawSurface(DrawingContext context, Rect rect, CornerRadius corners)
    {
        var inner = rect.Deflate(BorderThickness);
        var innerCorners = new CornerRadius(
            Math.Max(0, corners.TopLeft - Math.Max(BorderThickness.Top, BorderThickness.Left)),
            Math.Max(0, corners.TopRight - Math.Max(BorderThickness.Top, BorderThickness.Right)),
            Math.Max(0, corners.BottomRight - Math.Max(BorderThickness.Bottom, BorderThickness.Right)),
            Math.Max(0, corners.BottomLeft - Math.Max(BorderThickness.Bottom, BorderThickness.Left)));
        var insetBackground = BackgroundSizing == BackgroundSizing.InnerBorderEdge;
        context.DrawRectangle(Background, null, new RoundedRect(insetBackground ? inner : rect,
            insetBackground ? innerCorners : corners));
        if (BorderBrush is not null && BorderThickness != default)
        {
            var outer = Geometry(rect, corners);
            Geometry border = inner.Width > 0 && inner.Height > 0
                ? new CombinedGeometry(GeometryCombineMode.Exclude, outer, Geometry(inner, innerCorners))
                : outer;
            context.DrawGeometry(BorderBrush, null, border);
        }
    }

    private static CornerRadius Scale(CornerRadius corners, double scale) => new(
        corners.TopLeft * scale, corners.TopRight * scale, corners.BottomRight * scale, corners.BottomLeft * scale);

    private static StreamGeometry Geometry(Rect rect, CornerRadius corners)
    {
        var geometry = new StreamGeometry();
        using var path = geometry.Open();
        var max = Math.Max(0, Math.Min(rect.Width, rect.Height) / 2);
        var tl = Math.Min(corners.TopLeft, max);
        var tr = Math.Min(corners.TopRight, max);
        var br = Math.Min(corners.BottomRight, max);
        var bl = Math.Min(corners.BottomLeft, max);
        path.BeginFigure(new Point(rect.Left + tl, rect.Top), true);
        path.LineTo(new Point(rect.Right - tr, rect.Top));
        Arc(path, new Point(rect.Right, rect.Top + tr), tr);
        path.LineTo(new Point(rect.Right, rect.Bottom - br));
        Arc(path, new Point(rect.Right - br, rect.Bottom), br);
        path.LineTo(new Point(rect.Left + bl, rect.Bottom));
        Arc(path, new Point(rect.Left, rect.Bottom - bl), bl);
        path.LineTo(new Point(rect.Left, rect.Top + tl));
        Arc(path, new Point(rect.Left + tl, rect.Top), tl);
        path.EndFigure(true);
        return geometry;
    }

    private static void Arc(StreamGeometryContext path, Point end, double radius)
    {
        if (radius <= 0)
            path.LineTo(end);
        else
            path.ArcTo(end, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
    }
}