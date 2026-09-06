using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material3.Avalonia.Attached.Controls.Internal;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class ToolTipReveal : Decorator
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ToolTipReveal, double>(nameof(Progress));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ToolTipReveal>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<ToolTipReveal>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<ToolTipReveal>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<ToolTipReveal>();

    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        Border.BoxShadowProperty.AddOwner<ToolTipReveal>();

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
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

    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    static ToolTipReveal()
    {
        AffectsMeasure<ToolTipReveal>(BorderThicknessProperty);
        AffectsRender<ToolTipReveal>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty,
            CornerRadiusProperty, BoxShadowProperty);
    }

    private PlacementMode _direction = PlacementMode.Bottom;

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public ToolTipReveal()
    {
        Transitions =
        [
            new SpringDoubleTransition
                { Property = ProgressProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast }
        ];
    }

    public void Reset()
    {
        var transitions = Transitions;
        Transitions = null;
        SetValue(ProgressProperty, 0d);
        Transitions = transitions;
    }

    public void SetPresented(bool presented)
    {
        if (MotionSettings.ReduceMotion)
        {
            var transitions = Transitions;
            Transitions = null;
            SetValue(ProgressProperty, presented ? 1d : 0d);
            Transitions = transitions;
        }
        else
            SetValue(ProgressProperty, presented ? 1d : 0d);
    }

    public void UpdateDirection(Control? target, Popup popup)
    {
        if (target is null)
            return;
        // Wayland 12 exposes neither global window coordinates nor the compositor's final popup position.
        if (TopLevel.GetTopLevel(this) is PopupRoot native && native.TryGetPlatformHandle() is null)
        {
            _direction = ToolTipPlacement.GetRequestedDirection(popup);
            UpdateClip();
            InvalidateVisual();
            return;
        }

        var origin = this.PointToScreen(default);
        var anchor = target.PointToScreen(default);
        var scaling = TopLevel.GetTopLevel(target)?.RenderScaling ?? 1;
        var relative = new Rect((origin.X - anchor.X) / scaling, (origin.Y - anchor.Y) / scaling,
            Bounds.Width, Bounds.Height);
        _direction = relative.Top >= target.Bounds.Height ? PlacementMode.Bottom :
            relative.Bottom <= 0 ? PlacementMode.Top :
            relative.Left >= target.Bounds.Width ? PlacementMode.Right :
            relative.Right <= 0 ? PlacementMode.Left : PlacementMode.Bottom;
        UpdateClip();
        InvalidateVisual();
    }

    private Rect VisibleRect
    {
        get
        {
            var progress = Math.Clamp(Progress, 0, 1);
            return _direction switch
            {
                PlacementMode.Top => new Rect(0, Bounds.Height * (1 - progress), Bounds.Width,
                    Bounds.Height * progress),
                PlacementMode.Left => new Rect(Bounds.Width * (1 - progress), 0, Bounds.Width * progress,
                    Bounds.Height),
                PlacementMode.Right => new Rect(0, 0, Bounds.Width * progress, Bounds.Height),
                _ => new Rect(0, 0, Bounds.Width, Bounds.Height * progress)
            };
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ProgressProperty || change.Property == CornerRadiusProperty ||
            change.Property == BoundsProperty)
        {
            UpdateClip();
            InvalidateVisual();
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
        var result = finalSize;
        UpdateClip();
        return result;
    }

    private void UpdateClip()
    {
        if (Child is null)
            return;
        var rect = VisibleRect.Translate(-Child.Bounds.Position);
        Child.Clip = Geometry(rect, CornerRadius);
    }

    public override void Render(DrawingContext context)
    {
        if (Progress <= 0)
            return;
        var rect = VisibleRect;
        context.DrawRectangle(Background, null, new RoundedRect(rect, CornerRadius), BoxShadow);
        if (BorderBrush is not null && BorderThickness != default)
        {
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
            {
                path.SetFillRule(FillRule.EvenOdd);
                Append(path, rect, CornerRadius);
                var inner = rect.Deflate(BorderThickness);
                if (inner.Width > 0 && inner.Height > 0)
                    Append(path, inner, new CornerRadius(
                        Math.Max(0, CornerRadius.TopLeft - Math.Max(BorderThickness.Top, BorderThickness.Left)),
                        Math.Max(0, CornerRadius.TopRight - Math.Max(BorderThickness.Top, BorderThickness.Right)),
                        Math.Max(0, CornerRadius.BottomRight - Math.Max(BorderThickness.Bottom, BorderThickness.Right)),
                        Math.Max(0, CornerRadius.BottomLeft - Math.Max(BorderThickness.Bottom, BorderThickness.Left))));
            }

            context.DrawGeometry(BorderBrush, null, geometry);
        }
    }

    private static StreamGeometry Geometry(Rect rect, CornerRadius corners)
    {
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
            Append(path, rect, corners);
        return geometry;
    }

    private static void Append(StreamGeometryContext path, Rect rect, CornerRadius corners)
    {
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
    }

    private static void Arc(StreamGeometryContext path, Point end, double radius)
    {
        if (radius <= 0)
            path.LineTo(end);
        else
            path.ArcTo(end, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
    }
}