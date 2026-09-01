using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;

namespace Material3.Avalonia.Tests.Controls;

public sealed class InfrastructureControlThemeTests
{
    static InfrastructureControlThemeTests()
    {
        AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
    }

    [Fact]
    public void ItemsControlTheme_ShouldForwardContainerProperties()
    {
        var resources = LoadMaterialThemeResources();
        var itemsControl = new ItemsControl
        {
            Theme = GetResource<ControlTheme>(resources, typeof(ItemsControl)),
            Background = Brushes.Red,
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(1, 2, 3, 4),
            CornerRadius = new CornerRadius(5, 6, 7, 8),
            Padding = new Thickness(9, 10, 11, 12)
        };

        itemsControl.ApplyStyling();
        itemsControl.ApplyTemplate();

        var border = itemsControl.GetVisualDescendants()
            .OfType<Border>()
            .Should().ContainSingle()
            .Subject;

        border.Background.Should().BeSameAs(itemsControl.Background);
        border.BorderBrush.Should().BeSameAs(itemsControl.BorderBrush);
        border.BorderThickness.Should().Be(itemsControl.BorderThickness);
        border.CornerRadius.Should().Be(itemsControl.CornerRadius);
        border.Padding.Should().Be(itemsControl.Padding);
    }

    [Fact]
    public void SelectableTextBlockTheme_ShouldProvideContextFlyoutOnlyWhenEnabled()
    {
        var resources = LoadMaterialThemeResources();
        var theme = GetResource<ControlTheme>(resources, typeof(SelectableTextBlock));
        var enabledTextBlock = new SelectableTextBlock
        {
            Theme = theme
        };
        var disabledTextBlock = new SelectableTextBlock
        {
            IsEnabled = false,
            Theme = theme
        };

        enabledTextBlock.ApplyStyling();
        disabledTextBlock.ApplyStyling();

        enabledTextBlock.ContextFlyout.Should().BeOfType<MenuFlyout>();
        disabledTextBlock.ContextFlyout.Should().BeNull();
    }

    [Theory]
    [InlineData(ButtonSize.ExtraSmall, 20d)]
    [InlineData(ButtonSize.Small, 20d)]
    [InlineData(ButtonSize.Medium, 24d)]
    [InlineData(ButtonSize.Large, 32d)]
    [InlineData(ButtonSize.ExtraLarge, 40d)]
    public void ButtonTheme_ShouldApplyLabelLineHeightToContentPresenter(ButtonSize size, double expectedLineHeight)
    {
        var resources = LoadMaterialThemeResources();
        var button = new Button
        {
            Theme = GetResource<ControlTheme>(resources, typeof(Button)),
            Content = "Label"
        };
        button.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetSize(button, size);

        button.ApplyStyling();
        button.ApplyTemplate();

        var presenter = button.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Should().ContainSingle(x => x.Name == "PART_ContentPresenter")
            .Subject;
        presenter.ApplyStyling();
        presenter.UpdateChild();

        var textBlock = presenter.Child.Should().BeOfType<TextBlock>().Subject;

        presenter.LineHeight.Should().Be(expectedLineHeight);
        textBlock.LineHeight.Should().Be(expectedLineHeight);
    }

    private static IResourceDictionary LoadMaterialThemeResources()
    {
        return AvaloniaXamlLoader.Load(new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"))
            .Should().BeAssignableTo<IResourceDictionary>()
            .Subject;
    }

    private static TResource GetResource<TResource>(IResourceDictionary resources, object key)
    {
        resources.TryGetResource(key, null, out var resource)
            .Should().BeTrue();

        return resource.Should().BeOfType<TResource>().Subject;
    }
}
