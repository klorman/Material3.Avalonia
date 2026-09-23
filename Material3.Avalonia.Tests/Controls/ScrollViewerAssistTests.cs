using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Converters;

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
}