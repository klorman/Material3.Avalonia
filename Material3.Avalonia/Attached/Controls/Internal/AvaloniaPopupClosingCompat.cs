using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Primitives;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class AvaloniaPopupClosingCompat
{
    internal static bool TrySubscribe(Popup popup, EventHandler<CancelEventArgs> handler)
    {
        try
        {
            AddClosing(popup, handler);
            return true;
        }
        catch (Exception error) when (IsUnavailable(error))
        {
            return false;
        }
    }

    internal static void TryUnsubscribe(Popup popup, EventHandler<CancelEventArgs> handler)
    {
        try
        {
            RemoveClosing(popup, handler);
        }
        catch (Exception error) when (IsUnavailable(error))
        {
        }
    }

    private static bool IsUnavailable(Exception error) =>
        error is MissingMethodException or TypeLoadException or MemberAccessException;

    // Avalonia 12.1 keeps Popup.Closing internal. Losing this hook only disables the top-level menu exit animation.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "add_Closing")]
    private static extern void AddClosing(Popup popup, EventHandler<CancelEventArgs> handler);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "remove_Closing")]
    private static extern void RemoveClosing(Popup popup, EventHandler<CancelEventArgs> handler);
}
