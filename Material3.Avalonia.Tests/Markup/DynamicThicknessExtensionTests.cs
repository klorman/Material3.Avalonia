using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Markup;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Markup;

public sealed class DynamicThicknessExtensionTests
{
    static DynamicThicknessExtensionTests()
    {
        TestApp.EnsureStarted();
    }

    [Fact]
    public void DynamicThickness_ShouldResolveWhitespaceSeparatedResourceKeys()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestLeft"] = 4d;
        control.Resources["TestTop"] = 8d;
        control.Resources["TestRight"] = 12d;
        control.Resources["TestBottom"] = 16d;

        BindThickness(control, new DynamicThicknessExtension("TestLeft TestTop TestRight TestBottom"));

        control.Value.Should().Be(new Thickness(4, 8, 12, 16));
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

    [Fact]
    public void DynamicThickness_ShouldResolveUniformAlias()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestSpace"] = new TokenAlias("SysSpace");
        control.Resources["SysSpace"] = 8d;

        BindThickness(control, new DynamicThicknessExtension("TestSpace"));

        control.Value.Should().Be(new Thickness(8));
    }

    [Fact]
    public void DynamicThickness_ShouldResolveHorizontalAndVerticalAliases()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestHorizontal"] = new TokenAlias("HorizontalSys");
        control.Resources["HorizontalSys"] = 24d;
        control.Resources["TestVertical"] = new TokenAlias("VerticalSys");
        control.Resources["VerticalSys"] = new TokenAlias("VerticalRef");
        control.Resources["VerticalRef"] = 8d;

        BindThickness(control, new DynamicThicknessExtension("TestHorizontal", "TestVertical"));

        control.Value.Should().Be(new Thickness(24, 8));
    }

    [Fact]
    public void DynamicThickness_ShouldResolveSideSpecificAliases()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestLeft"] = new TokenAlias("LeftSys");
        control.Resources["LeftSys"] = 4d;
        control.Resources["TestTop"] = new TokenAlias("TopSys");
        control.Resources["TopSys"] = 8d;
        control.Resources["TestRight"] = new TokenAlias("RightSys");
        control.Resources["RightSys"] = 12d;
        control.Resources["TestBottom"] = new TokenAlias("BottomSys");
        control.Resources["BottomSys"] = 16d;

        BindThickness(
            control,
            new DynamicThicknessExtension("TestLeft", "TestTop", "TestRight", "TestBottom"));

        control.Value.Should().Be(new Thickness(4, 8, 12, 16));
    }

    [Fact]
    public void DynamicThickness_ShouldUpdateWhenLeafResourceChanges()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestSpace"] = new TokenAlias("SysSpace");
        control.Resources["SysSpace"] = new TokenAlias("RefSpace");
        control.Resources["RefSpace"] = 8d;

        BindThickness(control, new DynamicThicknessExtension("TestSpace"));

        control.Value.Should().Be(new Thickness(8));

        control.Resources["RefSpace"] = 12d;

        control.Value.Should().Be(new Thickness(12));
    }

    [Fact]
    public void DynamicThickness_ShouldResolveNestedDynamicResourceKeyAliasAndLeafReplacement()
    {
        var control = new ThicknessTestControl();
        control.Resources["TestSpace"] = new TokenAlias("SysSpace");
        control.Resources["SysSpace"] = new TokenAlias("RefSpace");
        control.Resources["RefSpace"] = 8d;

        BindThickness(control, new DynamicThicknessExtension(new DynamicResourceExtension("TestSpace")));

        control.Value.Should().Be(new Thickness(8));

        control.Resources["RefSpace"] = 12d;

        control.Value.Should().Be(new Thickness(12));
    }

    [Fact]
    public void DynamicThickness_ShouldResolveEachAliasFromOriginalTargetScope()
    {
        var root = new Grid();
        root.Resources["TestSpace"] = new TokenAlias("SysSpace");
        root.Resources["SysSpace"] = new TokenAlias("RefSpace");
        root.Resources["RefSpace"] = 8d;

        var local = new Grid();
        local.Resources["SysSpace"] = new TokenAlias("LocalSpace");
        local.Resources["LocalSpace"] = 24d;

        var control = new ThicknessTestControl();
        root.Children.Add(local);
        local.Children.Add(control);

        BindThickness(control, new DynamicThicknessExtension("TestSpace"));

        control.Value.Should().Be(new Thickness(24));

        local.Resources["SysSpace"] = 32d;

        control.Value.Should().Be(new Thickness(32));
    }

    [Fact]
    public void DynamicThickness_ShouldUseTemplatePriorityWhenUsedDirectlyInsideControlTemplate()
    {
        var button = AvaloniaXamlLoader.Load(new Uri(
                "avares://Material3.Avalonia.Tests/Markup/DynamicThicknessExtensionTemplateSmoke.axaml"))
            .Should().BeOfType<Button>()
            .Subject;
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = button
        };

        try
        {
            window.Show();
            button.ApplyStyling();
            button.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();

            var border = GetTemplateBorder(button, "PART_DynamicThickness");
            border.Padding.Should().Be(new Thickness(1));

            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            button.IsFocused.Should().BeTrue();
            border.Padding.Should().Be(new Thickness(3));
        }
        finally
        {
            window.Close();
        }
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

    private static void BindThickness(ThicknessTestControl control, DynamicThicknessExtension extension)
    {
        control.Bind(ThicknessTestControl.ValueProperty, GetBinding(extension));
    }

    private static Border GetTemplateBorder(Button button, string name)
    {
        return button.GetVisualDescendants()
            .OfType<Border>()
            .Should().ContainSingle(x => x.Name == name)
            .Subject;
    }

    private sealed class ThicknessTestControl : Control
    {
        public static readonly StyledProperty<Thickness> ValueProperty =
            AvaloniaProperty.Register<ThicknessTestControl, Thickness>(nameof(Value));

        public Thickness Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
