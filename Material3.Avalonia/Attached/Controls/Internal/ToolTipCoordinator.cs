using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class ToolTipCoordinator
{
    private static readonly ConditionalWeakTable<TopLevel, ToolTipCoordinator> Coordinators = new();
    private ToolTipOwner? _active;
    private long? _lastClosed;

    public static ToolTipCoordinator Get(TopLevel topLevel) =>
        Coordinators.GetValue(topLevel, _ => new ToolTipCoordinator());

    public bool Activate(ToolTipOwner owner)
    {
        if (_active != owner && _active is { } previous && !previous.Close())
            return false;

        _active = owner;
        return true;
    }

    public bool ShouldShowImmediately(int betweenShowDelay) =>
        betweenShowDelay >= 0 && (_active is not null ||
                                  _lastClosed is { } time && Stopwatch.GetElapsedTime(time).TotalMilliseconds <=
                                  betweenShowDelay);

    public void Closed(ToolTipOwner owner)
    {
        if (_active != owner)
            return;

        _active = null;
        _lastClosed = Stopwatch.GetTimestamp();
    }
}