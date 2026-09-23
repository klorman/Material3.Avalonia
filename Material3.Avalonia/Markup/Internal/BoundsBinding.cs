using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Material3.Avalonia.Markup.Internal;

internal static class BoundsBinding
{
    private static readonly CompiledBindingPath Path = new CompiledBindingPathBuilder()
        .Self()
        .Property(Visual.BoundsProperty, CreateAccessor)
        .Build();

    public static BindingBase Create() => new CompiledBinding(Path) { Mode = BindingMode.OneWay };

    private static IPropertyAccessor CreateAccessor(WeakReference<object?> target, IPropertyInfo property) =>
        new BoundsAccessor(target, (AvaloniaProperty)property);

    private sealed class BoundsAccessor : IPropertyAccessor, IObserver<object?>
    {
        private readonly WeakReference<object?> _target;
        private readonly AvaloniaProperty _property;
        private IDisposable? _subscription;
        private Action<object?>? _listener;

        public BoundsAccessor(WeakReference<object?> target, AvaloniaProperty property)
        {
            _target = target;
            _property = property;
        }

        public Type? PropertyType => _property.PropertyType;

        public object? Value => Target?.GetValue(_property);

        private AvaloniaObject? Target =>
            _target.TryGetTarget(out var target) ? target as AvaloniaObject : null;

        public bool SetValue(object? value, BindingPriority priority) => false;

        public void Subscribe(Action<object?> listener)
        {
            _listener = listener;
            if (Target is { } target)
                _subscription = target.GetObservable(_property).Subscribe(this);
            else
                listener(Value);
        }

        public void Unsubscribe()
        {
            _subscription?.Dispose();
            _subscription = null;
            _listener = null;
        }

        public void Dispose() => Unsubscribe();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value) => _listener?.Invoke(value);
    }
}