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
using Material3.Avalonia.Attached;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Density;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class IconButtonThemeTests
{
    static IconButtonThemeTests() => TestApp.EnsureStarted();

    [Fact]
    public void DefaultsUseStandardSmallRoundAndDefaultWidth()
    {
        var button = new IconButton();
        var toggle = new IconToggleButton();

        button.Variant.Should().Be(IconButtonVariant.Standard);
        button.Size.Should().Be(ButtonSize.Small);
        button.Shape.Should().Be(ButtonShape.Round);
        button.WidthMode.Should().Be(IconButtonWidth.Default);
        toggle.Variant.Should().Be(IconButtonVariant.Standard);
        toggle.Size.Should().Be(ButtonSize.Small);
        toggle.Shape.Should().Be(ButtonShape.Round);
        toggle.WidthMode.Should().Be(IconButtonWidth.Default);
    }

    [Theory]
    [InlineData(IconButtonVariant.Standard, "MdCompIconButtonStandardContainerBrush")]
    [InlineData(IconButtonVariant.Filled, "MdCompIconButtonFilledContainerBrush")]
    [InlineData(IconButtonVariant.Tonal, "MdCompIconButtonTonalContainerBrush")]
    [InlineData(IconButtonVariant.Outlined, "MdCompIconButtonOutlinedContainerBrush")]
    public void VariantUsesItsContainerAndIconTokens(IconButtonVariant variant, string containerKey)
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.Variant = variant;
        button.Resources[containerKey] = Brushes.Magenta;
        button.Resources[$"MdCompIconButton{variant}IconBrush"] = Brushes.Cyan;
        button.ApplyStyling();
        button.ApplyTemplate();
        var icon = GetIcon(button);
        icon.ApplyStyling();

        button.Background.Should().BeSameAs(Brushes.Magenta);
        icon.Foreground.Should().BeSameAs(Brushes.Cyan);
    }

    [Theory]
    [InlineData(ButtonSize.ExtraSmall, 32, 20, 1)]
    [InlineData(ButtonSize.Small, 40, 24, 1)]
    [InlineData(ButtonSize.Medium, 56, 24, 1)]
    [InlineData(ButtonSize.Large, 96, 32, 2)]
    [InlineData(ButtonSize.ExtraLarge, 136, 40, 3)]
    public void SizeControlsContainerIconAndOutline(ButtonSize size, double height, double iconSize, double outline)
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.Variant = IconButtonVariant.Outlined;
        button.Size = size;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var icon = GetIcon(button);

            button.Bounds.Height.Should().BeApproximately(height, 1);
            icon.Size.Should().Be(iconSize);
            button.BorderThickness.Should().Be(new Thickness(outline));
        }
        finally
        {
            window.Close();
        }
    }

    [Theory]
    [InlineData(IconButtonWidth.Narrow, 32)]
    [InlineData(IconButtonWidth.Default, 40)]
    [InlineData(IconButtonWidth.Wide, 52)]
    public void WidthModeProducesIconAndHorizontalSpacing(IconButtonWidth width, double expectedWidth)
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.WidthMode = width;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            button.Bounds.Width.Should().BeApproximately(expectedWidth, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void IconButtonKeepsContainerVisualsAtFullBounds()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();

            var background = button.GetVisualDescendants()
                .OfType<Border>()
                .Single(x => x.Name == "PART_Background");

            background.Bounds.Width.Should().BeApproximately(button.Bounds.Width, 1);
            background.Bounds.Height.Should().BeApproximately(button.Bounds.Height, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void IconButtonAnimatesContentWidthWhenWidthModeChanges()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.ApplyStyling();
        button.ApplyTemplate();

        var content = GetContent(button);
        content.Transitions!.Any(transition =>
            transition is SpringDoubleTransition spring &&
            spring.Property?.Name == "AnimatedWidth").Should().BeTrue();
        content.Transitions!.Any(transition =>
            transition is SpringDoubleTransition spring &&
            (spring.Property?.Name == nameof(ButtonContentMotion.LeadingSpace) ||
             spring.Property?.Name == nameof(ButtonContentMotion.TrailingSpace))).Should().BeFalse();
        button.Transitions!.Any(transition =>
            transition is SpringThicknessTransition thickness &&
            thickness.Property?.Name == nameof(TemplatedControl.Padding)).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CenteredIconControlKeepsIconCenterStableWhileWidthModeAnimates(bool toggle)
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var oldScheme = MotionSettings.GlobalScheme;
        var resources = LoadResources();
        Button button = toggle ? CreateToggle(resources) : CreateButton(resources);
        if (button is IconButton iconButton)
            iconButton.Size = ButtonSize.Large;
        else
            ((IconToggleButton)button).Size = ButtonSize.Large;
        button.HorizontalAlignment = HorizontalAlignment.Center;
        button.UseLayoutRounding = true;
        var host = new StackPanel
        {
            Width = 240,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { button }
        };
        var window = new Window { Width = 800, Height = 200, Content = host };
        try
        {
            MotionSettings.ReduceMotion = false;
            MotionSettings.GlobalScheme = MotionScheme.Expressive;
            window.Show();
            window.UpdateLayout();
            var icon = GetIcon(button);
            var initialCenter = GetCenterX(icon, window);
            var widest = button.Bounds.Width;
            var narrowest = button.Bounds.Width;
            var largestDrift = 0d;

            SetWidthMode(button, IconButtonWidth.Wide);
            SampleWidthAnimation(window, button, icon, initialCenter, 96.1, 127.9,
                ref widest, ref narrowest, ref largestDrift);
            SetWidthMode(button, IconButtonWidth.Narrow);
            var sawIntermediateNarrow = SampleWidthAnimation(window, button, icon, initialCenter, 64.1, 95.9,
                ref widest, ref narrowest, ref largestDrift);

            sawIntermediateNarrow.Should().BeTrue();
            widest.Should().Be(128);
            narrowest.Should().BeLessThan(63.9);
            largestDrift.Should().BeLessThan(.01);
        }
        finally
        {
            window.Close();
            MotionSettings.GlobalScheme = oldScheme;
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, 20)]
    [InlineData(HorizontalAlignment.Center, 100)]
    [InlineData(HorizontalAlignment.Right, 180)]
    [InlineData(HorizontalAlignment.Stretch, 100)]
    public void IconOnlyContentPreservesHorizontalContentAlignment(HorizontalAlignment alignment,
        double expectedIconCenter)
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.MinWidth = 200;
        button.HorizontalContentAlignment = alignment;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();

            GetCenterX(GetIcon(button), button).Should().BeApproximately(expectedIconCenter, .01);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void IconOnlyWidthModeSnapsWhenMotionIsReduced()
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.Size = ButtonSize.Large;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            MotionSettings.ReduceMotion = true;
            window.Show();
            window.UpdateLayout();

            button.WidthMode = IconButtonWidth.Wide;
            window.UpdateLayout();

            button.Bounds.Width.Should().BeApproximately(128, .01);
            GetCenterX(GetIcon(button), button).Should().BeApproximately(64, .01);
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Fact]
    public void IconOnlyContentUsesButtonBoundsAsItsAlignmentSpace()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.MinWidth = 200;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();

            var content = GetContent(button);
            content.Bounds.Width.Should().BeApproximately(button.Bounds.Width, .01);
            GetCenterX(GetIcon(button), button).Should().BeApproximately(button.Bounds.Width / 2, .01);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void IconToggleButtonAnimatesContentWidthWhenWidthModeChanges()
    {
        var resources = LoadResources();
        var toggle = CreateToggle(resources);
        toggle.ApplyStyling();
        toggle.ApplyTemplate();

        var content = GetContent(toggle);
        content.Transitions!.Any(transition =>
            transition is SpringDoubleTransition spring &&
            spring.Property?.Name == "AnimatedWidth").Should().BeTrue();
        content.Transitions!.Any(transition =>
            transition is SpringDoubleTransition spring &&
            (spring.Property?.Name == nameof(ButtonContentMotion.LeadingSpace) ||
             spring.Property?.Name == nameof(ButtonContentMotion.TrailingSpace))).Should().BeFalse();
        toggle.Transitions!.Any(transition =>
            transition is SpringThicknessTransition thickness &&
            thickness.Property?.Name == nameof(TemplatedControl.Padding)).Should().BeFalse();
    }

    [Theory]
    [InlineData(IconButtonWidth.Narrow, 32)]
    [InlineData(IconButtonWidth.Default, 40)]
    [InlineData(IconButtonWidth.Wide, 52)]
    public void ToggleWidthModeProducesIconAndHorizontalSpacing(IconButtonWidth width, double expectedWidth)
    {
        var resources = LoadResources();
        var toggle = CreateToggle(resources);
        toggle.WidthMode = width;
        var window = new Window { Width = 400, Height = 200, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            toggle.Bounds.Width.Should().BeApproximately(expectedWidth, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void IconButtonClassesUseIconButtonProperties()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.Classes.Add("md");
        button.Classes.Add("square");
        button.Classes.Add("filled");
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();

            button.Size.Should().Be(ButtonSize.Medium);
            button.Shape.Should().Be(ButtonShape.Square);
            button.Variant.Should().Be(IconButtonVariant.Filled);
            button.Bounds.Height.Should().BeApproximately(56, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void FilledUnselectedToggleHoverUsesUnselectedIconToken()
    {
        var resources = LoadResources();
        var toggle = CreateToggle(resources);
        toggle.Variant = IconButtonVariant.Filled;
        toggle.IsChecked = false;
        toggle.Resources["MdCompIconButtonFilledUnselectedContainerBrush"] = Brushes.Green;
        toggle.Resources["MdCompIconButtonFilledUnselectedIconBrush"] = Brushes.Blue;
        toggle.Resources["MdCompIconButtonFilledUnselectedHoveredIconBrush"] = Brushes.Magenta;
        toggle.ApplyStyling();
        toggle.ApplyTemplate();
        toggle.Classes.Add("m3-hovered");

        toggle.Background.Should().BeSameAs(Brushes.Green);
        var icon = GetIcon(toggle);
        icon.ApplyStyling();
        icon.Foreground.Should().BeSameAs(Brushes.Magenta);
    }

    [Theory]
    [InlineData(ButtonShape.Round, 20)]
    [InlineData(ButtonShape.Square, 12)]
    public void ShapeChangesContainerCorners(ButtonShape shape, double expectedCorner)
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.Shape = shape;
        var window = new Window { Width = 400, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            PumpUntil(() => Math.Abs(button.CornerRadius.TopLeft - expectedCorner) < 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ToggleUsesContentAndFillsMaterialSymbolWhenChecked()
    {
        var resources = LoadResources();
        var toggle = CreateToggle(resources);
        toggle.Content = MaterialSymbol.Favorite;
        toggle.ApplyStyling();
        toggle.ApplyTemplate();
        var icon = GetIcon(toggle);
        icon.ApplyStyling();

        icon.Value.Should().Be(MaterialSymbol.Favorite);
        icon.IsFilled.Should().BeFalse();

        toggle.IsChecked = true;
        icon.ApplyStyling();
        icon.Value.Should().Be(MaterialSymbol.Favorite);
        icon.IsFilled.Should().BeTrue();
    }

    [Fact]
    public void ToggleSelectedShapeIsOverriddenWhilePressed()
    {
        var resources = LoadResources();
        var toggle = CreateToggle(resources);
        toggle.Size = ButtonSize.Small;
        toggle.IsChecked = true;
        var window = new Window { Width = 400, Height = 200, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            toggle.CornerRadius.TopLeft.Should().Be(12);
            var point = toggle.TranslatePoint(
                new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2),
                window);
            point.Should().NotBeNull();

            window.MouseDown(point!.Value, MouseButton.Left);
            toggle.IsPressed.Should().BeTrue();
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 8) < 1);
            window.MouseUp(point.Value, MouseButton.Left);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void DisabledStateUsesDisabledIconTokenAndOpacity()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.IsEnabled = false;
        button.Resources["MdCompIconButtonDisabledIconBrush"] = Brushes.Magenta;
        button.ApplyStyling();
        button.ApplyTemplate();
        var icon = GetIcon(button);
        icon.ApplyStyling();

        icon.Foreground.Should().BeSameAs(Brushes.Magenta);
        icon.Opacity.Should().BeApproximately(.38, .01);
    }

    [Fact]
    public void FocusRingUsesSharedAdornerConfiguration()
    {
        var resources = LoadResources();
        var button = CreateButton(resources);
        button.ApplyStyling();

        FocusRing.GetThickness(button).Should().Be(new Thickness(3));
        FocusRing.GetOffset(button).Should().Be(new Thickness(2));
        FocusRing.GetBrush(button).Should().NotBeNull();
    }

    [Fact]
    public void IconButtonBlocksInheritedDensityButHonorsLocalDensity()
    {
        var parent = new StackPanel();
        DensityAssist.SetDensity(parent, MaterialDensity.Dense2);
        var inheritedResources = LoadResources();
        var localResources = LoadResources();
        var inherited = new IconButton
            { Theme = GetTheme(inheritedResources, typeof(IconButton)), Content = MaterialSymbol.Search };
        var local = new IconButton
            { Theme = GetTheme(localResources, typeof(IconButton)), Content = MaterialSymbol.Search };
        inherited.Resources.MergedDictionaries.Add(inheritedResources);
        local.Resources.MergedDictionaries.Add(localResources);
        DensityAssist.SetDensity(local, MaterialDensity.Dense2);
        parent.Children.Add(inherited);
        parent.Children.Add(local);
        var window = new Window { Width = 400, Height = 200, Content = parent };
        try
        {
            window.Show();
            window.UpdateLayout();

            inherited.Bounds.Height.Should().BeApproximately(40, 1);
            local.Bounds.Height.Should().BeApproximately(32, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void AssistClassesRejectIconControlsButAllowOrdinaryDescendants()
    {
        var button = new ButtonSubclass();
        var toggle = new ToggleButtonSubclass();
        var iconButton = new IconButton();
        var iconToggle = new IconToggleButton();

        ButtonAssist.SetVariant(button, ButtonVariant.Text);
        ToggleButtonAssist.SetVariant(toggle, ToggleButtonVariant.Outlined);

        Action setButtonAssistOnIcon = () => ButtonAssist.SetVariant(iconButton, ButtonVariant.Filled);
        Action setButtonAssistOnIconToggle = () => ButtonAssist.SetVariant(iconToggle, ButtonVariant.Filled);
        Action setToggleAssistOnIcon = () => ToggleButtonAssist.SetVariant(iconToggle, ToggleButtonVariant.Filled);

        setButtonAssistOnIcon.Should().Throw<ArgumentException>().WithMessage("*ButtonAssist*IconButton*");
        setButtonAssistOnIconToggle.Should().Throw<ArgumentException>().WithMessage("*ButtonAssist*IconToggleButton*");
        setToggleAssistOnIcon.Should().Throw<ArgumentException>().WithMessage("*ToggleButtonAssist*IconToggleButton*");
        Enum.GetNames<ToggleButtonVariant>().Should().NotContain("Text");
    }

    private static IconButton CreateButton(IResourceDictionary resources)
    {
        var button = new IconButton
        {
            Theme = GetTheme(resources, typeof(IconButton)),
            Content = MaterialSymbol.Search
        };
        button.Resources.MergedDictionaries.Add(resources);
        return button;
    }

    private static IconToggleButton CreateToggle(IResourceDictionary resources)
    {
        var toggle = new IconToggleButton
        {
            Theme = GetTheme(resources, typeof(IconToggleButton)),
            Content = MaterialSymbol.Favorite
        };
        toggle.Resources.MergedDictionaries.Add(resources);
        return toggle;
    }

    private static IconPresenter GetIcon(Button button) => button.GetVisualDescendants()
        .OfType<IconPresenter>().Single(x => x.Name == "PART_Icon");

    private static double GetCenterX(Control control, Visual relativeTo)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), relativeTo);
        center.Should().NotBeNull();
        return center!.Value.X;
    }

    private static void SetWidthMode(Button button, IconButtonWidth widthMode)
    {
        if (button is IconButton iconButton)
            iconButton.WidthMode = widthMode;
        else
            ((IconToggleButton)button).WidthMode = widthMode;
    }

    private static bool SampleWidthAnimation(Window window, Button button, IconPresenter icon, double initialCenter,
        double intermediateMinimum, double intermediateMaximum, ref double widest, ref double narrowest,
        ref double largestDrift)
    {
        var sawIntermediate = false;
        for (var i = 0; i < 120; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            widest = Math.Max(widest, button.Bounds.Width);
            narrowest = Math.Min(narrowest, button.Bounds.Width);
            sawIntermediate |= button.Bounds.Width > intermediateMinimum &&
                               button.Bounds.Width < intermediateMaximum;
            largestDrift = Math.Max(largestDrift, Math.Abs(GetCenterX(icon, window) - initialCenter));
            Thread.Sleep(5);
        }

        return sawIntermediate;
    }

    private static ButtonContentMotion GetContent(Button button) => button.GetVisualDescendants()
        .OfType<ButtonContentMotion>().Single(x => x.Name == "PART_Content");

    private static IResourceDictionary LoadResources() =>
        (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"))!;

    private static ControlTheme GetTheme(IResourceDictionary resources, Type type)
    {
        resources.TryGetResource(type, null, out var value).Should().BeTrue();
        return value.Should().BeOfType<ControlTheme>().Subject;
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        condition().Should().BeTrue();
    }

    private sealed class ButtonSubclass : Button
    {
    }

    private sealed class ToggleButtonSubclass : ToggleButton
    {
    }
}