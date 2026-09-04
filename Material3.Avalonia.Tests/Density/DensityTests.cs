using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Density;
using Material3.Avalonia.Markup;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Density;

public sealed class DensityTests
{
    static DensityTests()
    {
        TestApp.EnsureStarted();
    }

    [Fact]
    public void Density_ShouldInheritFromParentElement()
    {
        var panel = new StackPanel();
        var button = new Button();

        DensityAssist.SetDensity(panel, MaterialDensity.Dense2);
        panel.Children.Add(button);

        DensityAssist.GetDensity(button).Should().Be(MaterialDensity.Dense2);
    }

    [Fact]
    public void Density_ShouldAllowLocalOverride()
    {
        var panel = new StackPanel();
        var button = new Button();

        DensityAssist.SetDensity(panel, MaterialDensity.Dense2);
        DensityAssist.SetDensity(button, MaterialDensity.Dense1);
        panel.Children.Add(button);

        DensityAssist.GetDensity(button).Should().Be(MaterialDensity.Dense1);
    }

    [Theory]
    [InlineData(MaterialDensity.Default, 40)]
    [InlineData(MaterialDensity.Dense1, 36)]
    [InlineData(MaterialDensity.Dense2, 32)]
    [InlineData(MaterialDensity.Dense3, 28)]
    [InlineData(MaterialDensity.Dense4, 28)]
    [InlineData(MaterialDensity.Dense5, 28)]
    public void DensityScalar_ShouldApplyDensityAndClampToMostDense(MaterialDensity density, double expected)
    {
        var binding = GetScalarBinding(MaterialDensity.Dense3);

        var result = Convert(binding, 40d, density);

        result.Should().Be(expected);
    }

    [Fact]
    public void DensityScalar_ShouldClampFinalValueToZero()
    {
        var binding = GetScalarBinding(MaterialDensity.Dense3);

        var result = Convert(binding, 8d, MaterialDensity.Dense3);

        result.Should().Be(0d);
    }

    [Fact]
    public void DensityScalar_ShouldApplyDeltaScale()
    {
        var binding = GetScalarBinding(MaterialDensity.Dense4, deltaScale: 0.5d);

        Convert(binding, 16d, MaterialDensity.Default).Should().Be(16d);
        Convert(binding, 16d, MaterialDensity.Dense2).Should().Be(12d);
        Convert(binding, 16d, MaterialDensity.Dense4).Should().Be(8d);
        Convert(binding, 8d, MaterialDensity.Dense4).Should().Be(0d);
    }

    [Theory]
    [InlineData(32, MaterialDensity.Dense2, 24)]
    [InlineData(40, MaterialDensity.Dense3, 28)]
    [InlineData(56, MaterialDensity.Dense3, 44)]
    [InlineData(96, MaterialDensity.Dense3, 84)]
    [InlineData(136, MaterialDensity.Dense3, 124)]
    public void DensityScalar_ShouldSupportButtonDensityFloors(
        double baseHeight,
        MaterialDensity mostDense,
        double expectedDense5Height)
    {
        var binding = GetScalarBinding(mostDense);

        Convert(binding, baseHeight, MaterialDensity.Dense5).Should().Be(expectedDense5Height);
    }

    [Fact]
    public void DensityScalar_ShouldRejectResourceKeyAndBaseTogether()
    {
        var extension = new DensityScalarExtension("BaseScalar")
        {
            Base = new Binding(nameof(DensityTestControl.BaseScalar)),
            MostDense = MaterialDensity.Dense3
        };

        var provideValue = () => extension.ProvideValue(null!);

        provideValue.Should().Throw<InvalidOperationException>()
            .WithMessage("DensityScalar requires either ResourceKey or Base, not both.");
    }

    [Fact]
    public void DensityThickness_ShouldApplyVerticalDensityToTopAndBottom()
    {
        var binding = GetThicknessBinding(DensitySides.Vertical, MaterialDensity.Dense3);

        Convert(binding, new Thickness(16, 12, 16, 12), MaterialDensity.Default)
            .Should().Be(new Thickness(16, 12, 16, 12));
        Convert(binding, new Thickness(16, 12, 16, 12), MaterialDensity.Dense1)
            .Should().Be(new Thickness(16, 10, 16, 10));
        Convert(binding, new Thickness(16, 12, 16, 12), MaterialDensity.Dense2)
            .Should().Be(new Thickness(16, 8, 16, 8));
        Convert(binding, new Thickness(16, 12, 16, 12), MaterialDensity.Dense3)
            .Should().Be(new Thickness(16, 6, 16, 6));
        Convert(binding, new Thickness(16, 12, 16, 12), MaterialDensity.Dense4)
            .Should().Be(new Thickness(16, 6, 16, 6));
    }

    [Fact]
    public void DensityThickness_ShouldApplyBottomDensityToBottomOnly()
    {
        var binding = GetThicknessBinding(DensitySides.Bottom, MaterialDensity.Dense2);

        Convert(binding, new Thickness(4, 8, 12, 16), MaterialDensity.Default)
            .Should().Be(new Thickness(4, 8, 12, 16));
        Convert(binding, new Thickness(4, 8, 12, 16), MaterialDensity.Dense1)
            .Should().Be(new Thickness(4, 8, 12, 12));
        Convert(binding, new Thickness(4, 8, 12, 16), MaterialDensity.Dense2)
            .Should().Be(new Thickness(4, 8, 12, 8));
    }

    [Fact]
    public void DensityThickness_ShouldRejectResourceKeyAndBaseTogether()
    {
        var extension = new DensityThicknessExtension("BaseThickness")
        {
            Base = new Binding(nameof(DensityTestControl.BaseThickness)),
            MostDense = MaterialDensity.Dense3
        };

        var provideValue = () => extension.ProvideValue(null!);

        provideValue.Should().Throw<InvalidOperationException>()
            .WithMessage("DensityThickness requires either ResourceKey or Base, not both.");
    }

    [Fact]
    public void DensityScalar_ShouldUseExplicitBaseBinding()
    {
        var control = new DensityTestControl
        {
            BaseScalar = 40d
        };
        DensityAssist.SetDensity(control, MaterialDensity.Dense1);

        control.Bind(
            DensityTestControl.ScalarProperty,
            GetScalarBinding(MaterialDensity.Dense3, CreateSelfBinding(nameof(DensityTestControl.BaseScalar))));

        control.Scalar.Should().Be(36d);
    }

    [Fact]
    public void DensityThickness_ShouldUseExplicitBaseBinding()
    {
        var control = new DensityTestControl
        {
            BaseThickness = new Thickness(4, 8, 12, 16)
        };
        DensityAssist.SetDensity(control, MaterialDensity.Dense1);

        control.Bind(
            DensityTestControl.TestThicknessProperty,
            GetThicknessBinding(
                DensitySides.Vertical,
                MaterialDensity.Dense3,
                CreateSelfBinding(nameof(DensityTestControl.BaseThickness))));

        control.TestThickness.Should().Be(new Thickness(4, 6, 12, 14));
    }

    [Fact]
    public void DensityScalar_ShouldResolveResourceKeyAlias()
    {
        var control = new DensityTestControl();
        control.Resources["BaseScalar"] = new TokenAlias("BaseScalarLeaf");
        control.Resources["BaseScalarLeaf"] = 40d;
        DensityAssist.SetDensity(control, MaterialDensity.Dense1);

        BindScalar(control, MaterialDensity.Dense3);

        control.Scalar.Should().Be(36d);
    }

    [Fact]
    public void DensityThickness_ShouldResolveResourceKeyAlias()
    {
        var control = new DensityTestControl();
        control.Resources["BaseThickness"] = new TokenAlias("BaseThicknessLeaf");
        control.Resources["BaseThicknessLeaf"] = new Thickness(4, 8, 12, 16);
        DensityAssist.SetDensity(control, MaterialDensity.Dense1);

        BindThickness(control, DensitySides.Vertical, MaterialDensity.Dense3);

        control.TestThickness.Should().Be(new Thickness(4, 6, 12, 14));
    }

    [Fact]
    public void DensityScalar_ShouldUpdateWhenLeafResourceChanges()
    {
        var control = new DensityTestControl();
        control.Resources["BaseScalar"] = new TokenAlias("BaseScalarSys");
        control.Resources["BaseScalarSys"] = new TokenAlias("BaseScalarLeaf");
        control.Resources["BaseScalarLeaf"] = 40d;

        BindScalar(control, MaterialDensity.Dense3);

        control.Scalar.Should().Be(40d);

        control.Resources["BaseScalarLeaf"] = 56d;

        control.Scalar.Should().Be(56d);
    }

    [Fact]
    public void DensityScalar_ShouldResolveNestedDynamicResourceKeyAliasAndLeafReplacement()
    {
        var control = new DensityTestControl();
        control.Resources["BaseScalar"] = new TokenAlias("BaseScalarSys");
        control.Resources["BaseScalarSys"] = new TokenAlias("BaseScalarLeaf");
        control.Resources["BaseScalarLeaf"] = 40d;

        BindScalar(control, MaterialDensity.Dense3, new DynamicResourceExtension("BaseScalar"));

        control.Scalar.Should().Be(40d);

        control.Resources["BaseScalarLeaf"] = 56d;

        control.Scalar.Should().Be(56d);
    }

    [Fact]
    public void DensityThickness_ShouldResolveNestedDynamicResourceKeyAliasAndLeafReplacement()
    {
        var control = new DensityTestControl();
        control.Resources["BaseThickness"] = new TokenAlias("BaseThicknessSys");
        control.Resources["BaseThicknessSys"] = new TokenAlias("BaseThicknessLeaf");
        control.Resources["BaseThicknessLeaf"] = new Thickness(4, 8, 12, 16);

        BindThickness(
            control,
            DensitySides.Vertical,
            MaterialDensity.Dense3,
            new DynamicResourceExtension("BaseThickness"));

        control.TestThickness.Should().Be(new Thickness(4, 8, 12, 16));

        control.Resources["BaseThicknessLeaf"] = new Thickness(4, 10, 12, 18);

        control.TestThickness.Should().Be(new Thickness(4, 10, 12, 18));
    }

    [Fact]
    public void DensityExtensions_ShouldUseTemplatePriorityWhenUsedDirectlyInsideControlTemplate()
    {
        var button = AvaloniaXamlLoader.Load(new Uri(
                "avares://Material3.Avalonia.Tests/Density/DensityExtensionTemplateSmoke.axaml"))
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

            var scalar = GetTemplateBorder(button, "PART_DensityScalar");
            var thickness = GetTemplateBorder(button, "PART_DensityThickness");
            scalar.Height.Should().Be(10d);
            thickness.Padding.Should().Be(new Thickness(1, 2, 3, 4));

            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            button.IsFocused.Should().BeTrue();
            scalar.Height.Should().Be(30d);
            thickness.Padding.Should().Be(new Thickness(5, 6, 7, 8));
        }
        finally
        {
            window.Close();
        }
    }

    private static MultiBinding GetScalarBinding(
        MaterialDensity mostDense,
        BindingBase? @base = null,
        double deltaScale = 1d,
        object? resourceKey = null)
    {
        var extension = @base is null
            ? new DensityScalarExtension(resourceKey ?? "BaseScalar")
            : new DensityScalarExtension { Base = @base };

        extension.MostDense = mostDense;
        extension.DeltaScale = deltaScale;

        var value = extension.ProvideValue(null!);

        var binding = value.Should().BeOfType<MultiBinding>().Subject;

        return binding;
    }

    private static MultiBinding GetThicknessBinding(
        DensitySides sides,
        MaterialDensity mostDense,
        BindingBase? @base = null,
        object? resourceKey = null)
    {
        var extension = @base is null
            ? new DensityThicknessExtension(resourceKey ?? "BaseThickness")
            : new DensityThicknessExtension { Base = @base };

        extension.Sides = sides;
        extension.MostDense = mostDense;

        var value = extension.ProvideValue(null!);

        var binding = value.Should().BeOfType<MultiBinding>().Subject;

        return binding;
    }

    private static object? Convert(MultiBinding binding, params object?[] values)
    {
        binding.Converter.Should().NotBeNull();

        return binding.Converter!.Convert(
            values,
            typeof(object),
            binding.ConverterParameter,
            CultureInfo.InvariantCulture);
    }

    private static void BindScalar(DensityTestControl control, MaterialDensity mostDense, object? resourceKey = null)
    {
        control.Bind(DensityTestControl.ScalarProperty, GetScalarBinding(mostDense, resourceKey: resourceKey));
    }

    private static void BindThickness(
        DensityTestControl control,
        DensitySides sides,
        MaterialDensity mostDense,
        object? resourceKey = null)
    {
        control.Bind(DensityTestControl.TestThicknessProperty, GetThicknessBinding(sides, mostDense, resourceKey: resourceKey));
    }

    private static Binding CreateSelfBinding(string path)
    {
        return new Binding(path)
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.Self)
        };
    }

    private static Border GetTemplateBorder(Button button, string name)
    {
        return button.GetVisualDescendants()
            .OfType<Border>()
            .Should().ContainSingle(x => x.Name == name)
            .Subject;
    }

    private sealed class DensityTestControl : Control
    {
        public static readonly StyledProperty<double> BaseScalarProperty =
            AvaloniaProperty.Register<DensityTestControl, double>(nameof(BaseScalar));

        public static readonly StyledProperty<double> ScalarProperty =
            AvaloniaProperty.Register<DensityTestControl, double>(nameof(Scalar));

        public static readonly StyledProperty<Thickness> BaseThicknessProperty =
            AvaloniaProperty.Register<DensityTestControl, Thickness>(nameof(BaseThickness));

        public static readonly StyledProperty<Thickness> TestThicknessProperty =
            AvaloniaProperty.Register<DensityTestControl, Thickness>(nameof(TestThickness));

        public double BaseScalar
        {
            get => GetValue(BaseScalarProperty);
            set => SetValue(BaseScalarProperty, value);
        }

        public double Scalar
        {
            get => GetValue(ScalarProperty);
            set => SetValue(ScalarProperty, value);
        }

        public Thickness BaseThickness
        {
            get => GetValue(BaseThicknessProperty);
            set => SetValue(BaseThicknessProperty, value);
        }

        public Thickness TestThickness
        {
            get => GetValue(TestThicknessProperty);
            set => SetValue(TestThicknessProperty, value);
        }
    }
}
