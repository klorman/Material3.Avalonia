using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class MenuFocusRestore
{
    internal static void SuppressAutomaticScroll(Control previous)
    {
        if (TopLevel.GetTopLevel(previous) is not { } root) return;
        var values = new List<IDisposable>();
        previous.AddHandler(InputElement.GotFocusEvent, OnFocus, handledEventsToo: true);
        root.AddHandler(InputElement.GotFocusEvent, OnFocusBubbled, handledEventsToo: true);
        // Closing is synchronous. Restore overrides even when a later Closing handler cancels it.
        Dispatcher.UIThread.Post(Cleanup, DispatcherPriority.Send);

        void OnFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.NewFocusedElement != previous || e.NavigationMethod != NavigationMethod.Unspecified) return;
            previous.RemoveHandler(InputElement.GotFocusEvent, OnFocus);
            foreach (var scroll in previous.GetVisualAncestors().OfType<ScrollViewer>())
                if (scroll.SetValue(ScrollViewer.BringIntoViewOnFocusChangeProperty, false,
                        BindingPriority.Animation) is { } value)
                    values.Add(value);
        }

        void Cleanup()
        {
            previous.RemoveHandler(InputElement.GotFocusEvent, OnFocus);
            root.RemoveHandler(InputElement.GotFocusEvent, OnFocusBubbled);
            foreach (var value in values) value.Dispose();
            values.Clear();
        }

        void OnFocusBubbled(object? sender, FocusChangedEventArgs e)
        {
            if (e.NewFocusedElement == previous) Cleanup();
        }
    }
}