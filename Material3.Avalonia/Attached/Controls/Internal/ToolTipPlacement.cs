using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class ToolTipPlacement
{
    private sealed class DirectionState
    {
        public PlacementMode Direction;
    }

    private static readonly ConditionalWeakTable<Popup, DirectionState> Directions = new();

    private static void RememberDirection(Popup popup, PlacementMode direction) =>
        Directions.GetOrCreateValue(popup).Direction = direction;

    public static PlacementMode GetRequestedDirection(Popup popup)
    {
        if (popup.Placement == PlacementMode.Custom && Directions.TryGetValue(popup, out var state))
            return state.Direction;
        return popup.Placement switch
        {
            PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight => PlacementMode
                .Top,
            PlacementMode.Left or PlacementMode.LeftEdgeAlignedTop or PlacementMode.LeftEdgeAlignedBottom =>
                PlacementMode.Left,
            PlacementMode.Right or PlacementMode.RightEdgeAlignedTop or PlacementMode.RightEdgeAlignedBottom =>
                PlacementMode.Right,
            PlacementMode.AnchorAndGravity or PlacementMode.Custom => FromGravity(popup.PlacementGravity),
            _ => PlacementMode.Bottom
        };
    }

    private static PlacementMode FromGravity(PopupGravity gravity) =>
        gravity.HasFlag(PopupGravity.Top) ? PlacementMode.Top :
        gravity.HasFlag(PopupGravity.Bottom) ? PlacementMode.Bottom :
        gravity.HasFlag(PopupGravity.Left) ? PlacementMode.Left :
        gravity.HasFlag(PopupGravity.Right) ? PlacementMode.Right : PlacementMode.Bottom;

    public static IMultiValueConverter PlainOffset { get; } = new PlainOffsetConverter();

    private static T Token<T>(Control target, string key) => (T)TokenResolver.Resolve(
        key, AvaloniaProperty.UnsetValue, target, target.TemplatedParent, typeof(T))!;

    public static IDisposable ConfigurePlain(Popup popup, Control target) => new PlainPlacement(popup, target);

    private sealed class PlainPlacement : IDisposable
    {
        private readonly Popup _popup;
        private readonly Control _target;
        private readonly CustomPopupPlacementCallback? _callback;
        private bool _active;
        private bool _disposed;
        private bool _queued;

        public PlainPlacement(Popup popup, Control target)
        {
            _popup = popup;
            _target = target;
            _callback = popup.CustomPopupPlacementCallback;
            target.PropertyChanged += OnChanged;
            target.ResourcesChanged += OnResourcesChanged;
            Apply();
        }

        private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToolTip.PlacementProperty || e.Property == ToolTip.VerticalOffsetProperty ||
                e.Property == ToolTip.CustomPopupPlacementCallbackProperty)
                QueueApply();
        }

        private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e) => QueueApply();

        private void QueueApply()
        {
            if (_queued)
                return;
            _queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _queued = false;
                if (!_disposed)
                    Apply();
            }, DispatcherPriority.Render);
        }

        private void Restore()
        {
            if (!_active)
                return;
            _active = false;
            _popup.SetCurrentValue(Popup.PlacementProperty, ToolTip.GetPlacement(_target));
            _popup.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty,
                ToolTip.GetCustomPopupPlacementCallback(_target));
            _popup.SetCurrentValue(Popup.VerticalOffsetProperty, ToolTip.GetVerticalOffset(_target));
        }

        private void Apply()
        {
            Restore();
            var placement = ToolTip.GetPlacement(_target);
            if (placement is not (PlacementMode.Top or PlacementMode.Bottom) ||
                _callback is not null || ToolTip.GetCustomPopupPlacementCallback(_target) is not null)
                return;
            var offset = ToolTip.GetVerticalOffset(_target);
            if (placement == PlacementMode.Top && offset > 0 || placement == PlacementMode.Bottom && offset < 0)
                return;
            var gap = Math.Abs(offset);
            _active = true;
            // Avalonia clips PlacementRect to the target before the callback. Expand afterwards,
            // so the gap survives flipping without changing the application's attached values.
            _popup.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty, p =>
            {
                var direction = placement;
                if (p.ConstraintAdjustment.HasFlag(PopupPositionerConstraintAdjustment.FlipY) &&
                    TopLevel.GetTopLevel(_target) is { } root)
                {
                    var bounds = AvailableBounds(root, p.AnchorRectangle, UsesOverlay(_popup));
                    foreach (var candidate in VerticalCandidates(p.AnchorRectangle.Height, p.PopupSize.Height, gap,
                                 placement))
                    {
                        if (candidate.Y >= bounds.Top && candidate.Y + p.PopupSize.Height <= bounds.Bottom)
                        {
                            direction = candidate.Direction;
                            break;
                        }
                    }
                }

                p.AnchorRectangle = p.AnchorRectangle.Inflate(new Thickness(0, gap));
                p.Anchor = direction == PlacementMode.Top ? PopupAnchor.Top : PopupAnchor.Bottom;
                p.Gravity = direction == PlacementMode.Top ? PopupGravity.Top : PopupGravity.Bottom;
                p.Offset = p.Offset.WithY(0);
                RememberDirection(_popup, direction);
            });
            _popup.SetCurrentValue(Popup.PlacementProperty, PlacementMode.Custom);
            _popup.SetCurrentValue(Popup.VerticalOffsetProperty, 0d);
        }

        public void Dispose()
        {
            _disposed = true;
            _target.PropertyChanged -= OnChanged;
            _target.ResourcesChanged -= OnResourcesChanged;
            Restore();
        }
    }

    public static void PlaceRich(Flyout flyout, CustomPopupPlacement placement)
    {
        if (placement.Target is not Control target || TopLevel.GetTopLevel(target) is not { } root)
            return;
        if (flyout.IsSet(PopupFlyoutBase.PlacementAnchorProperty) ||
            flyout.IsSet(PopupFlyoutBase.PlacementGravityProperty))
        {
            placement.Anchor = flyout.PlacementAnchor;
            placement.Gravity = flyout.PlacementGravity;
            RememberDirection(flyout.Popup, FromGravity(placement.Gravity));
            placement.ConstraintAdjustment = flyout.PlacementConstraintAdjustment;
            return;
        }

        var shadow = Token<Thickness>(target, "MdImplRichToolTipShadowPadding");
        var gap = flyout.IsSet(PopupFlyoutBase.VerticalOffsetProperty)
            ? 0
            : Token<double>(target, "MdImplRichToolTipAnchorGap");
        var step = Math.Max(1, Token<double>(target, "MdImplRichToolTipPositionStep"));
        var width = Math.Max(0, placement.PopupSize.Width - shadow.Left - shadow.Right);
        var height = Math.Max(0, placement.PopupSize.Height - shadow.Top - shadow.Bottom);
        var bounds = AvailableBounds(root, placement.AnchorRectangle, UsesOverlay(flyout.Popup));
        var right = placement.AnchorRectangle.Width;
        var left = -width;
        var first = target.FlowDirection == FlowDirection.RightToLeft ? left : right;
        var second = target.FlowDirection == FlowDirection.RightToLeft ? right : left;
        var candidates = VerticalCandidates(placement.AnchorRectangle.Height, height, gap, PlacementMode.Bottom);
        Point? chosen = null;
        foreach (var candidate in flyout.IsSet(PopupFlyoutBase.PlacementConstraintAdjustmentProperty)
                     ? Array.Empty<(PlacementMode Direction, double Y)>()
                     : candidates)
        {
            var y = candidate.Y;
            foreach (var x in new[] { first, second })
            {
                var origin = new Point(x - shadow.Left + flyout.HorizontalOffset,
                    y - shadow.Top + flyout.VerticalOffset);
                if (bounds.Contains(new Rect(origin, placement.PopupSize)))
                {
                    chosen = origin;
                    break;
                }
            }

            if (chosen is not null)
                break;
            var initial = new Point(first - shadow.Left + flyout.HorizontalOffset,
                y - shadow.Top + flyout.VerticalOffset);
            var adjusted = initial.WithX(Shift(initial.X, placement.PopupSize.Width, bounds.Left, bounds.Right, step));
            if (bounds.Contains(new Rect(adjusted, placement.PopupSize)))
            {
                chosen = adjusted;
                break;
            }
        }

        placement.ConstraintAdjustment = flyout.PlacementConstraintAdjustment;
        if (chosen is { } point)
        {
            // Avalonia mirrors anchor and gravity after invoking custom placement.
            var rtl = target.FlowDirection == FlowDirection.RightToLeft;
            placement.Anchor = rtl ? PopupAnchor.TopRight : PopupAnchor.TopLeft;
            placement.Gravity = rtl ? PopupGravity.BottomLeft : PopupGravity.BottomRight;
            placement.Offset = point;
            RememberDirection(flyout.Popup, point.Y + shadow.Top < 0 ? PlacementMode.Top : PlacementMode.Bottom);
        }
        else
        {
            // Keep native flip/slide/resize available when no complete candidate fits.
            var rtl = target.FlowDirection == FlowDirection.RightToLeft;
            RememberDirection(flyout.Popup, PlacementMode.Bottom);
            placement.Anchor = PopupAnchor.BottomRight;
            placement.Gravity = PopupGravity.BottomRight;
            placement.Offset = new Point(flyout.HorizontalOffset + (rtl ? shadow.Right : -shadow.Left),
                flyout.VerticalOffset + gap - shadow.Top);
        }
    }

    private static double Shift(double value, double size, double minimum, double maximum, double step)
    {
        if (value < minimum)
            return value + Math.Ceiling((minimum - value) / step) * step;
        if (value + size > maximum)
            return value - Math.Ceiling((value + size - maximum) / step) * step;
        return value;
    }

    private static (PlacementMode Direction, double Y)[] VerticalCandidates(
        double anchorHeight, double popupHeight, double gap, PlacementMode preferred)
    {
        var top = (PlacementMode.Top, -popupHeight - gap);
        var bottom = (PlacementMode.Bottom, anchorHeight + gap);
        return preferred == PlacementMode.Top ? [top, bottom] : [bottom, top];
    }

    private static bool UsesOverlay(Popup popup) =>
        popup.Child?.GetVisualAncestors().Any(x => x is OverlayPopupHost) == true;

    private static Rect AvailableBounds(TopLevel root, Rect anchor, bool overlay)
    {
        if (!overlay && root.Screens?.ScreenFromPoint(root.PointToScreen(anchor.Center)) is { } screen)
        {
            var origin = root.PointToScreen(anchor.TopLeft);
            var area = screen.WorkingArea.Width > 0 && screen.WorkingArea.Height > 0
                ? screen.WorkingArea
                : screen.Bounds;
            return new Rect((area.X - origin.X) / root.RenderScaling, (area.Y - origin.Y) / root.RenderScaling,
                area.Width / root.RenderScaling, area.Height / root.RenderScaling);
        }

        var position = anchor.Position;
        return new Rect(-position.X, -position.Y, root.ClientSize.Width, root.ClientSize.Height);
    }

    private sealed class PlainOffsetConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3 || values[1] is not Control target || values[2] is not PlacementMode placement)
                return global::Avalonia.Data.BindingOperations.DoNothing;
            var resolved = TokenResolverConverter.Numeric.Convert(new object?[] { values[0], target },
                typeof(double), parameter ?? "MdImplPlainToolTipAnchorGap", culture);
            if (resolved is not double gap)
                return resolved;
            return placement switch
            {
                PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight => -gap,
                PlacementMode.Bottom or PlacementMode.BottomEdgeAlignedLeft
                    or PlacementMode.BottomEdgeAlignedRight => gap,
                _ => 0d
            };
        }
    }
}