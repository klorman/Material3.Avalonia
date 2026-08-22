using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Converters;

namespace Material3.Avalonia.Tests.Controls;

public sealed class ScrollViewerAssistTests
{
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
}