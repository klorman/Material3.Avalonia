using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class PopupGeometry
{
    private sealed class DirectionState
    {
        public PlacementMode Direction;
    }

    private static readonly ConditionalWeakTable<Popup, DirectionState> Directions = new();

    internal static void RememberDirection(Popup popup, PlacementMode direction) =>
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

    internal static PlacementMode FromGravity(PopupGravity gravity) =>
        gravity.HasFlag(PopupGravity.Top) ? PlacementMode.Top :
        gravity.HasFlag(PopupGravity.Bottom) ? PlacementMode.Bottom :
        gravity.HasFlag(PopupGravity.Left) ? PlacementMode.Left :
        gravity.HasFlag(PopupGravity.Right) ? PlacementMode.Right : PlacementMode.Bottom;

    internal static Rect AvailableBounds(TopLevel root, Rect anchor, bool overlay)
    {
        if (!overlay && HasScreenCoordinates(root) &&
            root.Screens?.ScreenFromPoint(root.PointToScreen(anchor.Center)) is { } screen)
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

    private static readonly ConditionalWeakTable<PopupRoot, Estimate> Estimates = new();

    private sealed record Estimate(TopLevel Root, Point Origin);

    internal static bool HasScreenCoordinates(TopLevel root) => root.TryGetPlatformHandle() is not null;

    internal static void Forget(Popup popup)
    {
        Directions.Remove(popup);
        if (TopLevel.GetTopLevel(popup.Child!) is PopupRoot root) Estimates.Remove(root);
    }

    internal static PlacementMode Resolve(Control surface, Rect visible, Control target, Popup popup,
        Rect? anchorOverride = null)
    {
        var host = TopLevel.GetTopLevel(surface);
        var direction = GetRequestedDirection(popup);
        if (host is not PopupRoot || HasScreenCoordinates(host))
        {
            var origin = surface.PointToScreen(visible.TopLeft);
            var far = surface.PointToScreen(visible.BottomRight);
            var anchorRect = anchorOverride ?? popup.PlacementRect ?? new Rect(target.Bounds.Size);
            var anchor = target.PointToScreen(anchorRect.TopLeft);
            var end = target.PointToScreen(anchorRect.BottomRight);
            if (far.Y <= anchor.Y) return PlacementMode.Top;
            if (origin.Y >= end.Y) return PlacementMode.Bottom;
            if (far.X <= anchor.X) return PlacementMode.Left;
            if (origin.X >= end.X) return PlacementMode.Right;
            return (origin.Y + far.Y) < (anchor.Y + end.Y) ? PlacementMode.Top : PlacementMode.Bottom;
        }

        var parent = TopLevel.GetTopLevel(target);
        if (parent is null) return direction;
        var anchorBounds = anchorOverride ?? popup.PlacementRect ?? new Rect(target.Bounds.Size);
        var local = target.TranslatePoint(anchorBounds.TopLeft, parent) ?? default;
        var root = parent;
        if (parent is PopupRoot parentPopup && Estimates.TryGetValue(parentPopup, out var estimate))
        {
            root = estimate.Root;
            local += estimate.Origin;
        }

        // Wayland exposes no global origin: the window is an estimate, not the compositor's work area.
        if (popup.CustomPopupPlacementCallback is null &&
            direction is PlacementMode.Top or PlacementMode.Bottom &&
            popup.PlacementConstraintAdjustment.HasFlag(PopupPositionerConstraintAdjustment.FlipY))
        {
            var below = root.ClientSize.Height - local.Y - anchorBounds.Height;
            var above = local.Y;
            if (direction == PlacementMode.Bottom && visible.Height > below && above > below)
                direction = PlacementMode.Top;
            else if (direction == PlacementMode.Top && visible.Height > above && below > above)
                direction = PlacementMode.Bottom;
        }

        var position = direction switch
        {
            PlacementMode.Top => new Point(local.X, local.Y - visible.Height),
            PlacementMode.Left => new Point(local.X - visible.Width, local.Y),
            PlacementMode.Right => new Point(local.X + anchorBounds.Width, local.Y),
            _ => new Point(local.X, local.Y + anchorBounds.Height)
        };
        Estimates.Remove((PopupRoot)host);
        Estimates.Add((PopupRoot)host, new Estimate(root, position - visible.TopLeft));
        return direction;
    }
}