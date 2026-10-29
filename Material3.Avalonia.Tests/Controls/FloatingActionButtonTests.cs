using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
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
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class FloatingActionButtonTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];
    private readonly bool _reduceMotion;
    private readonly MotionScheme _scheme;

    public FloatingActionButtonTests()
    {
        TestApp.EnsureStarted();
        _reduceMotion = MotionSettings.ReduceMotion;
        _scheme = MotionSettings.GlobalScheme;
        _theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
        MotionSettings.ReduceMotion = true;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShortSmallLabelsHaveAnExtendedMinimumWithoutChangingTheCompactWidth(bool hasIcon)
    {
        var button = new FloatingActionButton { Content = "I", Icon = hasIcon ? MaterialSymbol.Add : null };
        var window = Show(button);
        button.Bounds.Width.Should().Be(80);
        button.MinWidth.Should().Be(0);
        var label = Part<ContentPresenter>(button);
        var groupWidth = label.DesiredSize.Width +
                         (hasIcon ? Part<IconPresenter>(button).Bounds.Width + Part<FabContentMotion>(button).Gap : 0);
        var start = hasIcon ? Part<IconPresenter>(button).Bounds.Left : label.Bounds.Left;
        (start + groupWidth / 2).Should().BeApproximately(button.Bounds.Width / 2, 1);
        button.IsExtended = false;
        Layout(window);
        button.Bounds.Width.Should().Be(hasIcon ? 56 : 80);
        if (!hasIcon)
            return;
        MotionSettings.ReduceMotion = false;
        button.IsExtended = true;
        PumpUntil(() => button.Bounds.Width is > 56 and < 80, window);
        PumpUntil(() => Math.Abs(button.Bounds.Width - 80) < .1, window);
    }

    [Fact]
    public void DefaultMarginUsesLiveSpacingAndCanBeOverriddenByTheLayout()
    {
        var button = new FloatingActionButton
        {
            Icon = MaterialSymbol.Add, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var panel = new Panel { Children = { button } };
        var window = Show(panel);
        button.Margin.Should().Be(new Thickness(16));
        button.Bounds.Size.Should().Be(new Size(56, 56));
        button.Bounds.Right.Should().Be(panel.Bounds.Width - 16);
        button.Bounds.Bottom.Should().Be(panel.Bounds.Height - 16);
        button.Resources["MdSysSpacing200"] = 20d;
        Layout(window);
        button.Margin.Should().Be(new Thickness(20));
        button.Margin = default;
        button.Resources["MdSysSpacing200"] = 24d;
        Layout(window);
        button.Margin.Should().Be(default(Thickness));
        button.Bounds.Right.Should().Be(panel.Bounds.Width);
        button.Bounds.Bottom.Should().Be(panel.Bounds.Height);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachCompletedExitRaisesHiddenOnce(bool reduceMotion)
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add };
        var window = Show(button);
        MotionSettings.ReduceMotion = reduceMotion;
        var reveal = Part<FabReveal>(button);
        var completed = 0;
        reveal.Hidden += (_, _) => completed++;
        for (var i = 1; i <= 2; i++)
        {
            button.IsPresented = false;
            PumpUntil(() => !button.IsVisible, window);
            completed.Should().Be(i);
            button.IsPresented = true;
            PumpUntil(() => reveal.Alpha >= .999, window);
        }
    }

    [Theory]
    [InlineData(FloatingActionButtonSize.Small, 56, 24, 16, 16, 8)]
    [InlineData(FloatingActionButtonSize.Medium, 80, 28, 20, 26, 12)]
    [InlineData(FloatingActionButtonSize.Large, 96, 36, 28, 28, 16)]
    public void SizesFollowExpressiveGeometryAndTextOnlyNeverCollapses(FloatingActionButtonSize size,
        double height, double iconSize, double radius, double padding, double gap)
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add, Size = size };
        DensityAssist.SetDensity(button, MaterialDensity.Dense5);
        var window = Show(button);
        button.Bounds.Size.Should().Be(new Size(height, height));
        button.CornerRadius.Should().Be(new CornerRadius(radius));
        Part<IconPresenter>(button).Size.Should().Be(iconSize);
        button.Content = "Compose";
        Layout(window);
        button.Padding.Should().Be(new Thickness(padding, 0));
        var label = Part<ContentPresenter>(button);
        button.Bounds.Width.Should().BeApproximately(padding * 2 + iconSize + gap + label.DesiredSize.Width, 1);
        button.Bounds.Height.Should().Be(height);
        button.IsExtended = false;
        Layout(window);
        button.Bounds.Width.Should().Be(height);
        button.Icon = null;
        Layout(window);
        button.IsEffectivelyExtended.Should().BeTrue();
        button.Bounds.Width.Should().BeGreaterThan(height);
        label.Opacity.Should().Be(1);
    }

    [Theory]
    [InlineData(FloatingActionButtonVariant.PrimaryContainer)]
    [InlineData(FloatingActionButtonVariant.SecondaryContainer)]
    [InlineData(FloatingActionButtonVariant.TertiaryContainer)]
    [InlineData(FloatingActionButtonVariant.Primary)]
    [InlineData(FloatingActionButtonVariant.Secondary)]
    [InlineData(FloatingActionButtonVariant.Tertiary)]
    public void IndependentFabAndExtendedTokensUpdateThroughLocalAliases(FloatingActionButtonVariant variant)
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add, Variant = variant };
        var window = Show(button);
        button.Resources["CustomFabColor"] = Brushes.Magenta;
        button.Resources[$"MdCompFab{variant}IconBrush"] = new TokenAlias("CustomFabColor");
        button.Resources[$"MdCompFab{variant}ContainerShadowColor"] = Colors.Red;
        Layout(window);
        PumpUntil(() => ReferenceEquals(Part<IconPresenter>(button).Foreground, Brushes.Magenta), window);
        Part<IconPresenter>(button).Foreground.Should().BeSameAs(Brushes.Magenta);
        Part<Border>(button).BoxShadow[0].Color.R.Should().Be(255);
        button.Content = "Create";
        button.Resources[$"MdCompExtendedFab{variant}IconBrush"] = Brushes.Cyan;
        button.Resources[$"MdCompExtendedFab{variant}LabelBrush"] = Brushes.Lime;
        Layout(window);
        PumpUntil(() => ReferenceEquals(Part<IconPresenter>(button).Foreground, Brushes.Cyan), window);
        Part<IconPresenter>(button).Foreground.Should().BeSameAs(Brushes.Cyan);
        button.Foreground.Should().BeSameAs(Brushes.Lime);
        button.IsExtended = false;
        button.Resources["MdCompExtendedFabSmallLeadingSpace"] = 100d;
        button.Resources["CustomFabColor"] = Brushes.Blue;
        Layout(window);
        PumpUntil(() => ReferenceEquals(Part<IconPresenter>(button).Foreground, Brushes.Blue), window);
        Part<IconPresenter>(button).Foreground.Should().BeSameAs(Brushes.Blue);
        button.Bounds.Width.Should().Be(56);
        button.Resources.Remove("MdCompExtendedFabSmallLeadingSpace");
        button.Foreground = Brushes.Gold;
        button.Background = Brushes.Salmon;
        button.CornerRadius = new CornerRadius(5);
        button.Width = 240;
        button.IsExtended = true;
        Layout(window);
        PumpUntil(() => ReferenceEquals(Part<IconPresenter>(button).Foreground, Brushes.Gold), window);
        Part<IconPresenter>(button).Foreground.Should().BeSameAs(Brushes.Gold);
        button.Background.Should().BeSameAs(Brushes.Salmon);
        button.CornerRadius.Should().Be(new CornerRadius(5));
        button.Bounds.Width.Should().Be(240);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Layout(window);
        button.Background.Should().BeSameAs(Brushes.Salmon);
    }

    [Fact]
    public void NativeInputCommandsAndAccessibleNameSurviveCollapseAndAutomaticTipsRespectOverrides()
    {
        var command = new CountingCommand();
        var button = new FloatingActionButton
        {
            Icon = MaterialSymbol.Edit, Content = "Compose", Command = command,
            CommandParameter = "message"
        };
        var window = Show(button);
        button.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        command.Count.Should().Be(1);
        command.Parameter.Should().Be("message");
        var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
        peer.GetName().Should().Be("Compose");
        ToolTip.GetTip(button).Should().BeNull();
        button.IsExtended = false;
        Layout(window);
        button.IsFocused.Should().BeTrue();
        peer.GetName().Should().Be("Compose");
        ToolTip.GetTip(button).Should().Be("Compose");
        ((IInvokeProvider)peer).Invoke();
        Layout(window);
        command.Count.Should().Be(2);
        var point = button.TranslatePoint(new Point(28, 28), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        command.Count.Should().Be(3);
        ToolTip.SetTip(button, null);
        button.IsExtended = true;
        button.IsExtended = false;
        ToolTip.GetTip(button).Should().BeNull();
        AutomationProperties.SetName(button, "Compose a new message");
        peer.GetName().Should().Be("Compose a new message");
        Action invalid = () => ButtonAssist.SetIcon(button, MaterialSymbol.Add);
        invalid.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NativePointerAndKeyboardStatesKeepFabShapeAndUseTheirElevationTokens()
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add, Content = "Create" };
        var window = Show(button);
        var state = Part<StateLayer>(button);
        var shadow = Part<Border>(button);
        var point = button.TranslatePoint(new Point(28, 28), window)!.Value;
        window.MouseMove(point);
        PumpUntil(() => state.Opacity >= .079, window);
        button.Classes.Should().Contain("m3-hovered");
        var hovered = (BoxShadows)button.FindResource("MdSysShadowLevel4")!;
        PumpUntil(() => Math.Abs(shadow.BoxShadow[1].OffsetY - hovered[1].OffsetY) < .01, window);
        button.Focus(NavigationMethod.Tab);
        PumpUntil(() => state.Opacity >= .099, window);
        var normal = (BoxShadows)button.FindResource("MdSysShadowLevel3")!;
        PumpUntil(() => Math.Abs(shadow.BoxShadow[1].OffsetY - normal[1].OffsetY) < .01, window);
        window.MouseDown(point, MouseButton.Left);
        Layout(window);
        button.IsPressed.Should().BeTrue();
        button.CornerRadius.Should().Be(new CornerRadius(16));
        window.MouseUp(point, MouseButton.Left);
        button.IsPressed.Should().BeFalse();
        button.IsPresented = false;
        PumpUntil(() => state.Opacity < .001, window);
    }

    [Fact]
    public void NativeShadowTransitionsSupportLayerChangesAndInterruption()
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add };
        const string geometryKey = "MdCompFabPrimaryContainerContainerElevation";
        const string colorKey = "MdCompFabPrimaryContainerContainerShadowColor";
        button.Resources[geometryKey] = BoxShadows.Parse("0 2 3 1 #80000000");
        button.Resources[colorKey] = Colors.Red;
        var window = Show(button);
        var border = Part<Border>(button);
        border.BoxShadow.Count.Should().Be(1);
        MotionSettings.ReduceMotion = false;
        button.Resources[geometryKey] = BoxShadows.Parse("0 20 12 3 #80000000, 0 8 16 4 #40000000, 0 4 8 2 #20000000");
        PumpUntil(() => border.BoxShadow.Count > 0 && border.BoxShadow[0].OffsetY is > 2 and < 20, window);
        var intermediate = border.BoxShadow[0].OffsetY;
        button.Resources[geometryKey] = BoxShadows.Parse("0 -4 6 2 #60000000");
        button.Resources[colorKey] = Colors.Blue;
        Layout(window);
        border.BoxShadow[0].OffsetY.Should().BeApproximately(intermediate, 1);
        PumpUntil(() => border.BoxShadow.Count == 1 && Math.Abs(border.BoxShadow[0].OffsetY + 4) < .01, window);
        PumpUntil(() => border.BoxShadow[0].Color == Color.FromArgb(96, 0, 0, 255), window);
        MotionSettings.ReduceMotion = true;
        button.Resources[geometryKey] = default(BoxShadows);
        Layout(window);
        border.BoxShadow.Count.Should().Be(0);
    }

    [Fact]
    public void ScrollDirectionAndBoundariesCollapseWithoutReplacingRequestedStateBinding()
    {
        var source = new CheckBox { IsChecked = true };
        var viewer = new ScrollViewer { Height = 100, Content = new Border { Height = 1000 } };
        var button = new FloatingActionButton
            { Content = "Compose", Icon = MaterialSymbol.Edit, ScrollViewer = viewer };
        using var binding = button.Bind(FloatingActionButton.IsExtendedProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = source });
        var window = Show(new StackPanel { Children = { viewer, button } });
        SetOffset(100);
        button.IsEffectivelyExtended.Should().BeFalse();
        button.IsExtended.Should().BeTrue();
        SetOffset(99.75);
        button.IsEffectivelyExtended.Should().BeFalse();
        SetOffset(90);
        button.IsEffectivelyExtended.Should().BeTrue();
        SetOffset(120);
        button.IsEffectivelyExtended.Should().BeFalse();
        SetOffset(900);
        button.IsEffectivelyExtended.Should().BeTrue();
        source.IsChecked = false;
        SetOffset(0);
        button.IsEffectivelyExtended.Should().BeFalse();
        source.IsChecked = true;
        button.IsEffectivelyExtended.Should().BeTrue();
        button.ScrollViewer = null;
        SetOffset(200);
        button.IsEffectivelyExtended.Should().BeTrue();
        button.ScrollViewer = viewer;
        viewer.Content = new Border { Height = 30 };
        Layout(window);
        button.IsEffectivelyExtended.Should().BeTrue();

        void SetOffset(double offset)
        {
            viewer.Offset = new Vector(0, offset);
            Layout(window);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnavailableButtonsExitAndRecoverWithoutMutatingNativeVisibility(bool reduceMotion)
    {
        var command = new CountingCommand();
        var button = new FloatingActionButton { Content = "Compose", Icon = MaterialSymbol.Edit, Command = command };
        var window = Show(button);
        MotionSettings.ReduceMotion = reduceMotion;
        var reveal = Part<FabReveal>(button);
        var peer = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(button)!;
        button.Focus(NavigationMethod.Tab);
        button.IsPresented = false;
        button.IsHitTestVisible.Should().BeFalse();
        button.Focusable.Should().BeFalse();
        button.IsFocused.Should().BeFalse();
        peer.Invoke();
        Layout(window);
        command.Count.Should().Be(0);
        if (!reduceMotion)
            PumpUntil(() => reveal.Alpha is > 0 and < 1, window);
        PumpUntil(() => !button.IsVisible, window);
        button.IsPresented = true;
        PumpUntil(() => reveal.Alpha >= .999, window);
        button.IsVisible.Should().BeTrue();
        button.IsEnabled = false;
        PumpUntil(() => !button.IsVisible, window);
        button.IsEnabled = true;
        PumpUntil(() => reveal.Alpha >= .999, window);
        command.SetAvailable(false);
        PumpUntil(() => !button.IsVisible, window);
        command.SetAvailable(true);
        PumpUntil(() => reveal.Alpha >= .999, window);
        button.IsVisible = false;
        button.IsPresented = false;
        button.IsPresented = true;
        button.IsVisible.Should().BeFalse();
        button.ClearValue(Visual.IsVisibleProperty);
        button.IsVisible.Should().BeTrue();
    }

    [Theory]
    [InlineData(MotionSchemeKind.Standard, "Presented")]
    [InlineData(MotionSchemeKind.Standard, "Enabled")]
    [InlineData(MotionSchemeKind.Standard, "Command")]
    [InlineData(MotionSchemeKind.Expressive, "Presented")]
    [InlineData(MotionSchemeKind.Expressive, "Enabled")]
    [InlineData(MotionSchemeKind.Expressive, "Command")]
    public void FirstLabelAndSizeChangesAfterShowingTheSameActionStillAnimate(MotionSchemeKind scheme, string source)
    {
        var command = new CountingCommand();
        var button = new FloatingActionButton
        {
            Content = "Compose", Icon = MaterialSymbol.Edit, Size = FloatingActionButtonSize.Medium,
            Command = command, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var window = Show(new Panel { Children = { button } });
        MotionSettings.ReduceMotion = false;
        MotionSettings.GlobalScheme = scheme == MotionSchemeKind.Standard
            ? MotionScheme.Standard
            : MotionScheme.Expressive;
        foreach (var changeLabel in new[] { true, false })
        {
            Present(false);
            PumpUntil(() => !button.IsVisible, window);
            Present(true);
            PumpUntil(() => Part<FabReveal>(button).Alpha >= .999, window);
            var previousWidth = button.Bounds.Width;
            if (changeLabel)
                button.Content = "Compose a new message";
            else
                button.Size = FloatingActionButtonSize.Large;
            Layout(window);
            var targetWidth = button.Padding.Left + Part<IconPresenter>(button).DesiredSize.Width +
                              Part<FabContentMotion>(button).Gap + Part<ContentPresenter>(button).DesiredSize.Width +
                              button.Padding.Right;
            button.Bounds.Width.Should().BeLessThan(targetWidth - 1);
            PumpUntil(() => button.Bounds.Width > previousWidth + 1 && button.Bounds.Width < targetWidth - 1, window);
            PumpUntil(() => Math.Abs(button.Bounds.Width - targetWidth) < 1, window);
        }

        void Present(bool value)
        {
            switch (source)
            {
                case "Presented": button.IsPresented = value; break;
                case "Enabled": button.IsEnabled = value; break;
                default: command.SetAvailable(value); break;
            }
        }
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight, HorizontalAlignment.Left)]
    [InlineData(FlowDirection.RightToLeft, HorizontalAlignment.Right)]
    [InlineData(FlowDirection.LeftToRight, HorizontalAlignment.Center)]
    public void WidthAndLabelAnimateWithoutStretchingAndCanBeInterrupted(FlowDirection direction,
        HorizontalAlignment alignment)
    {
        var button = new FloatingActionButton
        {
            Content = "Compose", Icon = MaterialSymbol.Edit,
            FlowDirection = direction, HorizontalAlignment = alignment
        };
        var window = Show(button);
        var expandedWidth = button.Bounds.Width;
        var label = Part<ContentPresenter>(button);
        var textSize = label.DesiredSize;
        var icon = Part<IconPresenter>(button);
        var initialIconX = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, 0), button)!.Value.X;
        // Avalonia applies RTL mirroring after layout, outside TranslatePoint's coordinate space.
        var renderedIconX = direction == FlowDirection.RightToLeft ? button.Bounds.Width - initialIconX : initialIconX;
        if (direction == FlowDirection.RightToLeft)
            renderedIconX.Should().BeGreaterThan(button.Bounds.Width / 2);
        else
            renderedIconX.Should().BeLessThan(button.Bounds.Width / 2);
        MotionSettings.ReduceMotion = false;
        MotionSettings.GlobalScheme = MotionScheme.Expressive;
        button.IsExtended = false;
        Layout(window);
        PumpUntil(() => button.Bounds.Width > 57 && button.Bounds.Width < expandedWidth - 1, window);
        label.DesiredSize.Should().Be(textSize);
        var intermediate = button.Bounds.Width;
        button.IsExtended = true;
        Layout(window);
        button.Bounds.Width.Should().BeApproximately(intermediate, 1);
        PumpUntil(() => Math.Abs(button.Bounds.Width - expandedWidth) < .1 && label.Opacity > .999, window);
        button.IsExtended = false;
        PumpUntil(() => Math.Abs(button.Bounds.Width - 56) < .1 && label.Opacity < .001, window);
        label.DesiredSize.Should().Be(textSize);
        var center = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, 0), button)!.Value.X;
        center.Should().BeApproximately(button.Bounds.Width / 2, 1);
        MotionSettings.ReduceMotion = true;
        button.IsExtended = true;
        Layout(window);
        button.Bounds.Width.Should().BeApproximately(expandedWidth, 1);
    }

    [Theory]
    [InlineData(MotionSchemeKind.Standard)]
    [InlineData(MotionSchemeKind.Expressive)]
    public void ChangedLabelsStayFullyVisibleAfterAnimatedGeometryChanges(MotionSchemeKind scheme)
    {
        MotionSettings.GlobalScheme = scheme == MotionSchemeKind.Standard
            ? MotionScheme.Standard
            : MotionScheme.Expressive;
        var button = new FloatingActionButton
        {
            Icon = MaterialSymbol.Edit, Content = "Compose", Size = FloatingActionButtonSize.Medium,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var window = Show(button);
        MotionSettings.ReduceMotion = false;
        var label = Part<ContentPresenter>(button);
        button.Content = "Compose a new message";
        Settle();
        CheckLabel("Compose a new message");
        for (var i = 0; i < 2; i++)
        {
            button.IsExtended = false;
            Settle();
            button.Bounds.Width.Should().Be(80);
            button.IsExtended = true;
            Settle();
            CheckLabel("Compose a new message");
        }

        button.Content = "Local tokens";
        button.Size = FloatingActionButtonSize.Small;
        button.HorizontalAlignment = HorizontalAlignment.Center;
        Settle();
        CheckLabel("Local tokens");

        void Settle()
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < TimeSpan.FromMilliseconds(500))
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Layout(window);
                Thread.Sleep(5);
            }
        }

        void CheckLabel(string expected)
        {
            var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
            text.Text.Should().Be(expected);
            if (label.Clip is RectangleGeometry clip)
                clip.Rect.Width.Should().BeGreaterThanOrEqualTo(label.DesiredSize.Width - 1);
            // Avalonia snapshots Clip into a platform geometry; layout properties alone miss stale render clips.
            var renderedClip = (IGeometryImpl?)typeof(CompositionVisual)
                .GetProperty("Clip",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(ElementComposition.GetElementVisual(label))!;
            if (label.Clip is null)
                renderedClip.Should().BeNull();
            else
            {
                var renderedBounds = (Rect)typeof(IGeometryImpl)
                    .GetProperty("Bounds", System.Reflection.BindingFlags.Instance |
                                           System.Reflection.BindingFlags.NonPublic |
                                           System.Reflection.BindingFlags.Public)!
                    .GetValue(renderedClip)!;
                renderedBounds.Width.Should().BeGreaterThanOrEqualTo(label.DesiredSize.Width - 1);
            }

            label.Opacity.Should().BeApproximately(1, .001);
            var width = button.Padding.Left + Part<IconPresenter>(button).DesiredSize.Width +
                        Part<FabContentMotion>(button).Gap + label.DesiredSize.Width + button.Padding.Right;
            button.Bounds.Width.Should().BeApproximately(width, 1);
        }
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight, HorizontalAlignment.Left)]
    [InlineData(FlowDirection.LeftToRight, HorizontalAlignment.Center)]
    [InlineData(FlowDirection.RightToLeft, HorizontalAlignment.Right)]
    public void RevealedLabelsRemainVisibleDuringExpressiveWidthUndershoot(FlowDirection direction,
        HorizontalAlignment alignment)
    {
        const string longLabel = "Compose a new message for this project";
        var button = new FloatingActionButton
        {
            Icon = MaterialSymbol.Edit, Content = longLabel,
            FlowDirection = direction, HorizontalAlignment = alignment
        };
        var window = Show(button);
        var expandedWidth = button.Bounds.Width;
        var label = Part<ContentPresenter>(button);
        var group = Part<FabContentMotion>(button);
        MotionSettings.ReduceMotion = false;
        MotionSettings.GlobalScheme = MotionScheme.Expressive;
        button.Content = "Compose";
        Layout(window);
        var targetWidth = button.Padding.Left + Part<IconPresenter>(button).DesiredSize.Width +
                          group.Gap + label.DesiredSize.Width + button.Padding.Right;
        PumpUntil(() => button.Bounds.Width < targetWidth - .5, window);
        group.ClipToBounds.Should().BeFalse();
        label.Clip.Should().BeNull();
        label.Bounds.Width.Should().BeGreaterThanOrEqualTo(label.DesiredSize.Width);
        button.Content = longLabel;
        Layout(window);
        group.ClipToBounds.Should().BeTrue();
        PumpUntil(() => !group.ClipToBounds, window);
        label.Clip.Should().BeNull();
        PumpUntil(() => Math.Abs(button.Bounds.Width - expandedWidth) < .1, window);
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void ActualWidthConstraintsKeepExtendedLabelsClipped(FlowDirection direction)
    {
        var button = new FloatingActionButton
        {
            Icon = MaterialSymbol.Check, Content = "Сохранить изменения профиля",
            FlowDirection = direction, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var host = new Border { Width = 200, Child = button };
        var window = Show(host);
        var label = Part<ContentPresenter>(button);
        Part<FabContentMotion>(button).ClipToBounds.Should().BeTrue();
        label.Clip.Should().BeOfType<RectangleGeometry>().Which.Rect.Width.Should()
            .BeLessThan(label.DesiredSize.Width);
        button.Bounds.Width.Should().Be(168);
        button.Content = "Go";
        Layout(window);
        Part<FabContentMotion>(button).ClipToBounds.Should().BeFalse();
        label.Clip.Should().BeNull();
    }

    [Fact]
    public void NewPageActionIsReadyBeforeTheButtonReappears()
    {
        var firstIcon = new Border { Width = 20, Height = 20 };
        var nextIcon = new Border { Width = 20, Height = 20 };
        var button = new FloatingActionButton { Icon = firstIcon, Content = "Compose a message" };
        var window = Show(button);
        var nextAction = new FloatingActionButton { Icon = new Border { Width = 20, Height = 20 }, Content = "Create" };
        Show(nextAction);
        var expectedWidth = nextAction.Bounds.Width;
        MotionSettings.ReduceMotion = false;
        button.IsPresented = false;
        PumpUntil(() => !button.IsVisible, window);

        button.Content = "Create";
        button.Icon = nextIcon;
        Layout(window);
        nextIcon.GetVisualAncestors().Should().Contain(Part<IconPresenter>(button));
        var iconScale = (ScaleTransform)nextIcon.GetVisualAncestors().OfType<Viewbox>().Single().RenderTransform!;
        iconScale.ScaleX.Should().Be(1);

        button.IsPresented = true;
        Layout(window);
        button.Bounds.Width.Should().BeApproximately(expectedWidth, 1);
        PumpUntil(() => Part<FabReveal>(button).Alpha is > 0 and < .99, window);
        iconScale.ScaleX.Should().Be(1);
        button.Bounds.Width.Should().BeApproximately(expectedWidth, 1);
    }

    [Fact]
    public void HiddenInitialStateAndTemplateReplacementDoNotLeaveStaleExitCallbacks()
    {
        var button = new FloatingActionButton { Icon = MaterialSymbol.Add, Content = "Create", IsPresented = false };
        var panel = new StackPanel { Children = { button } };
        var window = Show(panel);
        button.IsVisible.Should().BeFalse();
        MotionSettings.ReduceMotion = false;
        button.IsPresented = true;
        PumpUntil(() => button.IsVisible && Part<FabReveal>(button).Alpha is > 0 and < .99, window);
        PumpUntil(() => Part<FabReveal>(button).Alpha >= .999, window);
        button.IsPresented = false;
        panel.Children.Remove(button);
        button.IsPresented = true;
        panel.Children.Add(button);
        Layout(window);
        button.IsVisible.Should().BeTrue();
        var template = button.Template;
        button.Template = null;
        button.ApplyTemplate();
        button.Template = template;
        button.ApplyTemplate();
        Layout(window);
        PumpUntil(() => Part<FabReveal>(button).Alpha >= .999, window);
        button.IsVisible.Should().BeTrue();
    }

    private Window Show(Control content)
    {
        var window = new Window { Width = 500, Height = 400, Content = content };
        _windows.Add(window);
        window.Show();
        Layout(window);
        return window;
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static T Part<T>(FloatingActionButton button) where T : Control =>
        button.GetVisualDescendants().OfType<T>().Single(x => x.TemplatedParent == button);

    private static void PumpUntil(Func<bool> condition, Window window)
    {
        Layout(window);
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(3))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Layout(window);
            Thread.Sleep(5);
        }

        condition().Should().BeTrue();
    }

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Application.Current!.Styles.Remove(_theme);
        MotionSettings.ReduceMotion = _reduceMotion;
        MotionSettings.GlobalScheme = _scheme;
    }

    private sealed class CountingCommand : ICommand
    {
        private bool _available = true;
        public int Count { get; private set; }
        public object? Parameter { get; private set; }
        public bool CanExecute(object? parameter) => _available;

        public void Execute(object? parameter)
        {
            Count++;
            Parameter = parameter;
        }

        public event EventHandler? CanExecuteChanged;

        public void SetAvailable(bool value)
        {
            _available = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}