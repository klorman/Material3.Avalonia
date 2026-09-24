using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal static class MenuOpenState
{
    private const string ClassName = "m3-menu-open";
    private static readonly ConditionalWeakTable<Control, State> States = new();

    internal static IDisposable? Acquire(Control? owner)
    {
        if (owner is null) return null;

        var state = States.GetValue(owner, static _ => new State());
        state.Count++;
        if (state.Count == 1 && !owner.Classes.Contains(ClassName))
        {
            owner.Classes.Add(ClassName);
            state.OwnsClass = true;
        }

        return new Lease(owner, state);
    }

    private sealed class State
    {
        internal int Count;
        internal bool OwnsClass;
    }

    private sealed class Lease(Control owner, State state) : IDisposable
    {
        private Control? _owner = owner;
        private State? _state = state;

        public void Dispose()
        {
            if (_owner is not { } owner || _state is not { } state) return;
            _owner = null;
            _state = null;
            if (--state.Count != 0) return;
            if (state.OwnsClass) owner.Classes.Remove(ClassName);
            state.OwnsClass = false;
        }
    }
}