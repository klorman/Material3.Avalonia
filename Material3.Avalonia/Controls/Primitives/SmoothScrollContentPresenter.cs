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
    private const double MaximumDurationMilliseconds = 200d;
    private const double MinimumDurationMilliseconds = 100d;
    private const double MaximumDurationDistance = 120d;
    private const double MinimumDurationDistance = 480d;
    private const double BezierX1 = 0.42d;
    private const double BezierX2 = 0.58d;
    private const double NumericEpsilon = 0.0001d;

    private bool _isAnimating;
    private bool _isFrameRequested;
    private bool _isSettingAnimatedOffset;
    private bool _hasAnimationStartTime;
    private bool _hasLastFrameTime;
    private ulong _animationGeneration;
    private TimeSpan _animationStartTime;
    private TimeSpan _animationDuration;
    private TimeSpan _lastFrameTime;
    private Vector _animationStartOffset;
    private Vector _initialVelocity;
    private Vector _currentVelocity;
    private Vector _targetOffset;

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
        {
            e.Handled = !IsScrollChainingEnabled;
            return;
        }

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
            var clampedTarget = ClampOffset(_targetOffset);

            if (_isAnimating && clampedTarget != _targetOffset)
                BeginAnimation(clampedTarget);
            else
                _targetOffset = clampedTarget;
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
        var wheelScrollDistance = ScrollViewerAssist.GetWheelScrollDistance(this);
        var x = baseOffset.X;
        var y = baseOffset.Y;

        if (Extent.Height > Viewport.Height)
            y += -delta.Y * wheelScrollDistance;

        if (Extent.Width > Viewport.Width)
            x += -delta.X * wheelScrollDistance;

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

        BeginAnimation(targetOffset);
    }

    private void BeginAnimation(Vector targetOffset)
    {
        var wasAnimating = _isAnimating;
        _animationStartOffset = ClampOffset(Offset);
        _initialVelocity = wasAnimating ? _currentVelocity : default;
        _currentVelocity = _initialVelocity;
        _targetOffset = targetOffset;
        _animationDuration = GetAnimationDuration(targetOffset - _animationStartOffset);
        _hasAnimationStartTime = wasAnimating && _hasLastFrameTime;

        if (_hasAnimationStartTime)
            _animationStartTime = _lastFrameTime;

        if (!wasAnimating)
            _animationGeneration++;

        _isAnimating = true;
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

        if (!_hasAnimationStartTime)
        {
            _animationStartTime = timestamp;
            _hasAnimationStartTime = true;
        }

        _hasLastFrameTime = true;
        _lastFrameTime = timestamp;
        _targetOffset = ClampOffset(_targetOffset);

        var elapsed = timestamp - _animationStartTime;
        var progress = Clamp(elapsed.TotalMilliseconds / _animationDuration.TotalMilliseconds, 0d, 1d);
        var frame = EvaluateAnimationFrame(
            _animationStartOffset,
            _targetOffset,
            _initialVelocity,
            _animationDuration,
            progress);
        var unclampedOffset = frame.Offset;
        var nextOffset = ClampOffset(unclampedOffset);

        _currentVelocity = frame.Velocity;

        if (!AreClose(nextOffset.X, unclampedOffset.X))
            _currentVelocity = _currentVelocity.WithX(0d);

        if (!AreClose(nextOffset.Y, unclampedOffset.Y))
            _currentVelocity = _currentVelocity.WithY(0d);

        SetAnimatedOffset(nextOffset);
        InvalidateVisual();

        if (progress >= 1d)
        {
            SetAnimatedOffset(_targetOffset);
            CancelAnimation(_targetOffset);
            return;
        }

        RequestAnimationFrame();
    }

    internal static (Vector Offset, Vector Velocity) EvaluateAnimationFrame(
        Vector startOffset,
        Vector targetOffset,
        Vector initialVelocity,
        TimeSpan duration,
        double progress)
    {
        progress = Clamp(progress, 0d, 1d);
        var easedProgress = Ease(progress, out var easedVelocity);
        var durationSeconds = duration.TotalSeconds;
        var velocityBlend = 1d - 4d * progress + 3d * progress * progress;
        var velocityOffset = initialVelocity * (durationSeconds * progress * Square(1d - progress));
        var animationDelta = targetOffset - startOffset;
        var unconstrainedOffset = startOffset + animationDelta * easedProgress + velocityOffset;
        var offset = new Vector(
            StopAtTarget(startOffset.X, targetOffset.X, unconstrainedOffset.X),
            StopAtTarget(startOffset.Y, targetOffset.Y, unconstrainedOffset.Y));
        var velocity = animationDelta * (easedVelocity / durationSeconds) + initialVelocity * velocityBlend;

        if (!AreClose(offset.X, unconstrainedOffset.X))
            velocity = velocity.WithX(0d);

        if (!AreClose(offset.Y, unconstrainedOffset.Y))
            velocity = velocity.WithY(0d);

        return (offset, velocity);
    }

    private static double StopAtTarget(double start, double target, double value)
    {
        if (AreClose(start, target))
            return target;

        return target > start ? Math.Min(value, target) : Math.Max(value, target);
    }

    private static TimeSpan GetAnimationDuration(Vector distance)
    {
        var length = Math.Max(Math.Abs(distance.X), Math.Abs(distance.Y));
        var durationProgress = Clamp(
            (length - MaximumDurationDistance) / (MinimumDurationDistance - MaximumDurationDistance),
            0d,
            1d);
        var milliseconds = MaximumDurationMilliseconds +
                           (MinimumDurationMilliseconds - MaximumDurationMilliseconds) * durationProgress;
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static double Ease(double progress, out double velocity)
    {
        if (progress <= 0d)
        {
            velocity = 0d;
            return 0d;
        }

        if (progress >= 1d)
        {
            velocity = 0d;
            return 1d;
        }

        var parameter = progress;

        for (var iteration = 0; iteration < 8; iteration++)
        {
            var error = CubicBezier(parameter, BezierX1, BezierX2) - progress;
            var derivative = CubicBezierDerivative(parameter, BezierX1, BezierX2);

            if (Math.Abs(derivative) < NumericEpsilon)
                break;

            parameter = Clamp(parameter - error / derivative, 0d, 1d);
        }

        var xVelocity = CubicBezierDerivative(parameter, BezierX1, BezierX2);
        var yVelocity = CubicBezierDerivative(parameter, 0d, 1d);
        velocity = IsZero(xVelocity) ? 0d : yVelocity / xVelocity;
        return CubicBezier(parameter, 0d, 1d);
    }

    private static double CubicBezier(double parameter, double point1, double point2)
    {
        var inverse = 1d - parameter;
        return 3d * inverse * inverse * parameter * point1 +
               3d * inverse * parameter * parameter * point2 +
               parameter * parameter * parameter;
    }

    private static double CubicBezierDerivative(double parameter, double point1, double point2)
    {
        var inverse = 1d - parameter;
        return 3d * inverse * inverse * point1 +
               6d * inverse * parameter * (point2 - point1) +
               3d * parameter * parameter * (1d - point2);
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
        _hasAnimationStartTime = false;
        _hasLastFrameTime = false;
        _initialVelocity = default;
        _currentVelocity = default;
        _targetOffset = synchronizedOffset ?? Offset;
        _animationGeneration++;
    }

    private static bool AreClose(Vector left, Vector right) =>
        LengthSquared(left - right) < NumericEpsilon * NumericEpsilon;

    private static bool AreClose(double left, double right) =>
        Math.Abs(left - right) < NumericEpsilon;

    private static bool IsZero(double value) =>
        Math.Abs(value) < NumericEpsilon;

    private static double LengthSquared(Vector vector) =>
        vector.X * vector.X + vector.Y * vector.Y;

    private static double Square(double value) => value * value;

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