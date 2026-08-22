using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Motion;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Scroll content presenter that smooths mouse wheel scrolling while preserving native non-wheel scroll behavior.
/// </summary>
public sealed class SmoothScrollContentPresenter : ScrollContentPresenter
{
    private const double WheelScrollPixels = 50d;
    private const double DefaultFrameSeconds = 1d / 60d;
    private const double MaximumFrameSeconds = 0.05d;
    private const double TargetEpsilon = 0.5d;
    private const double VelocityEpsilon = 5d;
    private const double NumericEpsilon = 0.0001d;

    private bool _isAnimating;
    private bool _isFrameRequested;
    private bool _isSettingAnimatedOffset;
    private bool _hasFrameTime;
    private ulong _animationGeneration;
    private TimeSpan _lastFrameTime;
    private Vector _targetOffset;
    private Vector _velocity;

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (ShouldUseNativeWheelScrolling())
        {
            CancelAnimation();
            base.OnPointerWheelChanged(e);
            return;
        }

        if (Extent.Height <= Viewport.Height && Extent.Width <= Viewport.Width)
            return;

        var delta = NormalizeWheelDelta(e.Delta, e.KeyModifiers);
        var baseOffset = _isAnimating ? _targetOffset : Offset;
        var targetOffset = ComputeWheelTarget(baseOffset, delta);
        var consumesWheel = _isAnimating || targetOffset != Offset;

        if (consumesWheel)
            AnimateTo(targetOffset);

        e.Handled = !IsScrollChainingEnabled || consumesWheel;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        var wasAnimatedOffsetChange = change.Property == OffsetProperty && _isSettingAnimatedOffset;

        base.OnPropertyChanged(change);

        if (change.Property == OffsetProperty)
        {
            if (!wasAnimatedOffsetChange)
                CancelAnimation(change.GetNewValue<Vector>());
        }
        else if (change.Property == ExtentProperty || change.Property == ViewportProperty)
        {
            _targetOffset = ClampOffset(_targetOffset);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelAnimation();
        base.OnDetachedFromVisualTree(e);
    }

    private bool ShouldUseNativeWheelScrolling()
    {
        if (!ScrollViewerAssist.GetIsSmoothWheelScrollingEnabled(this) || MotionSettings.ReduceMotion)
            return true;

        if (HorizontalSnapPointsType != SnapPointsType.None || VerticalSnapPointsType != SnapPointsType.None)
            return true;

        return Child is ILogicalScrollable { IsLogicalScrollEnabled: true };
    }

    private Vector NormalizeWheelDelta(Vector delta, KeyModifiers keyModifiers)
    {
        if (keyModifiers == KeyModifiers.Shift && IsZero(delta.X))
            return new Vector(delta.Y, delta.X);

        return FlowDirection == FlowDirection.RightToLeft ? delta.WithX(-delta.X) : delta;
    }

    private Vector ComputeWheelTarget(Vector baseOffset, Vector delta)
    {
        var x = baseOffset.X;
        var y = baseOffset.Y;

        if (Extent.Height > Viewport.Height)
            y += -delta.Y * WheelScrollPixels;

        if (Extent.Width > Viewport.Width)
            x += -delta.X * WheelScrollPixels;

        return ClampOffset(new Vector(x, y));
    }

    private Vector ClampOffset(Vector offset)
    {
        var maxX = Max(Extent.Width - Viewport.Width, 0d);
        var maxY = Max(Extent.Height - Viewport.Height, 0d);

        return new Vector(
            Clamp(offset.X, 0d, maxX),
            Clamp(offset.Y, 0d, maxY));
    }

    private void AnimateTo(Vector targetOffset)
    {
        targetOffset = ClampOffset(targetOffset);

        if (AreClose(targetOffset, Offset))
        {
            SetAnimatedOffset(targetOffset);
            CancelAnimation(targetOffset);
            return;
        }

        if (!_isAnimating)
        {
            _velocity = default;
            _hasFrameTime = false;
            _animationGeneration++;
        }

        _isAnimating = true;
        _targetOffset = targetOffset;
        RequestAnimationFrame();
    }

    private void RequestAnimationFrame()
    {
        if (_isFrameRequested)
            return;

        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            SetAnimatedOffset(_targetOffset);
            CancelAnimation(_targetOffset);
            return;
        }

        _isFrameRequested = true;
        var generation = _animationGeneration;
        topLevel.RequestAnimationFrame(timestamp => OnAnimationFrame(timestamp, generation));
    }

    private void OnAnimationFrame(TimeSpan timestamp, ulong generation)
    {
        _isFrameRequested = false;

        if (!_isAnimating || generation != _animationGeneration)
            return;

        if (!ScrollViewerAssist.GetIsSmoothWheelScrollingEnabled(this) || MotionSettings.ReduceMotion)
        {
            SetAnimatedOffset(ClampOffset(_targetOffset));
            CancelAnimation(_targetOffset);
            return;
        }

        var dt = _hasFrameTime
            ? Math.Clamp((timestamp - _lastFrameTime).TotalSeconds, DefaultFrameSeconds, MaximumFrameSeconds)
            : DefaultFrameSeconds;

        _hasFrameTime = true;
        _lastFrameTime = timestamp;
        _targetOffset = ClampOffset(_targetOffset);

        var nextOffset = StepCriticalSpring(ClampOffset(Offset), _targetOffset, dt, ref _velocity);
        nextOffset = ClampOffset(nextOffset);

        SetAnimatedOffset(nextOffset);

        if (IsSettled(nextOffset, _targetOffset, _velocity))
        {
            SetAnimatedOffset(_targetOffset);
            CancelAnimation(_targetOffset);
            return;
        }

        RequestAnimationFrame();
    }

    private Vector StepCriticalSpring(Vector current, Vector target, double dt, ref Vector velocity)
    {
        var token = MotionSettings.GlobalScheme.Resolve(MotionStyle.Spatial, MotionSpeed.Fast);
        // Scroll must be monotonic; use the scheme stiffness but force critical damping to avoid overshoot.
        var omega = Math.Sqrt(Math.Max(1e-9, token.Stiffness));

        var vx = velocity.X;
        var vy = velocity.Y;
        var x = StepAxis(current.X, target.X, dt, omega, ref vx);
        var y = StepAxis(current.Y, target.Y, dt, omega, ref vy);
        velocity = new Vector(vx, vy);

        return new Vector(x, y);
    }

    private static double StepAxis(double current, double target, double dt, double omega, ref double velocity)
    {
        var error = current - target;
        var springTerm = velocity + omega * error;
        var decay = Math.Exp(-omega * dt);
        var nextError = (error + springTerm * dt) * decay;
        var nextVelocity = (velocity - omega * springTerm * dt) * decay;
        var next = target + nextError;

        if (!IsZero(target - current) && Math.Sign(target - current) != Math.Sign(target - next))
        {
            velocity = 0d;
            return target;
        }

        velocity = nextVelocity;
        return next;
    }

    private void SetAnimatedOffset(Vector offset)
    {
        _isSettingAnimatedOffset = true;

        try
        {
            SetCurrentValue(OffsetProperty, offset);
        }
        finally
        {
            _isSettingAnimatedOffset = false;
        }
    }

    private void CancelAnimation(Vector? synchronizedOffset = null)
    {
        _isAnimating = false;
        _isFrameRequested = false;
        _hasFrameTime = false;
        _velocity = default;
        _targetOffset = synchronizedOffset ?? Offset;
        _animationGeneration++;
    }

    private static bool IsSettled(Vector offset, Vector target, Vector velocity) =>
        LengthSquared(target - offset) < TargetEpsilon * TargetEpsilon &&
        LengthSquared(velocity) < VelocityEpsilon * VelocityEpsilon;

    private static bool AreClose(Vector left, Vector right) =>
        LengthSquared(left - right) < NumericEpsilon * NumericEpsilon;

    private static bool IsZero(double value) =>
        Math.Abs(value) < NumericEpsilon;

    private static double LengthSquared(Vector vector) =>
        vector.X * vector.X + vector.Y * vector.Y;

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value))
            return min;

        return value < min ? min : value > max ? max : value;
    }

    private static double Max(double x, double y)
    {
        var result = Math.Max(x, y);
        return double.IsNaN(result) ? 0d : result;
    }
}
