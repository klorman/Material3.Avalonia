using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Markup;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Markup;

public sealed class TokenExtensionTests
{
    static TokenExtensionTests()
    {
        TestApp.EnsureStarted();
    }

    [Fact]
    public void ProvideValue_ShouldRejectMissingResourceKey()
    {
        var provideValue = () => new TokenExtension().ProvideValue(null!);

        provideValue.Should().Throw<InvalidOperationException>()
            .WithMessage("Token requires a resource key.");
    }

    [Fact]
    public void Token_ShouldLoadFromAxaml()
    {
        var button = AvaloniaXamlLoader.Load(new Uri(
                "avares://Material3.Avalonia.Tests/Markup/TokenExtensionSmoke.axaml"))
            .Should().BeOfType<Button>()
            .Subject;

        button.Tag.Should().Be(42d);
        button.Margin.Should().Be(new Thickness(8));
    }

    [Fact]
    public void Token_ShouldUseTemplatePriorityWhenUsedDirectlyInsideControlTemplate()
    {
        var button = AvaloniaXamlLoader.Load(new Uri(
                "avares://Material3.Avalonia.Tests/Markup/TokenExtensionDirectTemplateSmoke.axaml"))
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

            var token = GetTemplateBorder(button, "PART_Token");
            var native = GetTemplateBorder(button, "PART_Native");
            token.Height.Should().Be(1d);
            native.Height.Should().Be(1d);

            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            button.IsFocused.Should().BeTrue();
            token.Height.Should().Be(3d);
            native.Height.Should().Be(4d);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Token_ShouldUpdateWhenLeafResourceChanges()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        control.Resources["Sys"] = new TokenAlias("Ref");
        control.Resources["Ref"] = 10d;

        BindToken(control, "Comp");

        control.Value.Should().Be(10d);

        control.Resources["Ref"] = 20d;

        control.Value.Should().Be(20d);
    }

    [Fact]
    public void Token_ShouldResolveNestedDynamicResourceKeyAliasAndLeafReplacement()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        control.Resources["Sys"] = new TokenAlias("Ref");
        control.Resources["Ref"] = 10d;

        BindToken(control, new DynamicResourceExtension("Comp"));

        control.Value.Should().Be(10d);

        control.Resources["Ref"] = 20d;

        control.Value.Should().Be(20d);
    }

    [Fact]
    public void Token_ShouldUpdateWhenRootAliasChanges()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        control.Resources["Sys"] = 10d;
        control.Resources["Alt"] = 20d;

        BindToken(control, "Comp");

        control.Value.Should().Be(10d);

        control.Resources["Comp"] = new TokenAlias("Alt");

        control.Value.Should().Be(20d);
    }

    [Fact]
    public void Token_ShouldResolveEachAliasFromOriginalTargetScope()
    {
        var root = new Grid();
        root.Resources["Comp"] = new TokenAlias("Sys");
        root.Resources["Sys"] = new TokenAlias("Ref");
        root.Resources["Ref"] = 10d;

        var local = new Grid();
        local.Resources["Sys"] = new TokenAlias("LocalRef");
        local.Resources["LocalRef"] = 30d;

        var control = new TokenTestControl();
        root.Children.Add(local);
        local.Children.Add(control);

        BindToken(control, "Comp");

        control.Value.Should().Be(30d);

        local.Resources["Sys"] = 20d;

        control.Value.Should().Be(20d);
    }

    [Fact]
    public void Token_ShouldResolveTemplateChildAliasesFromTemplatedControlResources()
    {
        var theme = new ControlTheme(typeof(Button))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((_, _) =>
                {
                    var child = new TokenTestControl();
                    BindToken(child, "Comp");

                    return child;
                }))
            }
        };
        var button = new Button
        {
            Theme = theme
        };
        button.Resources["Comp"] = new TokenAlias("Sys");
        button.Resources["Sys"] = 40d;

        button.ApplyStyling();
        button.ApplyTemplate();

        var child = button.GetVisualDescendants()
            .OfType<TokenTestControl>()
            .Should().ContainSingle()
            .Subject;

        child.Value.Should().Be(40d);
    }

    [Fact]
    public void Token_ShouldResolveAliasHopsFromActualThemeVariant()
    {
        var control = new TokenTestControl();
        var window = new Window
        {
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = control
        };
        control.Resources["Comp"] = new TokenAlias("Sys");

        var lightResources = new ResourceDictionary();
        lightResources["Sys"] = 10d;
        var darkResources = new ResourceDictionary();
        darkResources["Sys"] = 20d;
        control.Resources.ThemeDictionaries[ThemeVariant.Light] = lightResources;
        control.Resources.ThemeDictionaries[ThemeVariant.Dark] = darkResources;
        window.ApplyStyling();

        BindToken(control, "Comp");

        control.Value.Should().Be(20d);
    }

    [Fact]
    public void Token_ShouldNotThrowWhenTargetIsDetachedAndReparented()
    {
        var host = new Grid();
        host.Resources["Comp"] = new TokenAlias("Sys");
        host.Resources["Sys"] = 10d;
        var control = new TokenTestControl();
        host.Children.Add(control);
        var window = new Window
        {
            Content = host
        };

        try
        {
            window.Show();
            BindToken(control, "Comp");

            control.Value.Should().Be(10d);

            var detachAndReparent = () =>
            {
                host.Children.Remove(control);
                host.Resources["Sys"] = 20d;
                host.Children.Add(control);
            };

            detachAndReparent.Should().NotThrow();
            control.Value.Should().Be(20d);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Convert_ShouldReturnBindingErrorForAliasCycleWithPath()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        control.Resources["Sys"] = new TokenAlias("Comp");
        var binding = GetTokenBinding("Comp");

        var value = ConvertToken(
            binding,
            typeof(double),
            control.Resources["Comp"],
            control);

        var error = GetTokenError(value).Should().BeOfType<TokenResolutionException>().Subject;
        error.Message.Should().Be("Token alias cycle detected: Comp -> Sys -> Comp.");
        error.Path.Should().Equal("Comp", "Sys", "Comp");
    }

    [Fact]
    public void Token_ShouldResolveLongAliasChain()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Alias1");
        control.Resources["Alias1"] = new TokenAlias("Alias2");
        control.Resources["Alias2"] = new TokenAlias("Alias3");
        control.Resources["Alias3"] = new TokenAlias("Alias4");
        control.Resources["Alias4"] = new TokenAlias("Leaf");
        control.Resources["Leaf"] = 10d;

        BindToken(control, "Comp");

        control.Value.Should().Be(10d);
    }

    [Fact]
    public void Convert_ShouldReturnBindingErrorForMissingAliasResourceWithPath()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        var binding = GetTokenBinding("Comp");
        var window = new Window
        {
            Content = control
        };

        try
        {
            window.Show();
            var value = ConvertToken(
                binding,
                typeof(double),
                control.Resources["Comp"],
                control);

            var error = GetTokenError(value).Should().BeOfType<TokenResolutionException>().Subject;
            error.Message.Should().Be("Token 'Comp -> Sys' could not be resolved. Resource 'Sys' was not found.");
            error.Path.Should().Equal("Comp", "Sys");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Convert_ShouldReturnBindingErrorForNullAliasResourceKeyWithPath()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias();
        var binding = GetTokenBinding("Comp");

        var value = ConvertToken(
            binding,
            typeof(double),
            control.Resources["Comp"],
            control);

        var error = GetTokenError(value).Should().BeOfType<TokenResolutionException>().Subject;
        error.Message.Should().Be("Token 'Comp' aliases a null resource key.");
        error.Path.Should().Equal("Comp");
    }

    [Fact]
    public void Convert_ShouldReturnBindingErrorForTypeMismatchWithPath()
    {
        var control = new TokenTestControl();
        control.Resources["Comp"] = new TokenAlias("Sys");
        control.Resources["Sys"] = "not a number";
        var binding = GetTokenBinding("Comp");

        var value = ConvertToken(
            binding,
            typeof(double),
            control.Resources["Comp"],
            control);

        var error = GetTokenError(value).Should().BeOfType<TokenResolutionException>().Subject;
        error.Message.Should()
            .Be("Token 'Comp -> Sys' resolved to 'System.String', which cannot be assigned to 'System.Double'.");
        error.Path.Should().Equal("Comp", "Sys");
    }

    private static void BindToken(TokenTestControl control, object resourceKey)
    {
        var binding = GetTokenBinding(resourceKey);

        control.Bind(TokenTestControl.ValueProperty, binding);
    }

    private static MultiBinding GetTokenBinding(object resourceKey)
    {
        return new TokenExtension(resourceKey).ProvideValue(null!)
            .Should().BeAssignableTo<MultiBinding>()
            .Subject;
    }

    private static object? ConvertToken(MultiBinding binding, Type targetType, params object?[] values)
    {
        binding.Converter.Should().NotBeNull();

        return binding.Converter!.Convert(
            values,
            targetType,
            binding.ConverterParameter,
            CultureInfo.InvariantCulture);
    }

    private static Exception? GetTokenError(object? value)
    {
        var notification = value.Should().BeOfType<BindingNotification>().Subject;
        notification.ErrorType.Should().Be(BindingErrorType.Error);

        return notification.Error;
    }

    private static Border GetTemplateBorder(Button button, string name)
    {
        return button.GetVisualDescendants()
            .OfType<Border>()
            .Should().ContainSingle(x => x.Name == name)
            .Subject;
    }

    private sealed class TokenTestControl : Control
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<TokenTestControl, double>(nameof(Value));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
