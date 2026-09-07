using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class BadgedLayout : Panel
{
    public static readonly StyledProperty<Badge?> BadgeProperty =
        AvaloniaProperty.Register<BadgedLayout, Badge?>(nameof(Badge));

    public static readonly StyledProperty<Vector> SmallOverlapProperty =
        AvaloniaProperty.Register<BadgedLayout, Vector>(nameof(SmallOverlap));

    public static readonly StyledProperty<Vector> LargeOverlapProperty =
        AvaloniaProperty.Register<BadgedLayout, Vector>(nameof(LargeOverlap));

    public Badge? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public Vector SmallOverlap
    {
        get => GetValue(SmallOverlapProperty);
        set => SetValue(SmallOverlapProperty, value);
    }

    public Vector LargeOverlap
    {
        get => GetValue(LargeOverlapProperty);
        set => SetValue(LargeOverlapProperty, value);
    }

    static BadgedLayout()
    {
        AffectsMeasure<BadgedLayout>(BadgeProperty);
        AffectsArrange<BadgedLayout>(SmallOverlapProperty, LargeOverlapProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2)
            return default;
        Children[0].Measure(availableSize);
        Children[1].Measure(Size.Infinity);
        return Children[0].DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2)
            return finalSize;
        Children[0].Arrange(new Rect(finalSize));
        var anchor = Children[0] is ContentPresenter { Child: { } child }
            ? child.Bounds.Translate(Children[0].Bounds.Position)
            : Children[0].Bounds;
        var size = Children[1].DesiredSize;
        var overlap = Badge?.IsLarge == true ? LargeOverlap : SmallOverlap;
        // Avalonia mirrors this layout at the FlowDirection boundary, including the overflow.
        Children[1].Arrange(new Rect(new Point(anchor.Right - overlap.X, anchor.Top + overlap.Y - size.Height), size));
        return finalSize;
    }
}