using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Motion;

namespace Material3.Avalonia.Tests.Controls;

public sealed class ScrollViewerAssistTests
{
    static ScrollViewerAssistTests()
    {
        TestApp.EnsureStarted();
    }

    [Fact]
    public void ScrollBarInset_ShouldInheritFromParentControl()
    {
        var panel = new StackPanel();
        var scrollViewer = new ScrollViewer();

        ScrollViewerAssist.SetScrollBarInset(panel, new Thickness(1, 2, 3, 4));
        panel.Children.Add(scrollViewer);

        ScrollViewerAssist.GetScrollBarInset(scrollViewer)
            .Should().Be(new Thickness(1, 2, 3, 4));
    }

    [Fact]
    public void ScrollBarInsetFromCornerRadiusConverter_ShouldUseBottomAndRightCorners()
    {
        var converter = new ScrollBarInsetFromCornerRadiusConverter();

        var inset = converter.Convert(
            new CornerRadius(1, 2, 3, 4),
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        inset.Should().Be(new Thickness(4, 2, 3, 3));
    }

    [Fact]
    public void ScrollBarTrackMarginConverter_ShouldUseTopAndBottomForVerticalScrollBar()
    {
        var converter = new ScrollBarTrackMarginConverter();

        var margin = converter.Convert(
            new Thickness(4, 2, 3, 5),
            typeof(Thickness),
            Orientation.Vertical,
            CultureInfo.InvariantCulture);

        margin.Should().Be(new Thickness(0, 2, 0, 5));
    }

    [Fact]
    public void ScrollBarTrackMarginConverter_ShouldUseLeftAndRightForHorizontalScrollBar()
    {
        var converter = new ScrollBarTrackMarginConverter();

        var margin = converter.Convert(
            new Thickness(4, 2, 3, 5),
            typeof(Thickness),
            Orientation.Horizontal,
            CultureInfo.InvariantCulture);

        margin.Should().Be(new Thickness(4, 0, 3, 0));
    }

    [Fact]
    public void SmoothWheelScrolling_ShouldStillBeInProgressAfterFiftyMilliseconds()
    {
        var previousReduceMotion = MotionSettings.ReduceMotion;

        try
        {
            MotionSettings.ReduceMotion = false;
            var (window, scrollViewer, presenter) = ShowScrollViewer();

            try
            {
                RaiseWheel(presenter, window, -1d);
                PumpFrames(4);

                scrollViewer.Offset.Y.Should().BeGreaterThan(0d).And.BeLessThan(30d);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReduceMotion;
        }
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(165)]
    public void SmoothWheelAnimation_ShouldReachTheSamePositionAtFiftyMilliseconds(int framesPerSecond)
    {
        const double progressAtFiftyMilliseconds = 0.25d;
        var duration = TimeSpan.FromMilliseconds(200);
        var frameInterval = TimeSpan.FromSeconds(1d / framesPerSecond);
        (Vector Offset, Vector Velocity) frame = default;

        for (var timestamp = TimeSpan.Zero; timestamp < TimeSpan.FromMilliseconds(50); timestamp += frameInterval)
        {
            frame = SmoothScrollContentPresenter.EvaluateAnimationFrame(
                default,
                new Vector(0, 120),
                default,
                duration,
                timestamp.TotalMilliseconds / duration.TotalMilliseconds);
        }

        frame = SmoothScrollContentPresenter.EvaluateAnimationFrame(
            default,
            new Vector(0, 120),
            default,
            duration,
            progressAtFiftyMilliseconds);

        frame.Offset.Y.Should().BeApproximately(15.4994d, 0.001d);
    }

    [Fact]
    public void SmoothWheelAnimation_ShouldNotCrossAndReturnToRetargetedOffset()
    {
        var offsets = new[] { 0.5d, 0.6d, 0.7d, 0.8d, 0.9d, 1d }
            .Select(progress => SmoothScrollContentPresenter.EvaluateAnimationFrame(
                default,
                new Vector(0, 40),
                new Vector(0, 1000),
                TimeSpan.FromMilliseconds(200),
                progress).Offset.Y)
            .ToArray();

        offsets.Should().OnlyContain(offset => offset <= 40d);
        offsets.Should().BeInAscendingOrder();
        offsets[^1].Should().Be(40d);
    }

    [Theory]
    [InlineData(120d, -1d, 120d)]
    [InlineData(80d, -1d, 80d)]
    [InlineData(120d, -0.5d, 60d)]
    public void SmoothWheelScrolling_ShouldAnimateToConfiguredProportionalDistance(
        double wheelScrollDistance,
        double wheelDelta,
        double expectedOffset)
    {
        var previousReduceMotion = MotionSettings.ReduceMotion;

        try
        {
            MotionSettings.ReduceMotion = false;
            var (window, scrollViewer, presenter) = ShowScrollViewer(wheelScrollDistance);

            try
            {
                RaiseWheel(presenter, window, wheelDelta);
                PumpFrames(4);
                scrollViewer.Offset.Y.Should().BeGreaterThan(0d).And.BeLessThan(expectedOffset);

                PumpUntil(() => Math.Abs(scrollViewer.Offset.Y - expectedOffset) < 0.01d);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReduceMotion;
        }
    }

    [Fact]
    public void SmoothWheelScrolling_ShouldAccumulateRetargetReverseAndClamp()
    {
        var previousReduceMotion = MotionSettings.ReduceMotion;

        try
        {
            MotionSettings.ReduceMotion = false;
            var (window, scrollViewer, presenter) = ShowScrollViewer();

            try
            {
                scrollViewer.Offset = new Vector(0, 400);
                RaiseWheel(presenter, window, -1d);
                PumpFrames(3);
                RaiseWheel(presenter, window, -1d);
                PumpFrames(3);
                var offsetBeforeReversal = scrollViewer.Offset.Y;

                RaiseWheel(presenter, window, 3d);
                PumpFrames(1);
                scrollViewer.Offset.Y.Should().BeGreaterThanOrEqualTo(offsetBeforeReversal);

                PumpUntil(() => Math.Abs(scrollViewer.Offset.Y - 280d) < 0.01d);

                RaiseWheel(presenter, window, -1d);
                PumpFrames(3);
                scrollViewer.Offset = new Vector(0, 700);
                PumpFrames(5);
                scrollViewer.Offset.Y.Should().Be(700d);

                scrollViewer.Offset = new Vector(0, 950);
                RaiseWheel(presenter, window, -1d);
                PumpUntil(() => Math.Abs(scrollViewer.Offset.Y - 1000d) < 0.01d);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReduceMotion;
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void WheelScrolling_ShouldRemainImmediateWhenSmoothAnimationIsUnavailable(
        bool reduceMotion,
        bool smoothWheelScrollingEnabled)
    {
        var previousReduceMotion = MotionSettings.ReduceMotion;

        try
        {
            MotionSettings.ReduceMotion = reduceMotion;
            var (window, scrollViewer, presenter) = ShowScrollViewer(
                smoothWheelScrollingEnabled: smoothWheelScrollingEnabled);

            try
            {
                RaiseWheel(presenter, window, -1d);
                var immediateOffset = scrollViewer.Offset.Y;
                immediateOffset.Should().BeGreaterThan(0d);

                PumpFrames(5);
                scrollViewer.Offset.Y.Should().Be(immediateOffset);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReduceMotion;
        }
    }

    [Fact]
    public void WheelScrolling_ShouldRemainNativeForLogicalScrolling()
    {
        var previousReduceMotion = MotionSettings.ReduceMotion;

        try
        {
            MotionSettings.ReduceMotion = false;
            var logicalContent = new LogicalScrollableControl();
            var (window, _, presenter) = ShowScrollViewer(content: logicalContent);

            try
            {
                RaiseWheel(presenter, window, -1d);
                var immediateOffset = logicalContent.Offset.Y;
                immediateOffset.Should().BeGreaterThan(0d);

                PumpFrames(5);
                logicalContent.Offset.Y.Should().Be(immediateOffset);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReduceMotion;
        }
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ScrollBarThumbUsesDynamicFullCornerRadius(Orientation orientation)
    {
        var resources = (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"));
        resources.TryGetResource(typeof(ScrollBar), null, out var theme).Should().BeTrue();
        var scrollBar = new ScrollBar
        {
            Theme = theme.Should().BeOfType<ControlTheme>().Subject,
            Orientation = orientation,
            Minimum = 0,
            Maximum = 100,
            Value = 20,
            ViewportSize = 20,
            Width = orientation == Orientation.Horizontal ? 160 : 12,
            Height = orientation == Orientation.Vertical ? 160 : 12
        };
        scrollBar.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 200, Height = 200, Content = scrollBar };
        try
        {
            window.Show();
            scrollBar.ApplyStyling();
            scrollBar.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var thumbVisual = scrollBar.GetVisualDescendants().OfType<Border>()
                .Should().ContainSingle(x => x.Name == "PART_ThumbVisual").Subject;
            thumbVisual.CornerRadius.Should().Be(new CornerRadius(2));
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, ScrollViewer ScrollViewer, SmoothScrollContentPresenter Presenter) ShowScrollViewer(
        double? wheelScrollDistance = null,
        bool smoothWheelScrollingEnabled = true,
        Control? content = null)
    {
        var resources = (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"));
        resources.TryGetResource(typeof(ScrollViewer), null, out var theme).Should().BeTrue();
        var scrollViewer = new ScrollViewer
        {
            Theme = theme.Should().BeOfType<ControlTheme>().Subject,
            Width = 200,
            Height = 200,
            Content = content ?? new Border { Width = 200, Height = 1200, Background = Brushes.White }
        };
        scrollViewer.Resources.MergedDictionaries.Add(resources);
        ScrollViewerAssist.SetIsSmoothWheelScrollingEnabled(scrollViewer, smoothWheelScrollingEnabled);

        if (wheelScrollDistance.HasValue)
            ScrollViewerAssist.SetWheelScrollDistance(scrollViewer, wheelScrollDistance.Value);

        var window = new Window { Width = 200, Height = 200, Content = scrollViewer };
        window.Show();
        window.UpdateLayout();
        scrollViewer.ApplyTemplate();
        var presenter = scrollViewer.GetVisualDescendants().OfType<SmoothScrollContentPresenter>().Single();
        return (window, scrollViewer, presenter);
    }

    private static void RaiseWheel(
        SmoothScrollContentPresenter presenter,
        Window window,
        double verticalDelta)
    {
        var point = presenter.TranslatePoint(new Point(20, 20), window)!.Value;
        presenter.RaiseEvent(new PointerWheelEventArgs(
            presenter,
            new Pointer(1, PointerType.Mouse, true),
            window,
            point,
            0,
            new PointerPointProperties(),
            KeyModifiers.None,
            new Vector(0, verticalDelta)));
    }

    private static void PumpFrames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            Thread.Sleep(5);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();

        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(2))
            PumpFrames(1);

        condition().Should().BeTrue();
    }

    private sealed class LogicalScrollableControl : Control, ILogicalScrollable
    {
        private Vector _offset;

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public bool IsLogicalScrollEnabled => true;

        public Size ScrollSize => new(1, 1);

        public Size PageScrollSize => new(1, 10);

        public Size Extent => new(1, 100);

        public Vector Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                RaiseScrollInvalidated(EventArgs.Empty);
            }
        }

        public Size Viewport => new(1, 10);

        public event EventHandler? ScrollInvalidated;

        public bool BringIntoView(Control target, Rect targetRect) => false;

        public Control? GetControlInDirection(NavigationDirection direction, Control? from) => null;

        public void RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);

        protected override Size MeasureOverride(Size availableSize) => new(200, 200);
    }
}