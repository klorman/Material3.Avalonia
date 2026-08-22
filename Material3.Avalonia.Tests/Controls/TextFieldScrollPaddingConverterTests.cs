using System.Globalization;
using Avalonia;
using Avalonia.Media;
using FluentAssertions;
using Material3.Avalonia.Converters;

namespace Material3.Avalonia.Tests.Controls;

public sealed class TextFieldScrollPaddingConverterTests
{
    [Fact]
    public void Convert_ShouldReturnZeroPaddingForSingleLineTextField()
    {
        var converter = new TextFieldScrollPaddingConverter();

        var padding = converter.Convert(
            [24d, false, TextWrapping.NoWrap],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        padding.Should().Be(new Thickness(0));
    }

    [Fact]
    public void Convert_ShouldReserveTrailingWidthForAcceptsReturnTextField()
    {
        var converter = new TextFieldScrollPaddingConverter();

        var padding = converter.Convert(
            [24d, true, TextWrapping.NoWrap],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        padding.Should().Be(new Thickness(0, 0, 24, 0));
    }

    [Fact]
    public void Convert_ShouldReserveTrailingWidthForWrappingTextField()
    {
        var converter = new TextFieldScrollPaddingConverter();

        var padding = converter.Convert(
            [24d, false, TextWrapping.Wrap],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        padding.Should().Be(new Thickness(0, 0, 24, 0));
    }
}
