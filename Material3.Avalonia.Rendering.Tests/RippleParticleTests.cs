using Avalonia;
using FluentAssertions;
using Material3.Avalonia.Controls.Primitives;
using Xunit;

namespace Material3.Avalonia.Rendering.Tests;

public sealed class RippleParticleTests
{
    [Fact]
    public void RadiusTracksCurrentBoundsWithoutMovingCenter()
    {
        var start = new RippleVisual.Start(
            1,
            new Point(10, 10),
            1,
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            [0, 1],
            false);
        var particle = new RippleParticle(start, TimeSpan.Zero);

        var initial = particle.GetValues(TimeSpan.FromMilliseconds(500), new Size(20, 20));
        var expanded = particle.GetValues(TimeSpan.FromMilliseconds(500), new Size(100, 40));

        expanded.Radius.Should().BeGreaterThan(initial.Radius);
        particle.Center.Should().Be(start.Center);
    }
}