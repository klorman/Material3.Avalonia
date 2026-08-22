using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Draws a rounded outline with an animatable top-edge notch.
/// </summary>
public class NotchedOutline : Decorator
{
    /// <summary>
    /// Defines the outline stroke brush.
    /// </summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<NotchedOutline, IBrush?>(nameof(Stroke));

    /// <summary>
    /// Defines the outline stroke thickness.
    /// </summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(StrokeThickness), 1d);

    /// <summary>
    /// Defines the outline corner radius.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<NotchedOutline, CornerRadius>(nameof(CornerRadius));

    /// <summary>
    /// Defines the minimum start-edge distance to the notch content.
    /// </summary>
    public static readonly StyledProperty<double> NotchStartProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(NotchStart));

    /// <summary>
    /// Defines the minimum end-edge distance to the notch content.
    /// </summary>
    public static readonly StyledProperty<double> NotchEndProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(NotchEnd));

    /// <summary>
    /// Defines the horizontal padding around the notch content.
    /// </summary>
    public static readonly StyledProperty<double> NotchPaddingProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(NotchPadding));

    /// <summary>
    /// Defines the bottom padding used to align the floating label.
    /// </summary>
    public static readonly StyledProperty<double> LabelBottomPaddingProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(LabelBottomPadding));

    /// <summary>
    /// Defines how open the notch is.
    /// </summary>
    public static readonly StyledProperty<double> NotchProgressProperty =
        AvaloniaProperty.Register<NotchedOutline, double>(nameof(NotchProgress));

    private readonly Pen _pen = new()
    {
        LineCap = PenLineCap.Flat,
        LineJoin = PenLineJoin.Round
    };

    private StreamGeometry? _bodyGeometry;
    private Size _geometrySize;
    private CornerRadius _geometryCornerRadius;
    private double _geometryStrokeThickness = double.NaN;
    private ResolvedRadii _resolvedRadii;
    private Rect _strokeRect;

    /// <summary>
    /// Gets or sets the outline stroke brush.
    /// </summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// Gets or sets the outline stroke thickness.
    /// </summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the outline corner radius.
    /// </summary>
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum start-edge distance to the notch content.
    /// </summary>
    public double NotchStart
    {
        get => GetValue(NotchStartProperty);
        set => SetValue(NotchStartProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum end-edge distance to the notch content.
    /// </summary>
    public double NotchEnd
    {
        get => GetValue(NotchEndProperty);
        set => SetValue(NotchEndProperty, value);
    }

    /// <summary>
    /// Gets or sets the horizontal padding around the notch content.
    /// </summary>
    public double NotchPadding
    {
        get => GetValue(NotchPaddingProperty);
        set => SetValue(NotchPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the bottom padding used to align the floating label.
    /// </summary>
    public double LabelBottomPadding
    {
        get => GetValue(LabelBottomPaddingProperty);
        set => SetValue(LabelBottomPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets how open the notch is.
    /// </summary>
    public double NotchProgress
    {
        get => GetValue(NotchProgressProperty);
        set => SetValue(NotchProgressProperty, value);
    }

    static NotchedOutline()
    {
        AffectsMeasure<NotchedOutline>(
            CornerRadiusProperty,
            NotchStartProperty,
            NotchEndProperty,
            NotchPaddingProperty);

        AffectsArrange<NotchedOutline>(
            CornerRadiusProperty,
            NotchStartProperty,
            NotchEndProperty,
            NotchPaddingProperty,
            LabelBottomPaddingProperty);

        AffectsRender<NotchedOutline>(
            StrokeProperty,
            StrokeThicknessProperty,
            CornerRadiusProperty,
            NotchStartProperty,
            NotchEndProperty,
            NotchPaddingProperty,
            NotchProgressProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotchedOutline" /> class.
    /// </summary>
    public NotchedOutline()
    {
        IsHitTestVisible = false;
        ClipToBounds = false;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is null)
            return default;

        var (start, end) = GetContentInsets();
        var availableWidth = availableSize.Width;
        var childWidth = double.IsPositiveInfinity(availableWidth)
            ? double.PositiveInfinity
            : Math.Max(0, availableWidth - start - end);

        Child.Measure(new Size(childWidth, double.PositiveInfinity));

        return new Size(
            Child.DesiredSize.Width + start + end,
            Child.DesiredSize.Height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is null)
            return finalSize;

        var (start, end) = GetContentInsets();
        var maxWidth = Math.Max(0, finalSize.Width - start - end);
        var width = Math.Min(Child.DesiredSize.Width, maxWidth);
        var height = Child.DesiredSize.Height;

        var labelBottomPadding = SanitizeNonNegative(LabelBottomPadding);

        // Mirrors Material's translateY(-100% + label-text-padding-bottom)
        // without baking the populated label line height into this primitive.
        Child.Arrange(new Rect(
            start,
            -height + labelBottomPadding,
            width,
            height));

        return finalSize;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var brush = Stroke;
        var thickness = SanitizeNonNegative(StrokeThickness);
        var width = Bounds.Width;
        var height = Bounds.Height;

        if (brush is null || thickness <= 0 || width <= thickness || height <= thickness)
            return;

        EnsureBodyGeometry(new Size(width, height), thickness);

        if (_bodyGeometry is null)
            return;

        _pen.Brush = brush;
        _pen.Thickness = thickness;

        context.DrawGeometry(null, _pen, _bodyGeometry);
        DrawTopEdge(context);
    }

    private void DrawTopEdge(DrawingContext context)
    {
        var left = _strokeRect.Left + _resolvedRadii.TopLeft;
        var right = _strokeRect.Right - _resolvedRadii.TopRight;

        if (right <= left)
            return;

        var progress = Math.Clamp(SanitizeFinite(NotchProgress), 0d, 1d);
        var child = Child;

        if (child is null || child.Bounds.Width <= 0 || progress <= 0)
        {
            context.DrawLine(_pen, new Point(left, _strokeRect.Top), new Point(right, _strokeRect.Top));
            return;
        }

        var padding = SanitizeNonNegative(NotchPadding);
        var fullGapStart = child.Bounds.Left - padding;
        var fullGapEnd = child.Bounds.Right + padding;

        fullGapStart = Math.Clamp(fullGapStart, left, right);
        fullGapEnd = Math.Clamp(fullGapEnd, left, right);

        if (fullGapEnd <= fullGapStart)
        {
            context.DrawLine(_pen, new Point(left, _strokeRect.Top), new Point(right, _strokeRect.Top));
            return;
        }

        // Material Web opens the two top panels from the center of the label.
        var center = (fullGapStart + fullGapEnd) / 2d;
        var halfGap = (fullGapEnd - fullGapStart) * progress / 2d;
        var gapStart = center - halfGap;
        var gapEnd = center + halfGap;

        if (gapStart > left)
            context.DrawLine(_pen, new Point(left, _strokeRect.Top), new Point(gapStart, _strokeRect.Top));

        if (gapEnd < right)
            context.DrawLine(_pen, new Point(gapEnd, _strokeRect.Top), new Point(right, _strokeRect.Top));
    }

    private void EnsureBodyGeometry(Size size, double thickness)
    {
        if (_bodyGeometry is not null &&
            _geometrySize == size &&
            _geometryCornerRadius == CornerRadius &&
            _geometryStrokeThickness.Equals(thickness))
        {
            return;
        }

        _geometrySize = size;
        _geometryCornerRadius = CornerRadius;
        _geometryStrokeThickness = thickness;

        var half = thickness / 2d;
        _strokeRect = new Rect(
            half,
            half,
            Math.Max(0, size.Width - thickness),
            Math.Max(0, size.Height - thickness));

        _resolvedRadii = ResolveRadii(_strokeRect.Size, CornerRadius, half);
        _bodyGeometry = BuildBodyGeometry(_strokeRect, _resolvedRadii);
    }

    private static StreamGeometry BuildBodyGeometry(Rect rect, ResolvedRadii radii)
    {
        var geometry = new StreamGeometry();

        using var path = geometry.Open();

        path.BeginFigure(new Point(rect.Right - radii.TopRight, rect.Top), false);

        AddCorner(
            path,
            radii.TopRight,
            new Point(rect.Right, rect.Top + radii.TopRight));

        path.LineTo(new Point(rect.Right, rect.Bottom - radii.BottomRight));

        AddCorner(
            path,
            radii.BottomRight,
            new Point(rect.Right - radii.BottomRight, rect.Bottom));

        path.LineTo(new Point(rect.Left + radii.BottomLeft, rect.Bottom));

        AddCorner(
            path,
            radii.BottomLeft,
            new Point(rect.Left, rect.Bottom - radii.BottomLeft));

        path.LineTo(new Point(rect.Left, rect.Top + radii.TopLeft));

        AddCorner(
            path,
            radii.TopLeft,
            new Point(rect.Left + radii.TopLeft, rect.Top));

        path.EndFigure(false);

        return geometry;
    }

    private static void AddCorner(StreamGeometryContext path, double radius, Point endPoint)
    {
        if (radius > 0)
        {
            path.ArcTo(
                endPoint,
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: false,
                SweepDirection.Clockwise);
        }
        else
        {
            path.LineTo(endPoint);
        }
    }

    private (double Start, double End) GetContentInsets()
    {
        var padding = SanitizeNonNegative(NotchPadding);
        var startCorner = Math.Max(
            SanitizeNonNegative(CornerRadius.TopLeft),
            SanitizeNonNegative(CornerRadius.BottomLeft));
        var endCorner = Math.Max(
            SanitizeNonNegative(CornerRadius.TopRight),
            SanitizeNonNegative(CornerRadius.BottomRight));

        // Mirrors Material Web's:
        // start-space = max(leading-space, shape-start + outline-label-padding)
        // end-space   = max(trailing-space, shape-end)
        var start = Math.Max(SanitizeNonNegative(NotchStart), startCorner + padding);
        var end = Math.Max(SanitizeNonNegative(NotchEnd), endCorner);

        return (start, end);
    }

    private static ResolvedRadii ResolveRadii(Size size, CornerRadius cornerRadius, double halfStroke)
    {
        var topLeft = Math.Max(0, SanitizeNonNegative(cornerRadius.TopLeft) - halfStroke);
        var topRight = Math.Max(0, SanitizeNonNegative(cornerRadius.TopRight) - halfStroke);
        var bottomRight = Math.Max(0, SanitizeNonNegative(cornerRadius.BottomRight) - halfStroke);
        var bottomLeft = Math.Max(0, SanitizeNonNegative(cornerRadius.BottomLeft) - halfStroke);

        var scale = 1d;
        scale = Math.Min(scale, GetRadiusScale(size.Width, topLeft + topRight));
        scale = Math.Min(scale, GetRadiusScale(size.Width, bottomLeft + bottomRight));
        scale = Math.Min(scale, GetRadiusScale(size.Height, topLeft + bottomLeft));
        scale = Math.Min(scale, GetRadiusScale(size.Height, topRight + bottomRight));

        return new ResolvedRadii(
            topLeft * scale,
            topRight * scale,
            bottomRight * scale,
            bottomLeft * scale);
    }

    private static double GetRadiusScale(double available, double required)
    {
        if (required <= 0)
            return 1d;

        return Math.Min(1d, Math.Max(0d, available) / required);
    }

    private static double SanitizeNonNegative(double value) =>
        double.IsFinite(value) ? Math.Max(0d, value) : 0d;

    private static double SanitizeFinite(double value) =>
        double.IsFinite(value) ? value : 0d;

    private readonly record struct ResolvedRadii(
        double TopLeft,
        double TopRight,
        double BottomRight,
        double BottomLeft);
}
