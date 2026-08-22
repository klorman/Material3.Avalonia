using Avalonia.Animation;
using Avalonia.Threading;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Motion.Transitions;

/// <summary>
/// Spring transition with separate increase and decrease motion tokens.
/// </summary>
public sealed class SpringDirectionalDoubleTransition : InterpolatingTransitionBase<double>
{
    private MotionStyle _increaseStyle = MotionStyle.Effects;
    private MotionSpeed _increaseSpeed = MotionSpeed.Slow;
    private MotionStyle _decreaseStyle = MotionStyle.Effects;
    private MotionSpeed _decreaseSpeed = MotionSpeed.Fast;

    private bool _durationDirty = true;
    private bool _recomputePosted;
    private CachedSpring _increase;
    private CachedSpring _decrease;
    private double _timelineSeconds;

    /// <summary>
    /// Gets or sets the motion style used when the value increases.
    /// </summary>
    public MotionStyle IncreaseStyle
    {
        get => _increaseStyle;
        set { _increaseStyle = value; MarkDurationDirty(); }
    }

    /// <summary>
    /// Gets or sets the motion speed used when the value increases.
    /// </summary>
    public MotionSpeed IncreaseSpeed
    {
        get => _increaseSpeed;
        set { _increaseSpeed = value; MarkDurationDirty(); }
    }

    /// <summary>
    /// Gets or sets the motion style used when the value decreases.
    /// </summary>
    public MotionStyle DecreaseStyle
    {
        get => _decreaseStyle;
        set { _decreaseStyle = value; MarkDurationDirty(); }
    }

    /// <summary>
    /// Gets or sets the motion speed used when the value decreases.
    /// </summary>
    public MotionSpeed DecreaseSpeed
    {
        get => _decreaseSpeed;
        set { _decreaseSpeed = value; MarkDurationDirty(); }
    }

    /// <summary>
    /// Gets or sets whether reduced-motion settings are respected.
    /// </summary>
    public bool RespectReduceMotion { get; set; } = true;

    /// <summary>
    /// Gets the computed transition duration.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("MD3 spring transitions compute Duration automatically; do not set.", true)]
    public new TimeSpan? Duration
    {
        get => base.Duration;
        set => throw new InvalidOperationException(
            "MD3 spring transitions compute Duration automatically; setting Duration is not supported.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringDirectionalDoubleTransition" /> class.
    /// </summary>
    public SpringDirectionalDoubleTransition()
    {
        MotionSettings.GlobalSchemeChanged += (_, _) => MarkDurationDirty();
        PostRecomputeIfNeeded();
    }

    /// <inheritdoc />
    protected override double Interpolate(double progress, double from, double to)
    {
        if (_durationDirty)
            RecomputeAutoDuration();

        if (RespectReduceMotion && MotionSettings.ReduceMotion)
            return from + (to - from) * progress;

        var spring = to >= from ? _increase : _decrease;
        var elapsed = progress * _timelineSeconds;

        if (elapsed >= spring.DurationSeconds)
            return to;

        var y = SpringAnalytic.Evaluate(elapsed, spring.Stiffness, spring.Damping, initialVelocity: 0d);

        if (spring.Style == MotionStyle.Effects)
            y = Math.Clamp(y, 0d, 1d);

        return from + (to - from) * y;
    }

    private void MarkDurationDirty()
    {
        _durationDirty = true;
        PostRecomputeIfNeeded();
    }

    private void PostRecomputeIfNeeded()
    {
        if (_recomputePosted)
            return;

        _recomputePosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _recomputePosted = false;
            if (_durationDirty)
                RecomputeAutoDuration();
        }, DispatcherPriority.Render);
    }

    private void RecomputeAutoDuration()
    {
        var scheme = MotionSettings.GlobalScheme;
        _increase = Resolve(scheme, IncreaseStyle, IncreaseSpeed);
        _decrease = Resolve(scheme, DecreaseStyle, DecreaseSpeed);

        _timelineSeconds = Math.Max(_increase.DurationSeconds, _decrease.DurationSeconds);
        base.Duration = TimeSpan.FromSeconds(_timelineSeconds);
        _durationDirty = false;
    }

    private static CachedSpring Resolve(MotionScheme scheme, MotionStyle style, MotionSpeed speed)
    {
        var token = scheme.Resolve(style, speed);
        var seconds = SpringDuration.ComputeSeconds(
            token.Stiffness,
            token.Damping,
            effects: style == MotionStyle.Effects);

        return new CachedSpring(token.Stiffness, token.Damping, style, seconds);
    }

    private readonly record struct CachedSpring(
        double Stiffness,
        double Damping,
        MotionStyle Style,
        double DurationSeconds);
}
