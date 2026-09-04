using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Material3.Avalonia.Tokens.Internal;

internal static class TokenTargetBinding
{
    private static readonly CompiledBindingPath ResolutionTargetPath = new CompiledBindingPathBuilder()
        .Self()
        .Property(StyledElement.TemplatedParentProperty, CreateResolutionTargetAccessor)
        .Build();

    public static BindingBase CreateResolutionTargetBinding()
    {
        return new CompiledBinding(ResolutionTargetPath)
        {
            Mode = BindingMode.OneWay
        };
    }

    private static IPropertyAccessor CreateResolutionTargetAccessor(
        WeakReference<object?> target,
        IPropertyInfo property)
    {
        return new ResolutionTargetAccessor(target, (AvaloniaProperty)property);
    }

    private sealed class ResolutionTargetAccessor : IPropertyAccessor, IObserver<object?>
    {
        private readonly WeakReference<object?> _target;
        private readonly AvaloniaProperty _property;
        private IDisposable? _subscription;
        private Action<object?>? _listener;

        public ResolutionTargetAccessor(WeakReference<object?> target, AvaloniaProperty property)
        {
            _target = target;
            _property = property;
        }

        public Type? PropertyType => typeof(object);

        public object? Value => Target;

        private object? Target =>
            _target.TryGetTarget(out var target) ? target : null;

        public bool SetValue(object? value, BindingPriority priority)
        {
            return false;
        }

        public void Subscribe(Action<object?> listener)
        {
            _listener = listener;
            if (Target is AvaloniaObject target)
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
            _listener?.Invoke(Value);
        }
    }
}
