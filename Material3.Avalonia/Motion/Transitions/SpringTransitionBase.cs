using Avalonia.Animation;
using Avalonia.Threading;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Motion.Transitions;

public abstract class SpringTransitionBase<T, TAdapter> : InterpolatingTransitionBase<T>
    where TAdapter : ISpringValueAdapter<T>, new()
{
    private readonly TAdapter _adapter = new();

    private double? _stiffness;

    public double? Stiffness
    {
        get => _stiffness;
        set
        {
            _stiffness = value;
            MarkDurationDirty();
        }
    }

    private double? _damping;

    public double? Damping
    {
        get => _damping;
        set
        {
            _damping = value;
            MarkDurationDirty();
        }
    }

    public double InitialVelocity { get; set; }

    private MotionStyle _style = MotionStyle.Spatial;

    public MotionStyle Style
    {
        get => _style;
        set
        {
            _style = value;
            MarkDurationDirty();
        }
    }

    private MotionSpeed _speed = MotionSpeed.Default;

    public MotionSpeed Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            MarkDurationDirty();
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("MD3 spring transitions compute Duration automatically; do not set.", true)]
    public new TimeSpan? Duration
    {
        get => base.Duration;
        set => throw new InvalidOperationException(
            "MD3 spring transitions compute Duration automatically; setting Duration is not supported.");
    }

    public bool RespectReduceMotion { get; set; } = true;

    private bool _durationDirty = true;
    private bool _recomputePosted;
    private double _cachedK, _cachedZ, _cachedAutoSeconds;
    private MotionStyle _cachedStyle;

    protected SpringTransitionBase()
    {
        MotionSettings.GlobalSchemeChanged += (_, _) => MarkDurationDirty();
        PostRecomputeIfNeeded();
    }

    private void MarkDurationDirty()
    {
        _durationDirty = true;
        PostRecomputeIfNeeded();
    }

    private void PostRecomputeIfNeeded()
    {
        if (_recomputePosted) return;
        _recomputePosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _recomputePosted = false;
            if (_durationDirty) RecomputeAutoDuration();
        }, DispatcherPriority.Render);
    }

    private void RecomputeAutoDuration()
    {
        var scheme = MotionSettings.GlobalScheme;
        var token = scheme.Resolve(Style, Speed);
        var k = Stiffness ?? token.Stiffness;
        var z = Damping ?? token.Damping;

        _cachedAutoSeconds = SpringDuration.ComputeSeconds(k, z, effects: Style == MotionStyle.Effects);
        _cachedK = k;
        _cachedZ = z;
        _cachedStyle = Style;

        base.Duration = TimeSpan.FromSeconds(_cachedAutoSeconds);
        _durationDirty = false;
    }

    protected override T Interpolate(double progress, T from, T to)
    {
        if (RespectReduceMotion && MotionSettings.ReduceMotion)
            return Style == MotionStyle.Spatial ? to : LerpLinear(progress, from, to);

        if (_durationDirty)
            RecomputeAutoDuration();

        var k = _cachedK;
        var z = _cachedZ;
        var t = progress * _cachedAutoSeconds;

        Span<double> a = stackalloc double[_adapter.Components];
        Span<double> b = stackalloc double[_adapter.Components];
        _adapter.Read(from, a);
        _adapter.Read(to, b);

        var v0 = NormalizeVelocity(InitialVelocity, a, b);
        var y = SpringAnalytic.Evaluate(t, k, z, v0);

        if (Style == MotionStyle.Effects)
            y = Math.Clamp(y, 0, 1);

        for (var i = 0; i < _adapter.Components; ++i)
            a[i] = a[i] + (b[i] - a[i]) * y;

        return _adapter.Make(a);
    }

    private static double NormalizeVelocity(double v0, ReadOnlySpan<double> from, ReadOnlySpan<double> to)
    {
        if (Math.Abs(v0) <= double.Epsilon) return 0;
        double maxDelta = 0;
        for (var i = 0; i < from.Length; ++i)
            maxDelta = Math.Max(maxDelta, Math.Abs(to[i] - from[i]));
        if (maxDelta <= 1e-6) return 0;
        return v0 / maxDelta;
    }

    private T LerpLinear(double p, T from, T to)
    {
        Span<double> a = stackalloc double[_adapter.Components];
        Span<double> b = stackalloc double[_adapter.Components];
        _adapter.Read(from, a);
        _adapter.Read(to, b);
        for (var i = 0; i < _adapter.Components; ++i)
            a[i] = a[i] + (b[i] - a[i]) * p;
        return _adapter.Make(a);
    }
}