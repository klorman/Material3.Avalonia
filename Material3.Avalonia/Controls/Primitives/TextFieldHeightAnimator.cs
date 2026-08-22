using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material3.Avalonia.Motion;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Animates text-field container auto-height changes while preserving native TextBox measurement.
/// </summary>
public sealed class TextFieldHeightAnimator : Decorator
{
    private const double DurationSeconds = 0.15d;
    private const double NumericEpsilon = 0.0001d;

    private bool _hasHeight;
    private bool _isAnimating;
    private bool _isFrameRequested;
    private bool _hasAnimationStart;
    private ulong _animationGeneration;
    private TimeSpan _animationStart;
    private double _startHeight;
    private double _targetHeight;
    private double _animatedHeight;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is null)
            return default;

        Child.Measure(new Size(availableSize.Width, double.PositiveInfinity));

        var targetHeight = CoerceTargetHeight(Child.DesiredSize.Height, availableSize.Height);
        UpdateTargetHeight(targetHeight);

        return new Size(Child.DesiredSize.Width, _animatedHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelAnimation();
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateTargetHeight(double targetHeight)
    {
        if (!_hasHeight || MotionSettings.ReduceMotion || _animatedHeight <= NumericEpsilon)
        {
            SetHeightImmediately(targetHeight);
            return;
        }

        if (AreClose(targetHeight, _targetHeight))
            return;

        var wasAnimating = _isAnimating;
        _startHeight = _animatedHeight;
        _targetHeight = targetHeight;
        _hasAnimationStart = false;
        _isAnimating = true;

        if (!wasAnimating)
            _animationGeneration++;

        RequestAnimationFrame();
    }

    private void SetHeightImmediately(double height)
    {
        _hasHeight = true;
        _isAnimating = false;
        _isFrameRequested = false;
        _hasAnimationStart = false;
        _startHeight = height;
        _targetHeight = height;
        _animatedHeight = height;
        _animationGeneration++;
    }

    private void RequestAnimationFrame()
    {
        if (_isFrameRequested)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            SetHeightImmediately(_targetHeight);
            InvalidateMeasure();
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

        if (MotionSettings.ReduceMotion)
        {
            SetHeightImmediately(_targetHeight);
            InvalidateMeasure();
            return;
        }

        if (!_hasAnimationStart)
        {
            _animationStart = timestamp;
            _hasAnimationStart = true;
        }

        var progress = Math.Clamp((timestamp - _animationStart).TotalSeconds / DurationSeconds, 0d, 1d);
        var eased = StandardEasing(progress);

        _animatedHeight = _startHeight + (_targetHeight - _startHeight) * eased;
        InvalidateMeasure();

        if (progress >= 1d || AreClose(_animatedHeight, _targetHeight))
        {
            _animatedHeight = _targetHeight;
            CancelAnimation();
            InvalidateMeasure();
            return;
        }

        RequestAnimationFrame();
    }

    private void CancelAnimation()
    {
        _isAnimating = false;
        _isFrameRequested = false;
        _hasAnimationStart = false;
        _animationGeneration++;
    }

    private static double CoerceTargetHeight(double desiredHeight, double availableHeight)
    {
        if (!double.IsFinite(desiredHeight) || desiredHeight < 0d)
            return 0d;

        if (double.IsFinite(availableHeight))
            return Math.Min(desiredHeight, Math.Max(availableHeight, 0d));

        return desiredHeight;
    }

    // M3 does not define a text-field autogrow token; use standard easing for this small spatial change.
    private static double StandardEasing(double progress) =>
        CubicBezier(progress, 0.2d, 0d, 0d, 1d);

    private static double CubicBezier(double progress, double x1, double y1, double x2, double y2)
    {
        if (progress <= 0d)
            return 0d;

        if (progress >= 1d)
            return 1d;

        var t = progress;

        for (var i = 0; i < 8; i++)
        {
            var x = Cubic(t, x1, x2) - progress;
            if (Math.Abs(x) < 0.00001d)
                return Cubic(t, y1, y2);

            var dx = CubicDerivative(t, x1, x2);
            if (Math.Abs(dx) < 0.000001d)
                break;

            t -= x / dx;
            if (t is < 0d or > 1d)
                break;
        }

        var lower = 0d;
        var upper = 1d;
        t = progress;

        for (var i = 0; i < 12; i++)
        {
            var x = Cubic(t, x1, x2);
            if (Math.Abs(x - progress) < 0.00001d)
                break;

            if (x < progress)
                lower = t;
            else
                upper = t;

            t = (lower + upper) / 2d;
        }

        return Cubic(t, y1, y2);
    }

    private static double Cubic(double t, double p1, double p2)
    {
        var inverse = 1d - t;
        return 3d * inverse * inverse * t * p1 + 3d * inverse * t * t * p2 + t * t * t;
    }

    private static double CubicDerivative(double t, double p1, double p2)
    {
        var inverse = 1d - t;
        return 3d * inverse * inverse * p1 + 6d * inverse * t * (p2 - p1) + 3d * t * t * (1d - p2);
    }

    private static bool AreClose(double left, double right) =>
        Math.Abs(left - right) < NumericEpsilon;
}
