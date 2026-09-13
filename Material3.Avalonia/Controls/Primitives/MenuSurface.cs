using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Data;
using Avalonia.Controls.Presenters;
using Material3.Avalonia.Attached.Controls.Internal;
using Avalonia.VisualTree;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Attached.Controls;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class MenuSurface : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<MenuSurface>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<MenuSurface>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<MenuSurface>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<MenuSurface>();

    public static readonly StyledProperty<CornerRadius> InnerCornerRadiusProperty =
        AvaloniaProperty.Register<MenuSurface, CornerRadius>(nameof(InnerCornerRadius));

    public static readonly StyledProperty<CornerRadius> InactiveCornerRadiusProperty =
        AvaloniaProperty.Register<MenuSurface, CornerRadius>(nameof(InactiveCornerRadius));

    public static readonly StyledProperty<double> AnchorGapProperty =
        AvaloniaProperty.Register<MenuSurface, double>(nameof(AnchorGap));

    public static readonly StyledProperty<IBrush?> ShadowBrushProperty =
        AvaloniaProperty.Register<MenuSurface, IBrush?>(nameof(ShadowBrush));

    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        Border.BoxShadowProperty.AddOwner<MenuSurface>();

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<MenuSurface, double>(nameof(Progress), 1);

    public static readonly StyledProperty<double> ActiveProgressProperty =
        AvaloniaProperty.Register<MenuSurface, double>(nameof(ActiveProgress), 1);

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

    public CornerRadius InnerCornerRadius
    {
        get => GetValue(InnerCornerRadiusProperty);
        set => SetValue(InnerCornerRadiusProperty, value);
    }

    public CornerRadius InactiveCornerRadius
    {
        get => GetValue(InactiveCornerRadiusProperty);
        set => SetValue(InactiveCornerRadiusProperty, value);
    }

    public double AnchorGap
    {
        get => GetValue(AnchorGapProperty);
        set => SetValue(AnchorGapProperty, value);
    }

    public IBrush? ShadowBrush
    {
        get => GetValue(ShadowBrushProperty);
        set => SetValue(ShadowBrushProperty, value);
    }

    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double ActiveProgress
    {
        get => GetValue(ActiveProgressProperty);
        set => SetValue(ActiveProgressProperty, value);
    }

    internal MenuPopupPresentation? Presentation { get; set; }
    private PlacementMode _direction = PlacementMode.Bottom;

    internal PlacementMode Direction
    {
        get => _direction;
        set
        {
            if (_direction == value) return;
            _direction = value;
            GeometryChanged();
        }
    }

    private readonly List<(Rect Rect, CornerRadius Corners)> _surfaces = [];
    internal MenuItemsPanel? ItemsPanel { get; private set; }
    private double _reserve;
    private ScrollContentPresenter? _scrollContent;
    private IDisposable? _scrollClip;
    private IDisposable? _scrollOwnerClip;
    internal Thickness ContentPadding => Padding + new Thickness(0, _reserve, 0, _reserve);
    internal Rect SurfaceBounds => new Rect(Bounds.Size).Deflate(ContentPadding);
    internal double MotionDistance => ItemsPanel?.CollapsedDistance ?? 0;

    internal void SetItemsPanel(MenuItemsPanel? panel)
    {
        ItemsPanel = panel;
        UpdateMotion();
        InvalidateVisual();
    }

    internal void GeometryChanged()
    {
        UpdateMotion();
        InvalidateVisual();
    }

    private BoxShadows _shadows;


    static MenuSurface() => AffectsRender<MenuSurface>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty,
        CornerRadiusProperty, InnerCornerRadiusProperty, InactiveCornerRadiusProperty, ShadowBrushProperty,
        BoxShadowProperty, ProgressProperty, ActiveProgressProperty);

    public MenuSurface()
    {
        DetachedFromVisualTree += (_, _) =>
        {
            _scrollClip?.Dispose();
            _scrollClip = null;
            _scrollOwnerClip?.Dispose();
            _scrollOwnerClip = null;
            _scrollContent = null;
        };
        PointerEntered += (_, e) =>
        {
            if (e.Pointer.Type != PointerType.Touch) Presentation?.Activate();
        };
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = availableSize.Deflate(ContentPadding);
        if (ItemsPanel is { } panel) panel.AvailableHeight = size.Height;
        Child?.Measure(size);
        var damping = Math.Min(MotionScheme.Expressive.SpatialFast.Damping,
            MotionSettings.GlobalScheme.SpatialFast.Damping);
        var peak = damping >= 1 ? 0 : Math.Exp(-Math.PI * damping / Math.Sqrt(1 - damping * damping));
        var contentHeight = Math.Min(ItemsPanel?.DesiredSize.Height ?? Child?.DesiredSize.Height ?? 0,
            Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom));
        var reserve = MenuAssist.GetIsAnimationEnabled(this) && !MotionSettings.ReduceMotion
            ? Math.Ceiling(contentHeight * (1 - (ItemsPanel?.Compression ?? .8)) * peak)
            : 0;
        if (_reserve != reserve)
        {
            _reserve = reserve;
            size = availableSize.Deflate(ContentPadding);
            if (ItemsPanel is { } current) current.AvailableHeight = size.Height;
            Child?.Measure(size);
        }

        _scrollContent ??= Child?.GetVisualDescendants().OfType<ScrollContentPresenter>().FirstOrDefault();
        Presentation?.UpdateAnchor();
        return (Child?.DesiredSize ?? default).Inflate(ContentPadding);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoxShadowProperty || change.Property == ShadowBrushProperty)
        {
            _shadows = BoxShadow;
            if (ShadowBrush is ISolidColorBrush brush && BoxShadow.Count > 0)
            {
                var shadows = new BoxShadow[BoxShadow.Count];
                for (var i = 0; i < shadows.Length; i++)
                {
                    shadows[i] = BoxShadow[i];
                    shadows[i].Color = Color.FromArgb((byte)(shadows[i].Color.A * brush.Color.A / 255 * brush.Opacity),
                        brush.Color.R, brush.Color.G, brush.Color.B);
                }

                _shadows = new BoxShadows(shadows[0], shadows.Skip(1).ToArray());
            }
        }

        if (change.Property == ProgressProperty || change.Property == BoundsProperty ||
            change.Property == ActiveProgressProperty || change.Property == CornerRadiusProperty ||
            change.Property == InnerCornerRadiusProperty || change.Property == InactiveCornerRadiusProperty)
            UpdateMotion();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rect = new Rect(finalSize).Deflate(ContentPadding);
        Child?.Arrange(rect);
        UpdateMotion();
        return finalSize;
    }

    private void UpdateMotion()
    {
        ItemsPanel?.ApplyMotion(Progress, Direction == PlacementMode.Top);
        if (Progress != 1 && ItemsPanel is { HasOverflow: false } && _scrollContent is not null)
        {
            _scrollClip ??= _scrollContent.SetValue(ClipToBoundsProperty, false, BindingPriority.Animation);
            _scrollOwnerClip ??= Child?.SetValue(ClipToBoundsProperty, false, BindingPriority.Animation);
        }
        else
        {
            _scrollClip?.Dispose();
            _scrollClip = null;
            _scrollOwnerClip?.Dispose();
            _scrollOwnerClip = null;
        }

        UpdateGeometry();
    }

    private Rect AnimateRect(Rect rect, Point origin)
    {
        if (ItemsPanel is not { } panel) return rect;
        var top = origin.Y + panel.MapY(rect.Top - origin.Y, Progress, Direction == PlacementMode.Top);
        var bottom = origin.Y + panel.MapY(rect.Bottom - origin.Y, Progress, Direction == PlacementMode.Top);
        // Keep overshoot within the host's preallocated drawing area.
        top = Math.Max(Padding.Top, top);
        bottom = Math.Min(Bounds.Height - Padding.Bottom, bottom);
        return new Rect(rect.X, top, rect.Width, Math.Max(0, bottom - top));
    }

    private void UpdateGeometry()
    {
        var panel = ItemsPanel;
        var rects = panel?.Surfaces;
        var origin = panel?.TranslatePoint(default, this) ?? new Point(ContentPadding.Left, ContentPadding.Top);
        var viewport = SurfaceBounds;
        var inner = InnerCornerRadius;
        var p = ActiveProgress;
        var inactive = InactiveCornerRadius;
        var outer = new CornerRadius(
            Math.Max(0, inactive.TopLeft + (CornerRadius.TopLeft - inactive.TopLeft) * p),
            Math.Max(0, inactive.TopRight + (CornerRadius.TopRight - inactive.TopRight) * p),
            Math.Max(0, inactive.BottomRight + (CornerRadius.BottomRight - inactive.BottomRight) * p),
            Math.Max(0, inactive.BottomLeft + (CornerRadius.BottomLeft - inactive.BottomLeft) * p));
        _surfaces.Clear();
        if (panel is null || panel.HasOverflow || rects is null || rects.Count == 0)
            AddSurface(AnimateRect(viewport, origin), outer);
        else
            for (var i = 0; i < rects.Count; i++)
                AddSurface(AnimateRect(rects[i].Translate(origin), origin), new CornerRadius(
                    i == 0 ? outer.TopLeft : inner.TopLeft,
                    i == 0 ? outer.TopRight : inner.TopRight,
                    i == rects.Count - 1 ? outer.BottomRight : inner.BottomRight,
                    i == rects.Count - 1 ? outer.BottomLeft : inner.BottomLeft));
        if (Child is not { } child) return;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
            foreach (var (rect, corners) in _surfaces)
                AddOutline(context, rect.Translate(new Vector(-child.Bounds.X, -child.Bounds.Y)), corners);
        child.Clip = geometry;
        InvalidateVisual();
    }

    private void AddSurface(Rect rect, CornerRadius corners)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        var limit = Math.Min(rect.Width, rect.Height) / 2;
        _surfaces.Add((rect, new CornerRadius(Math.Min(corners.TopLeft, limit), Math.Min(corners.TopRight, limit),
            Math.Min(corners.BottomRight, limit), Math.Min(corners.BottomLeft, limit))));
    }

    private static void AddOutline(StreamGeometryContext context, Rect rect, CornerRadius corners)
    {
        var tl = corners.TopLeft;
        var tr = corners.TopRight;
        var br = corners.BottomRight;
        var bl = corners.BottomLeft;
        context.BeginFigure(new Point(rect.Left + tl, rect.Top), true);
        context.LineTo(new Point(rect.Right - tr, rect.Top));
        Arc(new Point(rect.Right, rect.Top + tr), tr);
        context.LineTo(new Point(rect.Right, rect.Bottom - br));
        Arc(new Point(rect.Right - br, rect.Bottom), br);
        context.LineTo(new Point(rect.Left + bl, rect.Bottom));
        Arc(new Point(rect.Left, rect.Bottom - bl), bl);
        context.LineTo(new Point(rect.Left, rect.Top + tl));
        Arc(new Point(rect.Left + tl, rect.Top), tl);
        context.EndFigure(true);

        void Arc(Point point, double radius)
        {
            if (radius == 0) context.LineTo(point);
            else context.ArcTo(point, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
        }
    }

    public override void Render(DrawingContext context)
    {
        using (context.PushClip(new Rect(Bounds.Size)))
        {
            foreach (var (rect, corners) in _surfaces)
                context.DrawRectangle(Brushes.Transparent, null, new RoundedRect(rect, corners), _shadows);
            foreach (var (rect, corners) in _surfaces)
                Draw(context, rect, corners);
        }
    }

    private void Draw(DrawingContext context, Rect rect, CornerRadius corner)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        var pen = BorderBrush is null || BorderThickness.Left <= 0 ? null : new Pen(BorderBrush, BorderThickness.Left);
        context.DrawRectangle(Background, pen, new RoundedRect(rect, corner));
    }
}