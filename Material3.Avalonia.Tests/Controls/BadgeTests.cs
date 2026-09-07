using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Controls.Helpers;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class BadgeTests : IDisposable
{
    private readonly bool _reduceMotion = MotionSettings.ReduceMotion;

    public BadgeTests() => MotionSettings.ReduceMotion = true;

    public void Dispose() => MotionSettings.ReduceMotion = _reduceMotion;

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void LargeBadge_ShouldAnimateContentWidthSeparatelyFromItsHorizontalWipe(FlowDirection direction)
    {
        using var scene = new BadgeScene();
        scene.Host.FlowDirection = direction;
        scene.Badge.Count = 9;
        scene.Layout();
        var reveal = scene.Badge.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Reveal");
        var anchor = scene.BoundsInWindow(scene.Anchor);
        MotionSettings.ReduceMotion = false;
        foreach (var count in new[] { 10, 999, 1000, 9 })
        {
            var previousWidth = reveal.Child!.Clip!.Bounds.Width;
            scene.Badge.Count = count;
            scene.Layout();
            var targetWidth = scene.Badge.Bounds.Width;
            reveal.Child.Clip!.Bounds.Width.Should().BeInRange(Math.Min(previousWidth, targetWidth),
                Math.Max(previousWidth, targetWidth));
            Math.Abs(reveal.Child.Clip.Bounds.Width - targetWidth).Should().BeGreaterThan(0.1);
            var labelBounds = scene.BoundsInWindow(scene.Badge.Presenter!.Child!);
            PumpUntil(() => Math.Abs(reveal.Child.Clip!.Bounds.Width - targetWidth) < 0.001);
            scene.BoundsInWindow(scene.Badge.Presenter!.Child!).Should().Be(labelBounds);
            scene.BoundsInWindow(scene.Anchor).Should().Be(anchor);
            reveal.Child.Clip!.Bounds.Height.Should().Be(16);
            scene.Badge.Presenter!.Child.Should().BeOfType<TextBlock>().Which.FontSize.Should().Be(11);
        }

        scene.Badge.Count = 999;
        scene.Layout();
        PumpUntil(() => reveal.Child!.Clip!.Bounds.Width is > 17 and < 28);
        scene.Badge.Count = 10;
        scene.Layout();
        PumpUntil(() => Math.Abs(reveal.Child!.Clip!.Bounds.Width - scene.Badge.Bounds.Width) < 0.001);

        var text = scene.Badge.Presenter!.Child!;
        var textBounds = scene.BoundsInWindow(text);
        scene.Badge.IsActive = false;
        PumpUntil(() => reveal.Clip!.Bounds.Width is > 10 and < 14);
        reveal.Clip!.Bounds.Height.Should().Be(16);
        reveal.Child!.Clip!.Bounds.Width.Should().Be(scene.Badge.Bounds.Width);
        reveal.Child!.Clip!.Bounds.Height.Should().Be(16);
        reveal.Child.Clip.Bounds.Bottom.Should().Be(16);
        scene.BoundsInWindow(text).Should().Be(textBounds);
        PumpUntil(() => !scene.Badge.IsVisible);
    }

    [Fact]
    public void Activity_ShouldAnimateExitAndReverseWithoutHidingOrReplacingBindings()
    {
        using var scene = new BadgeScene();
        scene.Badge.Count = 7;
        scene.Layout();
        var reveal = scene.Badge.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Reveal");
        var progress = AvaloniaPropertyRegistry.Instance.FindRegistered(reveal, "Progress")!;
        double Progress() => (double)reveal.GetValue(progress)!;
        var size = scene.Badged.DesiredSize;
        var source = new CheckBox { IsChecked = true };
        using var binding = scene.Badge.Bind(Badge.IsActiveProperty, new Binding("IsChecked") { Source = source });
        MotionSettings.ReduceMotion = false;
        source.IsChecked = false;
        scene.Layout();
        scene.Badge.IsActive.Should().BeFalse();
        scene.Badge.IsVisible.Should().BeTrue();
        scene.Badge.DisplayContent.Should().Be("7");
        PumpUntil(() => Progress() is > 0 and < 0.9);
        scene.Badge.IsVisible.Should().BeTrue();
        source.IsChecked = true;
        PumpUntil(() => Progress() == 1);
        scene.Badge.IsVisible.Should().BeTrue();
        scene.Badged.DesiredSize.Should().Be(size);
        scene.Badge.Count = 0;
        scene.Badge.DisplayContent.Should().Be("7");
        PumpUntil(() => !scene.Badge.IsVisible);
        scene.Badge.DisplayContent.Should().Be("0");
        source.IsChecked.Should().BeTrue();
        scene.Badge.Count = 9;
        PumpUntil(() => Progress() == 1);
        scene.Badge.IsVisible.Should().BeTrue();
        MotionSettings.ReduceMotion = true;
        source.IsChecked = false;
        scene.Badge.IsVisible.Should().BeFalse();
        Progress().Should().Be(0);
    }

    private static void PumpUntil(Func<bool> predicate)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!predicate() && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        predicate().Should().BeTrue();
    }

    [Fact]
    public void InitiallyInactiveBadge_ShouldAnimateItsFirstAppearance()
    {
        using var scene = new BadgeScene();
        var badge = new Badge { IsActive = false };
        scene.Badged.Badge = badge;
        scene.Layout();
        MotionSettings.ReduceMotion = false;
        badge.IsActive = true;
        scene.Layout();
        var reveal = badge.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Reveal");
        var progress = AvaloniaPropertyRegistry.Instance.FindRegistered(reveal, "Progress")!;
        ((double)reveal.GetValue(progress)!).Should().BeLessThan(1);
        PumpUntil(() => (double)reveal.GetValue(progress)! == 1);
        badge.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void SingleDigit_ShouldCenterWithoutRoundingItsTextOrigin()
    {
        using var scene = new BadgeScene();
        foreach (var count in new[] { 1, 7, 9 })
        {
            scene.Badge.Count = count;
            scene.Layout();
            var text = scene.Badge.Presenter!.Child.Should().BeOfType<TextBlock>().Subject;
            var origin = text.TranslatePoint(default, scene.Badge)!.Value;
            (origin.X + text.TextLayout.Width / 2).Should().BeApproximately(scene.Badge.Bounds.Width / 2, 0.001);
        }
    }

    [Fact]
    public void Counts_ShouldFormatWithoutReplacingContentOrVisibilityBindings()
    {
        using var scene = new BadgeScene();
        var badge = scene.Badge;
        var source = new TextBlock { Text = "New", IsVisible = true };
        using var contentBinding = badge.Bind(ContentControl.ContentProperty, new Binding("Text") { Source = source });
        using var visibleBinding = badge.Bind(Visual.IsVisibleProperty, new Binding("IsVisible") { Source = source });

        foreach (var (count, label) in new[]
                 {
                     (1, "1"), (9, "9"), (10, "10"), (99, "99"), (999, "999"), (1000, "999+"), (int.MaxValue, "999+")
                 })
        {
            badge.Count = count;
            scene.Layout();
            badge.Presenter!.Child.Should().BeOfType<TextBlock>().Which.Text.Should().Be(label);
            badge.Content.Should().Be("New");
        }

        badge.MaxCount = 99;
        scene.Layout();
        badge.DisplayContent.Should().Be("99+");
        badge.Count = 0;
        scene.Layout();
        badge.IsVisible.Should().BeFalse();
        badge.DesiredSize.Should().Be(default(Size));
        source.IsVisible.Should().BeTrue();
        badge.ShowZero = true;
        scene.Layout();
        badge.IsVisible.Should().BeTrue();
        badge.DisplayContent.Should().Be("0");
        source.IsVisible = false;
        badge.Count = 3;
        scene.Layout();
        badge.IsVisible.Should().BeFalse();
        source.IsVisible = true;
        badge.Count = null;
        source.Text = "Done";
        scene.Layout();
        badge.IsVisible.Should().BeTrue();
        badge.DisplayContent.Should().Be("Done");
        badge.Invoking(x => x.Count = -1).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Overlay_ShouldKeepAnchorLayoutAndMirrorExactlyOnce(bool stretch)
    {
        using var scene = new BadgeScene();
        scene.Badged.Width = stretch ? 180 : double.NaN;
        scene.Badged.Height = stretch ? 60 : double.NaN;
        scene.Layout();
        var size = scene.Badged.DesiredSize;
        var anchor = scene.BoundsInWindow(scene.Anchor);
        scene.Badge.Bounds.Size.Should().Be(new Size(6, 6));
        scene.BoundsInWindow(scene.Badge).Should().Be(new Rect(anchor.Right - 6, anchor.Top, 6, 6));

        foreach (var count in new[] { 1, 10, 1000 })
        {
            scene.Badge.Count = count;
            scene.Layout();
            scene.Badged.DesiredSize.Should().Be(size);
            scene.BoundsInWindow(scene.Anchor).Should().Be(anchor);
            var badgeRect = scene.BoundsInWindow(scene.Badge);
            badgeRect.Left.Should().Be(anchor.Right - 12);
            badgeRect.Bottom.Should().Be(anchor.Top + 14);
            badgeRect.Height.Should().Be(16);
            var transformed = scene.Badge.GetTransformedBounds()!.Value;
            var renderedBounds = transformed.Bounds.TransformToAABB(transformed.Transform);
            renderedBounds.Intersect(transformed.Clip).Should().Be(renderedBounds);
        }

        foreach (var rtlOwner in new Control[] { scene.Host, scene.Badged })
        {
            rtlOwner.FlowDirection = FlowDirection.RightToLeft;
            scene.Layout();
            var rtlAnchor = scene.BoundsInWindow(scene.Anchor);
            var rtlBadge = scene.BoundsInWindow(scene.Badge);
            rtlBadge.Right.Should().Be(rtlAnchor.Left + 12);
            rtlBadge.Bottom.Should().Be(rtlAnchor.Top + 14);
            scene.Badge.Variant = BadgeVariant.Small;
            scene.Layout();
            scene.BoundsInWindow(scene.Badge).Should().Be(new Rect(rtlAnchor.Left, rtlAnchor.Top, 6, 6));
            scene.Badge.Variant = BadgeVariant.Auto;
            rtlOwner.FlowDirection = FlowDirection.LeftToRight;
            scene.Layout();
        }

        scene.Badge.Count = 0;
        scene.Layout();
        scene.Badged.DesiredSize.Should().Be(size);
        scene.BoundsInWindow(scene.Anchor).Should().Be(anchor);
    }

    [Fact]
    public void LocalTokens_ShouldUpdateGeometryAndKeepBadgeTypographyIsolated()
    {
        using var scene = new BadgeScene();
        var badge = scene.Badge;
        Typography.SetTextStyle(scene.Host, MdTextStyle.DisplayLarge);
        badge.Count = 7;
        scene.Badged.Resources["MdCompBadgeLargeBrush"] = new TokenAlias("CustomBadgeBrush");
        scene.Badged.Resources["CustomBadgeBrush"] = Brushes.Blue;
        scene.Badged.Resources["MdCompBadgeLargeSize"] = 20d;
        scene.Badged.Resources["MdImplBadgeLargeOverlap"] = new Vector(10, 18);
        scene.Layout();
        badge.Background.Should().BeSameAs(Brushes.Blue);
        badge.Bounds.Height.Should().Be(20);
        var text = badge.Presenter!.Child.Should().BeOfType<TextBlock>().Subject;
        text.FontSize.Should().Be(11);
        text.LineHeight.Should().Be(16);
        Typography.GetTextStyle(scene.Anchor).Should().Be(MdTextStyle.DisplayLarge);
        scene.BoundsInWindow(badge).Left.Should().Be(scene.BoundsInWindow(scene.Anchor).Right - 10);
        scene.BoundsInWindow(badge).Bottom.Should().Be(scene.BoundsInWindow(scene.Anchor).Top + 18);

        scene.Badged.Resources["CustomBadgeBrush"] = Brushes.Green;
        scene.Badged.Resources["MdCompBadgeLargeLabelTextSize"] = 13d;
        scene.Layout();
        badge.Background.Should().BeSameAs(Brushes.Green);
        text.FontSize.Should().Be(13);
        badge.Background = Brushes.Red;
        scene.Badged.Resources["CustomBadgeBrush"] = Brushes.Yellow;
        scene.Layout();
        badge.Background.Should().BeSameAs(Brushes.Red);

        scene.Badged.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
        {
            ["VariantBadgeBrush"] = Brushes.Purple
        };
        scene.Badged.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["VariantBadgeBrush"] = Brushes.Orange
        };
        badge.ClearValue(ContentControl.BackgroundProperty);
        scene.Badged.Resources["MdCompBadgeLargeBrush"] = new TokenAlias("VariantBadgeBrush");
        scene.Window.RequestedThemeVariant = ThemeVariant.Light;
        scene.Layout();
        badge.Background.Should().BeSameAs(Brushes.Purple);
        scene.Window.RequestedThemeVariant = ThemeVariant.Dark;
        scene.Layout();
        badge.Background.Should().BeSameAs(Brushes.Orange);
    }

    [Fact]
    public void ReplacementAndTemplates_ShouldPreserveLogicalScopeAndBindings()
    {
        using var scene = new BadgeScene();
        var previous = scene.Badge;
        scene.Badged.DataContext = new TextBlock { Text = "New" };
        var replacement = new Badge();
        replacement.Bind(ContentControl.ContentProperty, new Binding("Text"));
        replacement.ContentTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Text = value });
        scene.Badged.Badge = replacement;
        scene.Layout();
        previous.Parent.Should().BeNull();
        replacement.Parent.Should().BeSameAs(scene.Badged);
        replacement.Presenter!.Child.Should().BeOfType<TextBlock>().Which.Text.Should().Be("New");
        ControlAutomationPeer.CreatePeerForElement(replacement).GetName().Should().Be("New");
        scene.Badged.Badge = null;
        scene.Layout();
        replacement.Parent.Should().BeNull();
        scene.Badged.DesiredSize.Should().Be(new Size(24, 24));
    }

    [Fact]
    public void Badge_ShouldNotConsumePointerOrKeyboardInput()
    {
        using var scene = new BadgeScene();
        scene.Host.Children.Clear();
        var button = new Button { Content = scene.Badged, Padding = new Thickness(20) };
        scene.Host.Children.Add(button);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        scene.Badge.Count = 8;
        scene.Layout();
        var point = scene.BoundsInWindow(scene.Badge).Center;
        scene.Window.MouseDown(point, MouseButton.Left);
        scene.Window.MouseUp(point, MouseButton.Left);
        clicks.Should().Be(1);
        scene.Badge.Focus().Should().BeFalse();
        button.Focus();
        scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        scene.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        clicks.Should().Be(2);
        button.IsEnabled = false;
        scene.Window.MouseDown(point, MouseButton.Left);
        scene.Window.MouseUp(point, MouseButton.Left);
        clicks.Should().Be(2);
    }

    [Fact]
    public void Accessibility_ShouldDescribeFullCountOrLocalizedDotWithoutDuplicateLabelChildren()
    {
        using var scene = new BadgeScene();
        var badge = scene.Badge;
        var peer = ControlAutomationPeer.CreatePeerForElement(badge);
        scene.Badged.Resources["StringBadgeNewNotificationText"] = "Новое уведомление";
        peer.GetName().Should().Be("Новое уведомление");
        badge.Count = 1200;
        scene.Layout();
        peer.GetName().Should().Be("1200");
        peer.GetChildren().Should().BeEmpty();
        AutomationProperties.SetName(badge, "Непрочитанные сообщения");
        badge.Count = 1201;
        peer.GetName().Should().Be("Непрочитанные сообщения");
        AutomationProperties.SetAccessibilityView(badge, AccessibilityView.Raw);
        peer.IsControlElement().Should().BeFalse();
        peer.IsContentElement().Should().BeFalse();
    }

    private sealed class BadgeScene : IDisposable
    {
        public Badge Badge { get; } = new();
        public Border Anchor { get; } = new() { Width = 24, Height = 24, Background = Brushes.Gray };
        public Badged Badged { get; }
        public Grid Host { get; } = new();
        public Window Window { get; }

        public BadgeScene()
        {
            TestApp.EnsureStarted();
            Host.Resources.MergedDictionaries.Add((IResourceDictionary)AvaloniaXamlLoader.Load(
                new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml")));
            Badged = new Badged
            {
                Content = Anchor, Badge = Badge,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Host.Children.Add(Badged);
            Window = new Window
                { Width = 300, Height = 160, WindowDecorations = WindowDecorations.None, Content = Host };
            Window.Show();
            Layout();
        }

        public Rect BoundsInWindow(Control control) =>
            new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(Window)!.Value);

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }

        public void Dispose() => Window.Close();
    }
}