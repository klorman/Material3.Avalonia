using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Material3.Avalonia.Attached;

namespace Material3.Avalonia.Density.Internal;

internal static class DensityBinding
{
    private static readonly CompiledBindingPath DensityPath = new CompiledBindingPathBuilder()
        .Self()
        .Property(DensityAssist.DensityProperty, CreateAvaloniaPropertyAccessor)
        .Build();

    public static BindingBase CreateDensityBinding()
    {
        return new CompiledBinding(DensityPath)
        {
            Mode = BindingMode.OneWay
        };
    }

    private static IPropertyAccessor CreateAvaloniaPropertyAccessor(
        WeakReference<object?> target,
        IPropertyInfo property)
    {
        return new AvaloniaObjectPropertyAccessor(target, (AvaloniaProperty)property);
    }

    private sealed class AvaloniaObjectPropertyAccessor : IPropertyAccessor, IObserver<object?>
    {
        private readonly WeakReference<object?> _target;
        private readonly AvaloniaProperty _property;
        private IDisposable? _subscription;
        private Action<object?>? _listener;

        public AvaloniaObjectPropertyAccessor(WeakReference<object?> target, AvaloniaProperty property)
        {
            _target = target;
            _property = property;
        }

        public Type? PropertyType => _property.PropertyType;

        public object? Value => Target?.GetValue(_property);

        private AvaloniaObject? Target =>
            _target.TryGetTarget(out var target) ? target as AvaloniaObject : null;

        public bool SetValue(object? value, BindingPriority priority)
        {
            if (_property.IsReadOnly || Target is not { } target) return false;

            target.SetValue(_property, value, priority);
            return true;
        }

        public void Subscribe(Action<object?> listener)
        {
            _listener = listener;
            if (Target is { } target)
            {
                _subscription = target.GetObservable(_property).Subscribe(this);
                return;
            }

            listener(Value);
        }

        public void Unsubscribe()
        {
            _subscription?.Dispose();
            _subscription = null;
            _listener = null;
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value)
        {
            _listener?.Invoke(value);
        }
    }
}
