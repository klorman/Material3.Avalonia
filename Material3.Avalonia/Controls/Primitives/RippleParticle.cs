using Avalonia;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class RippleParticle(RippleVisual.Start start, TimeSpan started)
{
    internal long Id => start.Id;
    internal Point Center => start.Center;

    internal Rect Bounds => new(start.Center.X - start.Radius, start.Center.Y - start.Radius, 2 * start.Radius,
        2 * start.Radius);

    private TimeSpan? _released;
    private TimeSpan _fadeDuration;
    private double _releaseOpacity;

    internal void Release(TimeSpan now, TimeSpan duration)
    {
        if (_released is not null) return;
        var earliest = started + start.FadeInDuration;
        _released = duration > TimeSpan.Zero && now < earliest ? earliest : now;
        _releaseOpacity = start.Opacity * Fraction(_released.Value - started, start.FadeInDuration);
        _fadeDuration = duration;
    }

    internal bool IsFinished(TimeSpan now) => _released is { } released && now - released >= _fadeDuration;

    internal bool IsAnimating(TimeSpan now) => !IsFinished(now) &&
                                               (_released is not null || now - started < start.GrowDuration ||
                                                now - started < start.FadeInDuration);

    internal (double Radius, double Opacity) GetValues(TimeSpan now)
    {
        var progress = Fraction(now - started, start.GrowDuration);
        var index = progress * (start.Easing.Length - 1);
        var lower = (int)index;
        var upper = Math.Min(lower + 1, start.Easing.Length - 1);
        var growth = start.Easing[lower] + (start.Easing[upper] - start.Easing[lower]) * (index - lower);
        var opacity = _released is { } released && now >= released
            ? _releaseOpacity * (1 - Fraction(now - released, _fadeDuration))
            : start.Opacity * Fraction(now - started, start.FadeInDuration);
        return (Math.Max(0, start.Radius * growth), Math.Clamp(opacity, 0, 1));
    }

    private static double Fraction(TimeSpan elapsed, TimeSpan duration) =>
        duration <= TimeSpan.Zero ? 1 : Math.Clamp(elapsed.TotalSeconds / duration.TotalSeconds, 0, 1);
}