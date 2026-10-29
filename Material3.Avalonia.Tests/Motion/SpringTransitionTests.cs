using System.Reflection;
using FluentAssertions;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Tests.Motion;

public sealed class SpringTransitionTests
{
    static SpringTransitionTests()
    {
        TestApp.EnsureStarted();
    }

    [Theory]
    [InlineData(MotionStyle.Spatial, 10)]
    [InlineData(MotionStyle.Effects, 5)]
    public void ReducedMotionUsesStyleSpecificFallback(MotionStyle style, double expected)
    {
        WithReducedMotion(() =>
        {
            var transition = new TestDoubleTransition { Style = style };

            transition.Sample(0.5, 0, 10).Should().Be(expected);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingTransitionsFollowSchemeChangesAfterGarbageCollection(bool directional)
    {
        var previousScheme = MotionSettings.GlobalScheme;
        var previousReducedMotion = MotionSettings.ReduceMotion;
        try
        {
            MotionSettings.ReduceMotion = false;
            MotionSettings.GlobalScheme = MotionScheme.Expressive;
            var transition = CreateTransition();
            var expressive = Read(transition);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            MotionSettings.GlobalScheme = MotionScheme.Standard;

            var standard = Read(CreateTransition());
            standard.Should().NotBeApproximately(expressive, 0.01);
            Read(transition).Should().BeApproximately(standard, 0.0001);
            GC.KeepAlive(transition);
        }
        finally
        {
            MotionSettings.GlobalScheme = previousScheme;
            MotionSettings.ReduceMotion = previousReducedMotion;
        }

        object CreateTransition() => directional
            ? new SpringDirectionalDoubleTransition
            {
                IncreaseStyle = MotionStyle.Spatial, DecreaseStyle = MotionStyle.Spatial,
                IncreaseSpeed = MotionSpeed.Fast, DecreaseSpeed = MotionSpeed.Fast
            }
            : new TestDoubleTransition { Speed = MotionSpeed.Fast };

        static double Read(object transition) => transition is SpringDirectionalDoubleTransition value
            ? Sample(value, 0.5, 0, 1)
            : ((TestDoubleTransition)transition).Sample(0.5, 0, 1);
    }

    [Fact]
    public void RespectReduceMotionFalseKeepsSpringInterpolation()
    {
        WithReducedMotion(() =>
        {
            var transition = new TestDoubleTransition
            {
                Style = MotionStyle.Spatial,
                RespectReduceMotion = false
            };

            transition.Sample(0.25, 0, 10).Should().NotBe(10);
        });
    }

    [Theory]
    [InlineData(MotionStyle.Spatial, 10)]
    [InlineData(MotionStyle.Effects, 5)]
    public void DirectionalTransitionUsesActiveDirectionStyle(MotionStyle style, double expected)
    {
        WithReducedMotion(() =>
        {
            var transition = new SpringDirectionalDoubleTransition
            {
                IncreaseStyle = style,
                DecreaseStyle = style
            };

            Sample(transition, 0.5, 0, 10).Should().Be(expected);
            Sample(transition, 0.5, 10, 0).Should().Be(10 - expected);
        });
    }

    [Theory]
    [InlineData(MotionStyle.Spatial, 10)]
    [InlineData(MotionStyle.Effects, 5)]
    public void NullableTransitionDelegatesExplicitValuesToBasePolicy(MotionStyle style, double expected)
    {
        WithReducedMotion(() =>
        {
            var transition = new SpringNullableDoubleTransition { Style = style };

            Sample(transition, 0.5, 0d, 10d).Should().Be(expected);
            Sample(transition, 0.5, 0d, null).Should().BeNull();
        });
    }

    private static double Sample(SpringDirectionalDoubleTransition transition, double progress, double from,
        double to) =>
        (double)typeof(SpringDirectionalDoubleTransition)
            .GetMethod("Interpolate", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(transition, [progress, from, to])!;

    private static double? Sample(SpringNullableDoubleTransition transition, double progress, double? from,
        double? to) =>
        (double?)typeof(SpringNullableDoubleTransition)
            .GetMethod("Interpolate", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(transition, [progress, from, to]);

    private static void WithReducedMotion(Action action)
    {
        var previous = MotionSettings.ReduceMotion;
        try
        {
            MotionSettings.ReduceMotion = true;
            action();
        }
        finally
        {
            MotionSettings.ReduceMotion = previous;
        }
    }

    private sealed class TestDoubleTransition : SpringTransitionBase<double, DoubleAdapter>
    {
        public double Sample(double progress, double from, double to) => base.Interpolate(progress, from, to);
    }
}