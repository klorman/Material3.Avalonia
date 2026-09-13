using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class RippleVisual : CompositionCustomVisualHandler
{
    internal sealed record Appearance(IImmutableBrush? Brush, CornerRadius Corners, bool Bounded);

    internal sealed record Start(
        long Id,
        Point Center,
        double Radius,
        double Opacity,
        TimeSpan GrowDuration,
        TimeSpan FadeInDuration,
        double[] Easing,
        bool LatestOnly);

    internal sealed record End(long Id, TimeSpan Duration);

    internal sealed record Clear
    {
        internal static readonly Clear Instance = new();
    }

    private readonly List<RippleParticle> _particles = [];
    private Appearance _appearance = new(null, default, true);
    private bool _framePending;

    public override void OnMessage(object message)
    {
        switch (message)
        {
            case Appearance appearance:
                _appearance = appearance;
                break;
            case Start start:
                if (start.LatestOnly) _particles.Clear();
                _particles.Add(new RippleParticle(start, CompositionNow));
                break;
            case End end:
                _particles.FirstOrDefault(p => p.Id == end.Id)?.Release(CompositionNow, end.Duration);
                break;
            case Clear:
                _particles.Clear();
                break;
        }

        _particles.RemoveAll(p => p.IsFinished(CompositionNow));
        Invalidate();
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (_framePending || !_particles.Any(p => p.IsAnimating(CompositionNow))) return;
        _framePending = true;
        RegisterForNextAnimationFrameUpdate();
    }

    public override void OnAnimationFrameUpdate()
    {
        _framePending = false;
        _particles.RemoveAll(p => p.IsFinished(CompositionNow));
        Invalidate();
        RequestFrame();
    }

    public override Rect GetRenderBounds()
    {
        var bounds = new Rect(new Size(EffectiveSize.X, EffectiveSize.Y));
        if (!_appearance.Bounded)
            foreach (var particle in _particles)
                bounds = bounds.Union(particle.Bounds);
        return bounds;
    }

    public override void OnRender(ImmediateDrawingContext context)
    {
        if (_appearance.Brush is null) return;
        var bounds = GetRenderBounds();
        if (_appearance.Bounded)
        {
            using (context.PushClip(new RoundedRect(bounds, _appearance.Corners))) Draw(context, bounds);
        }
        else Draw(context, bounds);
    }

    private void Draw(ImmediateDrawingContext context, Rect bounds)
    {
        foreach (var particle in _particles)
        {
            var (radius, opacity) = particle.GetValues(CompositionNow);
            if (radius <= 0 || opacity <= 0) continue;
            using (context.PushOpacity(opacity, bounds))
                context.DrawEllipse(_appearance.Brush, null, particle.Center, radius, radius);
        }
    }
}