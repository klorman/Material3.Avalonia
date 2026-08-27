using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FluentAssertions;
using Material3.Avalonia.Markup;

namespace Material3.Avalonia.Tests.Markup;

public sealed class DynamicThicknessExtensionTests
{
    [Fact]
    public void ProvideValue_ShouldCreateOneDynamicResourceBindingForUniformThickness()
    {
        var binding = GetBinding(new DynamicThicknessExtension("TestSpace"));

        binding.Bindings.Should().HaveCount(1);
    }

    [Fact]
    public void ProvideValue_ShouldSplitWhitespaceSeparatedResourceKeys()
    {
        var binding = GetBinding(new DynamicThicknessExtension("TestLeft TestTop TestRight TestBottom"));

        binding.Bindings.Should().HaveCount(4);
    }

    [Fact]
    public void ProvideValue_ShouldRejectUnsupportedResourceKeyCount()
    {
        var provideValue = () => new DynamicThicknessExtension("TestLeft TestTop TestRight").ProvideValue(null!);

        provideValue.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_ShouldCreateUniformThicknessFromOneValue()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestSpace"));

        var thickness = converter.Convert(
            [8d],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        thickness.Should().Be(new Thickness(8));
    }

    [Fact]
    public void Convert_ShouldCreateHorizontalAndVerticalThicknessFromTwoValues()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestHorizontal", "TestVertical"));

        var thickness = converter.Convert(
            [24d, 8d],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        thickness.Should().Be(new Thickness(24, 8));
    }

    [Fact]
    public void Convert_ShouldCreateSideSpecificThicknessFromFourValues()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestLeft", "TestTop", "TestRight", "TestBottom"));

        var thickness = converter.Convert(
            [24d, 8d, 24d, 8d],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        thickness.Should().Be(new Thickness(24, 8, 24, 8));
    }

    [Fact]
    public void Convert_ShouldReturnDoNothingForUnsetValue()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestHorizontal", "TestVertical"));

        var thickness = converter.Convert(
            [24d, AvaloniaProperty.UnsetValue],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        thickness.Should().BeSameAs(BindingOperations.DoNothing);
    }

    [Fact]
    public void Convert_ShouldRejectNonNumericValue()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestSpace"));

        var convert = () => converter.Convert(
            ["8"],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        convert.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_ShouldRejectNonFiniteValue()
    {
        var converter = GetConverter(new DynamicThicknessExtension("TestSpace"));

        var convert = () => converter.Convert(
            [double.PositiveInfinity],
            typeof(Thickness),
            null,
            CultureInfo.InvariantCulture);

        convert.Should().Throw<InvalidOperationException>();
    }

    private static MultiBinding GetBinding(DynamicThicknessExtension extension)
    {
        var value = extension.ProvideValue(null!);

        return value.Should().BeOfType<MultiBinding>().Subject;
    }

    private static IMultiValueConverter GetConverter(DynamicThicknessExtension extension)
    {
        var binding = GetBinding(extension);
        binding.Converter.Should().NotBeNull();

        return binding.Converter!;
    }
}
