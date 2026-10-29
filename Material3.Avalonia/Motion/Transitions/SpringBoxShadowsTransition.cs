using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Motion.Transitions;

/// <summary>Animates native box shadows using the Material effects motion scheme.</summary>
public sealed class SpringBoxShadowsTransition : BoxShadowsTransition
{
    private readonly EventHandler _globalSchemeChanged;
    private MotionSpeed _speed = MotionSpeed.Fast;

    /// <summary>Gets or sets the effects motion speed.</summary>
    public MotionSpeed Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            UpdateMotion();
        }
    }

    /// <summary>Gets the duration computed from the motion scheme.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("MD3 spring transitions compute Duration automatically; do not set.", true)]
    public new TimeSpan Duration
    {
        get => base.Duration;
        set => throw new InvalidOperationException(
            "MD3 spring transitions compute Duration automatically; setting Duration is not supported.");
    }

    /// <summary>Initializes a new instance of the <see cref="SpringBoxShadowsTransition" /> class.</summary>
    public SpringBoxShadowsTransition()
    {
        _globalSchemeChanged = (_, _) => UpdateMotion();
        MotionSettings.GlobalSchemeChanged += _globalSchemeChanged;
        UpdateMotion();
    }

    private void UpdateMotion()
    {
        var token = MotionSettings.GlobalScheme.Resolve(MotionStyle.Effects, Speed);
        var seconds = SpringDuration.ComputeSeconds(token.Stiffness, token.Damping, effects: true);
        base.Duration = TimeSpan.FromSeconds(seconds);
        Easing = new ShadowEasing(token, seconds);
    }

    private sealed class ShadowEasing(SpringToken token, double seconds) : Easing
    {
        public override double Ease(double progress) => MotionSettings.ReduceMotion
            ? 1
            : Math.Clamp(SpringAnalytic.Evaluate(progress * seconds, token.Stiffness, token.Damping, 0), 0, 1);
    }
}