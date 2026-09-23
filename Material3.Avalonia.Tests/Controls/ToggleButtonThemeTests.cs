using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Material3.Avalonia.Motion;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class ToggleButtonThemeTests
{
    static ToggleButtonThemeTests() => TestApp.EnsureStarted();

    [Fact]
    public void FilledToggleUsesSelectedColorsAndIndependentIconToken()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        toggle.IsChecked = true;
        toggle.Resources["MdCompButtonFilledIconToggleSelectedBrush"] = Brushes.Magenta;
        toggle.ApplyStyling();
        toggle.ApplyTemplate();

        var icon = GetIcon(toggle);
        icon.ApplyStyling();
        toggle.Foreground.Should().NotBeSameAs(Brushes.Magenta);
        icon.Foreground.Should().BeSameAs(Brushes.Magenta);
    }

    [Fact]
    public void OutlinedToggleRemovesOutlineWhenChecked()
    {
        var toggle = CreateToggle(ButtonVariant.Outlined);
        toggle.ApplyStyling();
        toggle.BorderThickness.Left.Should().Be(1);

        toggle.IsChecked = true;

        toggle.BorderThickness.Should().Be(new Thickness(0));
        toggle.Background.Should().NotBeNull();
    }

    [Fact]
    public void SelectedComponentAliasResolvesLocalOverrideAndReplacement()
    {
        var toggle = CreateToggle(ButtonVariant.Tonal);
        toggle.IsChecked = true;
        toggle.Resources["MdSysSecondaryBrush"] = Brushes.Red;
        toggle.ApplyStyling();
        toggle.Background.Should().BeSameAs(Brushes.Red);

        toggle.Resources["MdSysSecondaryBrush"] = Brushes.Blue;
        toggle.Background.Should().BeSameAs(Brushes.Blue);

        toggle.Resources["MdCompButtonTonalContainerToggleSelectedBrush"] =
            new TokenAlias("MdSysPrimaryBrush");
        toggle.Resources["MdSysPrimaryBrush"] = Brushes.Green;
        toggle.Background.Should().BeSameAs(Brushes.Green);
    }

    [Theory]
    [InlineData(ButtonVariant.Elevated)]
    [InlineData(ButtonVariant.Filled)]
    [InlineData(ButtonVariant.Tonal)]
    [InlineData(ButtonVariant.Outlined)]
    public void ContainerBackgroundHasSingleContinuousTransition(ButtonVariant variant)
    {
        var toggle = CreateToggle(variant);
        var prefix = variant.ToString();
        if (variant != ButtonVariant.Outlined)
            toggle.Resources[$"MdCompButton{prefix}ContainerToggleUnselectedBrush"] = Brushes.Red;
        toggle.Resources[$"MdCompButton{prefix}ContainerToggleSelectedBrush"] = Brushes.Blue;
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            var background = toggle.GetVisualDescendants().OfType<Border>()
                .Single(x => x.Name == "PART_Background");

            VerifyTransition(true);
            VerifyTransition(false);

            void VerifyTransition(bool isChecked)
            {
                toggle.IsChecked = isChecked;
                var alpha = new List<byte>();
                for (var i = 0; i < 50; i++)
                {
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    Dispatcher.UIThread.RunJobs();
                    var ownerColor = ((ISolidColorBrush)toggle.Background!).Color;
                    var surfaceColor = ((ISolidColorBrush)background.Background!).Color;
                    surfaceColor.Should().Be(ownerColor);
                    alpha.Add(ownerColor.A);
                    if (variant != ButtonVariant.Outlined)
                        ownerColor.A.Should().Be(byte.MaxValue);
                    Thread.Sleep(5);
                }

                if (variant == ButtonVariant.Outlined)
                    alpha.Zip(alpha.Skip(1), (previous, current) =>
                            isChecked ? current >= previous : current <= previous)
                        .Should().OnlyContain(value => value);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void TextVariantIsRejectedOnlyForToggleButton()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        var button = new Button();

        ButtonAssist.SetVariant(button, ButtonVariant.Text);
        var setText = () => ButtonAssist.SetVariant(toggle, ButtonVariant.Text);

        setText.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Text variant is not supported*");
    }

    [Fact]
    public void ExtraSmallButtonUsesFourPixelIconGap()
    {
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)),
            Content = "Label"
        };
        button.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetIcon(button, new Border());
        ButtonAssist.SetSize(button, ButtonSize.ExtraSmall);
        var window = new Window { Width = 400, Height = 120, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var icon = button.GetVisualDescendants().OfType<IconPresenter>()
                .Single(x => x.Name == "PART_Icon");
            var slot = button.GetVisualDescendants().OfType<Decorator>()
                .Single(x => x.Name == "PART_IconSlot");
            var content = button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_ContentPresenter");
            var slotX = slot.TranslatePoint(default, button)!.Value.X;
            var contentX = content.TranslatePoint(default, button)!.Value.X;
            (contentX - slotX - icon.Bounds.Width).Should().BeApproximately(4, .01);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ButtonIconTokenChangesOnlyIcon()
    {
        var resources = LoadResources();
        var button = new Button { Theme = GetTheme(resources, typeof(Button)), Content = "Label" };
        button.Resources.MergedDictionaries.Add(resources);
        button.Resources["MdCompButtonFilledIconBrush"] = Brushes.Magenta;
        button.ApplyStyling();
        button.ApplyTemplate();

        var icon = button.GetVisualDescendants().OfType<IconPresenter>().Single();
        icon.ApplyStyling();
        icon.Foreground.Should().BeSameAs(Brushes.Magenta);
        button.Foreground.Should().NotBeSameAs(Brushes.Magenta);
    }

    [Theory]
    [InlineData(ButtonVariant.Elevated)]
    [InlineData(ButtonVariant.Filled)]
    [InlineData(ButtonVariant.Tonal)]
    public void HoverDoesNotChangeButtonElevation(ButtonVariant variant)
    {
        var resources = LoadResources();
        var button = new Button { Theme = GetTheme(resources, typeof(Button)), Content = "Action" };
        button.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetVariant(button, variant);
        var window = new Window { Width = 400, Height = 120, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var outline = button.GetVisualDescendants().OfType<Border>()
                .Single(x => x.Name == "PART_Outline");
            var resting = (BoxShadows)outline.GetValue(Border.BoxShadowProperty)!;

            button.Classes.Add("m3-hovered");
            button.ApplyStyling();
            outline.ApplyStyling();

            ((BoxShadows)outline.GetValue(Border.BoxShadowProperty)!).Should().Be(resting);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void DisabledElevatedButtonUsesLevelZeroElevation()
    {
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)), Content = "Action", IsEnabled = false
        };
        button.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetVariant(button, ButtonVariant.Elevated);
        var window = new Window { Width = 400, Height = 120, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var outline = button.GetVisualDescendants().OfType<Border>()
                .Single(x => x.Name == "PART_Outline");

            ((BoxShadows)outline.GetValue(Border.BoxShadowProperty)!).Count.Should().Be(0);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void CheckedShapeSwapsRoundAndSquareCorners()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        ButtonAssist.SetSize(toggle, ButtonSize.Small);
        ButtonAssist.SetShape(toggle, ButtonShape.Round);
        toggle.IsChecked = true;
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            toggle.CornerRadius.TopLeft.Should().Be(12);

            ButtonAssist.SetShape(toggle, ButtonShape.Square);
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 20) < 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void PressedShapeOverridesSelectedShape()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        ButtonAssist.SetSize(toggle, ButtonSize.Small);
        ButtonAssist.SetShape(toggle, ButtonShape.Square);
        toggle.IsChecked = true;
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 20) < 1);
            var point = toggle.TranslatePoint(new Point(8, 8), window);
            point.Should().NotBeNull();

            window.MouseDown(point!.Value, MouseButton.Left);
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 8) < 1);
            window.MouseUp(point.Value, MouseButton.Left);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void PressedCornerRadiusStaysVisiblyRounded()
    {
        var oldScheme = MotionSettings.GlobalScheme;
        var toggle = CreateToggle(ButtonVariant.Filled);
        ButtonAssist.SetSize(toggle, ButtonSize.Small);
        toggle.IsChecked = true;
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            MotionSettings.GlobalScheme = MotionScheme.Expressive;
            window.Show();
            window.UpdateLayout();
            var point = toggle.TranslatePoint(new Point(8, 8), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            var minimum = toggle.CornerRadius.TopLeft;
            for (var i = 0; i < 70; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                minimum = Math.Min(minimum, toggle.CornerRadius.TopLeft);
                Thread.Sleep(5);
            }

            minimum.Should().BeGreaterThan(4);
            window.MouseUp(point, MouseButton.Left);
        }
        finally
        {
            window.Close();
            MotionSettings.GlobalScheme = oldScheme;
        }
    }

    [Fact]
    public void FullCornerComponentTokenCanBeOverriddenAndRemapped()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        ButtonAssist.SetSize(toggle, ButtonSize.Small);
        toggle.Resources["MdCompButtonSmRoundContainerShape"] = new CornerRadius(7);
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 7) < 1);

            toggle.Resources["CustomRoundShape"] = new CornerRadius(15);
            toggle.Resources["MdCompButtonSmRoundContainerShape"] = new TokenAlias("CustomRoundShape");
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 15) < 1);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void NativeSpaceActivationTogglesCheckedState()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            toggle.Focus();
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            toggle.IsChecked.Should().BeFalse();
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            toggle.IsChecked.Should().BeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    [Theory]
    [InlineData(ButtonVariant.Elevated)]
    [InlineData(ButtonVariant.Filled)]
    [InlineData(ButtonVariant.Tonal)]
    [InlineData(ButtonVariant.Outlined)]
    public void SpaceReleaseAnimatesOverlayColorsWithoutClearingStateLayer(ButtonVariant variant)
    {
        var toggle = CreateToggle(variant);
        var prefix = variant.ToString();
        if (variant != ButtonVariant.Outlined)
            toggle.Resources[$"MdCompButton{prefix}ContainerToggleUnselectedBrush"] = Brushes.White;
        toggle.Resources[$"MdCompButton{prefix}ContainerToggleSelectedBrush"] = Brushes.Black;
        var unselectedPressed = $"MdCompButton{prefix}ToggleUnselectedPressedStateLayerBrush";
        toggle.Resources[unselectedPressed] = Brushes.Red;
        toggle.Resources[$"MdCompButton{prefix}ToggleSelectedPressedStateLayerBrush"] = Brushes.Blue;
        var window = new Window { Width = 400, Height = 120, Content = toggle };
        try
        {
            window.Show();
            window.UpdateLayout();
            toggle.Focus();
            var stateLayer = toggle.GetVisualDescendants().OfType<StateLayer>().Single();
            var ripple = toggle.GetVisualDescendants().OfType<InkRipple>().Single();

            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            PumpFrames(30);
            ColorOf(stateLayer.Brush).Should().Be(Colors.Red);
            ColorOf(ripple.Brush).Should().Be(Colors.Red);
            stateLayer.Opacity.Should().BeApproximately(.1, .01);

            toggle.Resources[unselectedPressed] = Brushes.Green;
            ColorOf(stateLayer.Brush).Should().NotBe(Colors.Green);
            ColorOf(ripple.Brush).Should().NotBe(Colors.Green);
            PumpUntil(() => ColorOf(stateLayer.Brush) == Colors.Green);
            PumpUntil(() => ColorOf(ripple.Brush) == Colors.Green);

            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            toggle.IsChecked.Should().BeTrue();
            ColorOf(toggle.Background).Should().NotBe(Colors.Black);
            ColorOf(stateLayer.Brush).Should().NotBe(Colors.Blue);
            ColorOf(ripple.Brush).Should().NotBe(Colors.Blue);
            stateLayer.Opacity.Should().BeGreaterThan(0);

            PumpUntil(() => ColorOf(toggle.Background) == Colors.Black);
            PumpUntil(() => ColorOf(stateLayer.Brush) == Colors.Blue);
            PumpUntil(() => ColorOf(ripple.Brush) == Colors.Blue);
        }
        finally
        {
            window.Close();
        }

        static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : default;
    }

    [Fact]
    public void HeldEnterActivatesButtonAndToggleOncePerKeyCycle()
    {
        var buttonResources = LoadResources();
        var toggleResources = LoadResources();
        var button = new Button { Theme = GetTheme(buttonResources, typeof(Button)), Content = "Action" };
        var toggle = new ToggleButton
            { Theme = GetTheme(toggleResources, typeof(ToggleButton)), Content = "Toggle" };
        button.Resources.MergedDictionaries.Add(buttonResources);
        toggle.Resources.MergedDictionaries.Add(toggleResources);
        var window = new Window
            { Width = 400, Height = 120, Content = new StackPanel { Children = { button, toggle } } };
        try
        {
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            window.Show();
            window.UpdateLayout();
            button.GetVisualDescendants().OfType<InkRipple>().Single().Brush = null;

            button.Focus();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            clicks.Should().Be(1);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            clicks.Should().Be(2);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

            toggle.Focus();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            toggle.IsChecked.Should().BeTrue();
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void EnterRippleReleasesWithoutWaitingForKeyUp()
    {
        var resources = LoadResources();
        var button = new Button { Theme = GetTheme(resources, typeof(Button)), Content = "Action" };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 400, Height = 120, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            button.Focus();
            var ripple = button.GetVisualDescendants().OfType<InkRipple>().Single();
            var keyboardPress = typeof(InkRipple).GetField("_keyboardPress",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();

            keyboardPress.GetValue(ripple).Should().BeNull();
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void UnselectedTokenCanBeReplacedLocally()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        toggle.Resources["MdCompButtonFilledContainerToggleUnselectedBrush"] = Brushes.Red;
        toggle.ApplyStyling();
        toggle.Background.Should().BeSameAs(Brushes.Red);

        toggle.Resources["MdCompButtonFilledContainerToggleUnselectedBrush"] = Brushes.Blue;
        toggle.Background.Should().BeSameAs(Brushes.Blue);
    }

    [Fact]
    public void ContentDrivenWidthAnimatesAndReducedMotionSnaps()
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var button = new Button { Theme = GetTheme(resources, typeof(Button)), Content = "Short" };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            MotionSettings.ReduceMotion = false;
            window.Show();
            window.UpdateLayout();
            var first = button.Bounds.Width;
            first.Should().BeGreaterThan(0);

            button.Content = "A substantially longer button label";
            window.UpdateLayout();
            var intermediate = button.Bounds.Width;
            intermediate.Should().BeApproximately(first, 1);
            PumpUntil(() => button.Bounds.Width > first + 2);
            var moving = button.Bounds.Width;
            button.Content = "Medium length";
            window.UpdateLayout();
            button.Bounds.Width.Should().BeApproximately(moving, 2);

            MotionSettings.ReduceMotion = true;
            button.Content = "An even more substantially longer button label";
            window.UpdateLayout();
            button.Bounds.Width.Should().BeGreaterThan(moving + 20);
            button.GetVisualDescendants().OfType<Panel>().Single(x => x.Name == "PART_Content")
                .ClipToBounds.Should().BeFalse();
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Fact]
    public void CenteredContentGroupKeepsItsCurrentWidthWhenContentChanges()
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)),
            Content = "Save",
            MinWidth = 240,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        button.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetIcon(button, new Border());
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            MotionSettings.ReduceMotion = false;
            window.Show();
            window.UpdateLayout();
            var group = button.GetVisualDescendants().OfType<Panel>()
                .Single(x => x.Name == "PART_Content");
            var icon = button.GetVisualDescendants().OfType<IconPresenter>()
                .Single(x => x.Name == "PART_Icon");
            var content = button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_ContentPresenter");
            var initialWidth = group.Bounds.Width;
            var initialIconX = icon.TranslatePoint(default, button)!.Value.X;

            button.Content = "Save all changes to this substantially longer profile";
            window.UpdateLayout();

            group.Bounds.Width.Should().BeApproximately(initialWidth, 1);
            icon.TranslatePoint(default, button)!.Value.X.Should().BeApproximately(initialIconX, 1);
            group.ClipToBounds.Should().BeTrue();
            content.ClipToBounds.Should().BeFalse();
            content.Bounds.Width.Should().BeApproximately(content.DesiredSize.Width, .01);
            content.Bounds.Width.Should().BeGreaterThan(group.Bounds.Width);

            PumpUntil(() => group.Bounds.Width > initialWidth + 10);
            icon.TranslatePoint(default, button)!.Value.X.Should().BeLessThan(initialIconX);
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void ContentStaysCenteredWhileButtonWidthAnimates(FlowDirection direction)
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)), Content = "A substantially longer button label",
            FlowDirection = direction
        };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            MotionSettings.ReduceMotion = false;
            window.Show();
            window.UpdateLayout();
            var content = button.GetVisualDescendants().OfType<Panel>()
                .Single(x => x.Name == "PART_Content");
            var firstWidth = button.Bounds.Width;

            button.Content = "Short";
            window.UpdateLayout();
            PumpUntil(() => button.Bounds.Width < firstWidth - 10);
            var x = content.TranslatePoint(default, button)!.Value.X;
            (x + content.Bounds.Width / 2).Should().BeApproximately(button.Bounds.Width / 2, 1);
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void IconPresenceAnimatesSlotAtTheLeadingSide(FlowDirection direction)
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)), Content = "Save", FlowDirection = direction
        };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var slot = button.GetVisualDescendants().OfType<Decorator>()
                .Single(x => x.Name == "PART_IconSlot");
            var content = button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_ContentPresenter");
            slot.Bounds.Width.Should().BeApproximately(0, .01);

            ButtonAssist.SetIcon(button, new Border());
            window.UpdateLayout();
            slot.Bounds.Width.Should().BeLessThan(18);
            PumpUntil(() => slot.Bounds.Width > 18);
            var iconBounds = new Rect(slot.Bounds.Size).TransformToAABB(slot.TransformToVisual(window)!.Value);
            var contentBounds = new Rect(content.Bounds.Size).TransformToAABB(content.TransformToVisual(window)!.Value);
            if (direction == FlowDirection.LeftToRight)
                iconBounds.Right.Should().BeLessThan(contentBounds.Left);
            else
                iconBounds.Left.Should().BeGreaterThan(contentBounds.Right);

            ButtonAssist.SetIcon(button, null);
            window.UpdateLayout();
            slot.Bounds.Width.Should().BeGreaterThan(0);
            PumpUntil(() => slot.Bounds.Width < .1);

            MotionSettings.ReduceMotion = true;
            ButtonAssist.SetIcon(button, new Border());
            window.UpdateLayout();
            slot.Bounds.Width.Should().BeGreaterThan(18);
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, FlowDirection.LeftToRight)]
    [InlineData(HorizontalAlignment.Center, FlowDirection.LeftToRight)]
    [InlineData(HorizontalAlignment.Right, FlowDirection.LeftToRight)]
    [InlineData(HorizontalAlignment.Center, FlowDirection.RightToLeft)]
    public void RevealedContentRemainsUnclippedDuringExpressiveOvershoot(
        HorizontalAlignment alignment, FlowDirection direction)
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var oldScheme = MotionSettings.GlobalScheme;
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)),
            Content = "Short",
            MinWidth = 160,
            HorizontalContentAlignment = alignment,
            FlowDirection = direction
        };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            MotionSettings.ReduceMotion = false;
            MotionSettings.GlobalScheme = MotionScheme.Expressive;
            window.Show();
            window.UpdateLayout();
            var group = button.GetVisualDescendants().OfType<Panel>()
                .Single(x => x.Name == "PART_Content");
            var content = button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_ContentPresenter");

            button.Content = "Save changes to your profile";
            window.UpdateLayout();
            group.ClipToBounds.Should().BeTrue();
            content.Bounds.Width.Should().BeApproximately(content.DesiredSize.Width, .01);
            content.Bounds.Width.Should().BeGreaterThan(group.Bounds.Width);
            PumpUntil(() => !group.ClipToBounds);
            PumpFrames(80);

            button.Content = "Save";
            window.UpdateLayout();
            var targetWidth = content.DesiredSize.Width;
            group.ClipToBounds.Should().BeFalse();
            var crossedBelowTarget = false;
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!crossedBelowTarget && timeout.Elapsed < TimeSpan.FromSeconds(2))
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
                crossedBelowTarget = group.Bounds.Width < targetWidth - .05;
            }

            crossedBelowTarget.Should().BeTrue();
            group.ClipToBounds.Should().BeFalse();
            content.ClipToBounds.Should().BeFalse();
            content.Bounds.Width.Should().BeGreaterThanOrEqualTo(content.DesiredSize.Width - .01);
            button.GetVisualDescendants().Should().NotContain(x => x.Name == "PART_ContentClip");
        }
        finally
        {
            window.Close();
            MotionSettings.GlobalScheme = oldScheme;
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
    }

    [Fact]
    public void WidthConstraintKeepsContentGroupClipped()
    {
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)),
            Content = "A substantially longer button label that cannot fit",
            MaxWidth = 140
        };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var group = button.GetVisualDescendants().OfType<Panel>()
                .Single(x => x.Name == "PART_Content");
            var content = button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_ContentPresenter");

            group.ClipToBounds.Should().BeTrue();
            content.Bounds.Width.Should().BeApproximately(content.DesiredSize.Width, .01);
            content.Bounds.Width.Should().BeGreaterThan(group.Bounds.Width);
            PumpFrames(30);
            group.ClipToBounds.Should().BeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ToggleIconFillFollowsCheckedState()
    {
        var toggle = CreateToggle(ButtonVariant.Filled);
        ButtonAssist.SetIcon(toggle, Material3.Avalonia.Symbols.MaterialSymbol.Check);
        toggle.ApplyStyling();
        toggle.ApplyTemplate();
        var icon = GetIcon(toggle);
        icon.ApplyStyling();

        icon.IsFilled.Should().BeFalse();
        toggle.IsChecked = true;
        icon.ApplyStyling();
        icon.IsFilled.Should().BeTrue();
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Stretch)]
    public void WidthAnimationPreservesExplicitContentAlignment(HorizontalAlignment alignment)
    {
        var resources = LoadResources();
        var button = new Button
        {
            Theme = GetTheme(resources, typeof(Button)),
            Content = "A substantially longer button label",
            HorizontalContentAlignment = alignment
        };
        button.Resources.MergedDictionaries.Add(resources);
        var window = new Window { Width = 800, Height = 200, Content = button };
        try
        {
            window.Show();
            window.UpdateLayout();
            var content = button.GetVisualDescendants().OfType<Panel>()
                .Single(x => x.Name == "PART_Content");
            var firstWidth = button.Bounds.Width;
            button.Content = "Short";
            window.UpdateLayout();
            PumpUntil(() => button.Bounds.Width < firstWidth - 10);
            content.HorizontalAlignment.Should().Be(alignment);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void SizeAndGeometryTokenChangesAnimateHeight()
    {
        var oldReduceMotion = MotionSettings.ReduceMotion;
        var resources = LoadResources();
        var toggle = new ToggleButton { Theme = GetTheme(resources, typeof(ToggleButton)), Content = "Size" };
        toggle.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetSize(toggle, ButtonSize.Small);
        var window = new Window { Width = 800, Height = 200, Content = toggle };
        try
        {
            MotionSettings.ReduceMotion = false;
            window.Show();
            window.UpdateLayout();
            toggle.Bounds.Height.Should().BeApproximately(40, 1);
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 20) < 1);

            ButtonAssist.SetSize(toggle, ButtonSize.Medium);
            window.UpdateLayout();
            toggle.Bounds.Height.Should().BeLessThan(56);
            PumpUntil(() => toggle.Bounds.Height > 42);
            PumpUntil(() => Math.Abs(toggle.Bounds.Height - 56) < 1);
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 28) < 1);

            toggle.Resources["MdCompButtonMdContainerHeight"] = 72d;
            window.UpdateLayout();
            toggle.Bounds.Height.Should().BeLessThan(72);
            PumpUntil(() => Math.Abs(toggle.Bounds.Height - 72) < 1);
            PumpUntil(() => Math.Abs(toggle.CornerRadius.TopLeft - 36) < 1);

            MotionSettings.ReduceMotion = true;
            ButtonAssist.SetSize(toggle, ButtonSize.Small);
            window.UpdateLayout();
            PumpUntil(() => Math.Abs(toggle.Bounds.Height - 40) < 1);
        }
        finally
        {
            window.Close();
            MotionSettings.ReduceMotion = oldReduceMotion;
        }
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

    private static void PumpFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
    }

    private static ToggleButton CreateToggle(ButtonVariant variant)
    {
        var resources = LoadResources();
        var toggle = new ToggleButton
        {
            Theme = GetTheme(resources, typeof(ToggleButton)),
            Content = "Label"
        };
        toggle.Resources.MergedDictionaries.Add(resources);
        ButtonAssist.SetVariant(toggle, variant);
        return toggle;
    }

    private static IconPresenter GetIcon(ToggleButton toggle) => toggle.GetVisualDescendants()
        .OfType<IconPresenter>().Single(x => x.Name == "PART_Icon");

    private static IResourceDictionary LoadResources() =>
        (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"))!;

    private static ControlTheme GetTheme(IResourceDictionary resources, Type type)
    {
        resources.TryGetResource(type, null, out var value).Should().BeTrue();
        return value.Should().BeOfType<ControlTheme>().Subject;
    }
}