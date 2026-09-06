using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Tokens;
using Material3.Avalonia.Theme;

namespace Material3.Avalonia.Tests.Controls;

public sealed class ToolTipTests : IDisposable
{
    private readonly List<Window> _windows = [];
    private readonly bool _reducedMotion;
    private readonly IResourceDictionary _resources;
    private readonly MaterialTheme _theme;

    public ToolTipTests()
    {
        TestApp.EnsureStarted();
        _resources = (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"));
        Application.Current!.Resources.MergedDictionaries.Add(_resources);
        _theme = new MaterialTheme { Mode = ThemeMode.Light, MotionScheme = null };
        Application.Current.Styles.Add(_theme);
        _reducedMotion = MotionSettings.ReduceMotion;
        MotionSettings.ReduceMotion = true;
    }

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();
        Dispatcher.UIThread.RunJobs();
        MotionSettings.ReduceMotion = _reducedMotion;
        Application.Current!.Resources.MergedDictionaries.Remove(_resources);
        Application.Current.Styles.Remove(_theme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichToolTip_ShouldOpenOnFirstHover_WithEitherVariantSyntax(bool useClass)
    {
        var flyout = new Flyout { Content = "Explanation" };
        if (useClass)
            flyout.FlyoutPresenterClasses.Add("rich-tooltip");
        else
            FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        var action = new Button { Content = "Details" };
        FlyoutAssist.SetSubhead(flyout, "Title");
        FlyoutAssist.SetActions(flyout, action);
        var button = new Button { Content = "Hover", Flyout = flyout };
        var window = Show(button);

        Hover(window, button);

        flyout.IsOpen.Should().BeTrue();
        flyout.ShowMode.Should().Be(FlyoutShowMode.Transient);
        var presenter = flyout.Popup.Child.Should().BeOfType<FlyoutPresenter>().Subject;
        FlyoutAssist.GetVariant(presenter).Should().Be(FlyoutVariant.RichToolTip);
        presenter.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text)
            .Should().Contain(["Title", "Explanation", "Details"]);
        ButtonAssist.GetVariant(action).Should().Be(ButtonVariant.Text);
        action.FontSize.Should().Be(14);
        action.IsKeyboardFocusWithin.Should().BeFalse();
    }

    [Fact]
    public void StandardVariant_ShouldOverrideClass_AndSlotsShouldNotSelectRichToolTip()
    {
        var flyout = new Flyout { Content = "Ordinary content" };
        flyout.FlyoutPresenterClasses.Add("rich-tooltip");
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.Standard);
        FlyoutAssist.SetSubhead(flyout, "Unused title");
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Show(button);

        Hover(window, button);
        flyout.IsOpen.Should().BeFalse();
        flyout.ShowAt(button);
        Pump();
        FlyoutAssist.GetVariant(flyout.Popup.Child!).Should().Be(FlyoutVariant.Standard);
        flyout.Popup.Child!.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text)
            .Should().NotContain("Unused title");

        flyout.Hide();
        flyout.FlyoutPresenterClasses.Clear();
        flyout.ClearValue(FlyoutAssist.VariantProperty);
        Hover(window, button);
        flyout.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RichToolTip_ShouldPassInputToAnchor_AndRemainOpenOnStationaryHover(bool overlay, bool useClass)
    {
        var flyout = new Flyout { Content = "Explanation" };
        if (useClass)
            flyout.FlyoutPresenterClasses.Add("rich-tooltip");
        else
            FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Hover", Flyout = flyout };
        ToolTipAssist.SetHideDelay(button, 25);
        var window = Show(button);
        var openings = 0;
        flyout.Opened += (_, _) => openings++;
        for (var i = 0; i < 2; i++)
        {
            Hover(window, button);
            flyout.Popup.OverlayInputPassThroughElement.Should().BeSameAs(window);
            Pump(100);
            flyout.IsOpen.Should().BeTrue();
            openings.Should().Be(i + 1);
            window.MouseMove(new Point(450, 250));
            Pump(100);
            flyout.IsOpen.Should().BeFalse();
        }
    }

    [Fact]
    public void PlainToolTip_ShouldDelayDismissal_CancelItOnReentry_AndCloseOnEscape()
    {
        var tip = new ToolTip { Content = "Save" };
        var button = new Button { Content = "Hover" };
        ToolTip.SetTip(button, tip);
        ToolTipAssist.SetHideDelay(button, 40);
        var window = Show(button);

        Hover(window, button);
        ToolTip.GetIsOpen(button).Should().BeTrue();
        tip.FontSize.Should().Be(12);
        window.MouseMove(new Point(450, 250));
        Pump();
        ToolTip.GetIsOpen(button).Should().BeTrue();
        Hover(window, button);
        Pump(80);
        ToolTip.GetIsOpen(button).Should().BeTrue();

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump();
        ToolTip.GetIsOpen(button).Should().BeFalse();
        window.MouseMove(new Point(450, 250));
        Hover(window, button);
        ToolTip.GetIsOpen(button).Should().BeTrue();
        window.MouseMove(new Point(450, 250));
        Pump(80);
        ToolTip.GetIsOpen(button).Should().BeFalse();
    }

    [Fact]
    public void BehaviorOptOut_ShouldRestoreNativeBindingsAndPlacement()
    {
        var source = new ToggleButton { IsChecked = true };
        var button = new Button { Content = "Hover" };
        ToolTip.SetTip(button, "Hint");
        button.Bind(ToolTip.ServiceEnabledProperty,
            new Binding(nameof(ToggleButton.IsChecked)) { Source = source });
        ToolTip.SetPlacement(button, PlacementMode.Right);
        ToolTip.SetVerticalOffset(button, 17);
        var window = Show(button);

        ToolTip.GetServiceEnabled(button).Should().BeFalse();
        ToolTip.GetPlacement(button).Should().Be(PlacementMode.Right);
        ToolTip.GetVerticalOffset(button).Should().Be(17);

        ToolTipAssist.SetIsMaterialBehaviorEnabled(window, false);
        Pump();
        ToolTip.GetServiceEnabled(button).Should().BeTrue();
        source.IsChecked = false;
        ToolTip.GetServiceEnabled(button).Should().BeFalse();
        source.IsChecked = true;
        ToolTip.GetServiceEnabled(button).Should().BeTrue();
    }

    [Fact]
    public void PersistentRichToolTip_ShouldIgnoreHoverAndPointerExit_AndKeepActionsInteractive()
    {
        var action = new Button { Content = "Dismiss" };
        var flyout = new Flyout { Content = "Explanation", ShowMode = FlyoutShowMode.Standard };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        FlyoutAssist.SetActions(flyout, action);
        var clicks = 0;
        action.Click += (_, _) =>
        {
            clicks++;
            flyout.Hide();
        };
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Show(button);
        ToolTipAssist.SetHideDelay(button, 0);

        Hover(window, button);
        flyout.IsOpen.Should().BeFalse();
        flyout.ShowAt(button);
        Pump();
        action.IsFocused.Should().BeTrue();
        window.MouseMove(new Point(450, 250));
        Pump(30);
        flyout.IsOpen.Should().BeTrue();
        action.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        Pump();
        clicks.Should().Be(1);
        flyout.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void OpenToolTip_ShouldResolveLocalAliasesAndRuntimeChanges()
    {
        var tip = new ToolTip { Content = "Hint" };
        var button = new Button { Content = "Hover" };
        ToolTip.SetTip(button, tip);
        button.Resources["CustomToolTipBrush"] = Brushes.Red;
        button.Resources["MdCompPlainToolTipContainerBrush"] = new TokenAlias("CustomToolTipBrush");
        var window = Show(button);
        Hover(window, button);
        tip.Background.Should().BeSameAs(Brushes.Red);

        button.Resources["CustomToolTipBrush"] = Brushes.Blue;
        Pump();
        tip.Background.Should().BeSameAs(Brushes.Blue);
    }

    [Fact]
    public void LeavingOrRemovingAnchor_ShouldCancelPendingShow()
    {
        var button = new Button { Content = "Hover" };
        ToolTip.SetTip(button, "Hint");
        var window = Show(button);
        ToolTip.SetShowDelay(button, 40);
        Hover(window, button);
        window.MouseMove(new Point(450, 250));
        Pump(80);
        ToolTip.GetIsOpen(button).Should().BeFalse();

        Hover(window, button);
        ((Panel)window.Content!).Children.Remove(button);
        Pump(80);
        ToolTip.GetIsOpen(button).Should().BeFalse();
        ToolTip.GetServiceEnabled(button).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichToolTip_ShouldKeepPopupInteractive_AndDismissAfterLeavingBothRoots(bool overlay)
    {
        var action = new Button { Content = "Details" };
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        FlyoutAssist.SetActions(flyout, action);
        flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Hover", Flyout = flyout };
        ToolTipAssist.SetHideDelay(button, 40);
        var window = Show(button);
        Hover(window, button);

        var presenter = (FlyoutPresenter)flyout.Popup.Child!;
        var popupRoot = TopLevel.GetTopLevel(presenter)!;
        window.MouseMove(new Point(450, 250));
        Hover(popupRoot, presenter);
        Pump(80);
        flyout.IsOpen.Should().BeTrue();

        action.Focus(NavigationMethod.Tab);
        popupRoot.MouseMove(new Point(-20, -20));
        Pump(80);
        flyout.IsOpen.Should().BeTrue();
        button.Focus(NavigationMethod.Pointer);
        window.MouseMove(new Point(450, 250));
        Pump(80);
        flyout.IsOpen.Should().BeFalse();

        Hover(window, button);
        flyout.IsOpen.Should().BeTrue();
        ((Panel)window.Content!).Children.Remove(button);
        Pump();
        flyout.IsOpen.Should().BeFalse();
        flyout.ShowMode.Should().Be(FlyoutShowMode.Standard);
        ((Panel)window.Content!).Children.Add(button);
        Pump();
        window.MouseMove(new Point(450, 250));
        Hover(window, button);
        flyout.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichToolTip_ShouldAllowTabIntoActions_AndBackToWindow(bool overlay)
    {
        var first = new Button { Content = "First" };
        var last = new Button { Content = "Last" };
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        FlyoutAssist.SetActions(flyout, new StackPanel { Children = { first, last } });
        flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Anchor", Flyout = flyout };
        var window = Show(button);
        var following = new Button
        {
            Content = "Following", Margin = new Thickness(300, 100, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
        };
        ((Panel)window.Content!).Children.Add(following);
        Pump();

        button.Focus(NavigationMethod.Tab);
        Pump();
        flyout.IsOpen.Should().BeTrue();
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        first.IsFocused.Should().BeTrue();
        var popupRoot = TopLevel.GetTopLevel(first)!;
        popupRoot.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        last.IsFocused.Should().BeTrue();
        popupRoot.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        following.IsFocused.Should().BeTrue();
        flyout.IsOpen.Should().BeFalse();

        button.Focus(NavigationMethod.Tab);
        Pump();
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        TopLevel.GetTopLevel(first)!.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        button.IsFocused.Should().BeTrue();
        flyout.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ProgrammaticRichToolTip_ShouldSupportEscapeWithoutAttachedFlyout()
    {
        var anchor = new Button { Content = "Anchor" };
        var window = Show(anchor);
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);

        flyout.ShowAt(anchor);
        Pump();
        flyout.ShowMode.Should().Be(FlyoutShowMode.Transient);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        flyout.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ProgrammaticRichToolTip_ShouldReleaseAnchorAfterClosing(bool overlay, bool cancel)
    {
        var button = new Button { Content = "Anchor" };
        ToolTip.SetTip(button, "Plain hint");
        var window = Show(button);
        var programmatic = new Flyout { Content = "Programmatic" };
        FlyoutAssist.SetVariant(programmatic, FlyoutVariant.RichToolTip);
        programmatic.Popup.ShouldUseOverlayLayer = overlay;
        programmatic.Opening += (_, e) => ((System.ComponentModel.CancelEventArgs)e).Cancel = cancel;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            programmatic.ShowAt(button);
            Pump();
            programmatic.IsOpen.Should().Be(!cancel);
            programmatic.Hide();
            Pump();
            Hover(window, button);
            ToolTip.GetIsOpen(button).Should().BeTrue();
            window.MouseMove(new Point(490, 290));
            ToolTip.SetIsOpen(button, false);
            Pump();
        }

        var assigned = new Flyout { Content = "Assigned" };
        FlyoutAssist.SetVariant(assigned, FlyoutVariant.RichToolTip);
        assigned.Popup.ShouldUseOverlayLayer = overlay;
        button.Flyout = assigned;
        Hover(window, button);
        assigned.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpenRichToolTip_ShouldUpdateGap_AndPreserveCustomPlacement(bool overlay)
    {
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Anchor", Flyout = flyout };
        button.Resources["MdImplRichToolTipAnchorGap"] = 8d;
        var window = Show(button);
        flyout.ShowAt(button);
        Pump();
        var surface = flyout.Popup.Child!.GetVisualDescendants().OfType<Control>()
            .Single(x => x.Name == "PART_Surface");

        double Gap() => (surface.PointToScreen(default).Y -
                         button.PointToScreen(new Point(0, button.Bounds.Height)).Y) / window.RenderScaling;

        Gap().Should().BeApproximately(8, 0.01);
        var anchorSource = new Popup
            { PlacementAnchor = global::Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.Left };
        flyout.Popup.Bind(Popup.PlacementAnchorProperty,
            new Binding(nameof(Popup.PlacementAnchor)) { Source = anchorSource });
        button.Resources["MdImplRichToolTipAnchorGap"] = 40d;
        Pump();
        flyout.IsOpen.Should().BeTrue();
        Gap().Should().BeApproximately(40, 0.01);
        button.Resources["MdImplRichToolTipAnchorGap"] = 16d;
        Pump();
        Gap().Should().BeApproximately(16, 0.01);
        flyout.Popup.PlacementAnchor.Should().Be(anchorSource.PlacementAnchor);
        anchorSource.PlacementAnchor = global::Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.Right;
        Pump();
        flyout.Popup.PlacementAnchor.Should().Be(anchorSource.PlacementAnchor);

        var source = new Popup { CustomPopupPlacementCallback = placement => placement.Offset = new Point(20, 20) };
        flyout.Popup.Bind(Popup.CustomPopupPlacementCallbackProperty,
            new Binding(nameof(Popup.CustomPopupPlacementCallback)) { Source = source });
        Pump();
        button.Resources["MdImplRichToolTipAnchorGap"] = 24d;
        Pump();
        flyout.Popup.CustomPopupPlacementCallback.Should().BeSameAs(source.CustomPopupPlacementCallback);
        source.CustomPopupPlacementCallback = placement => placement.Offset = new Point(30, 30);
        Pump();
        flyout.Popup.CustomPopupPlacementCallback.Should().BeSameAs(source.CustomPopupPlacementCallback);
    }

    [Fact]
    public void DisabledToolTip_AndWindowCoordination_ShouldPreserveNativeContracts()
    {
        var disabled = new Button { Content = "Disabled", IsEnabled = false };
        ToolTip.SetTip(disabled, "Disabled hint");
        var window = Show(disabled);
        Hover(window, disabled);
        ToolTip.GetIsOpen(disabled).Should().BeFalse();
        window.MouseMove(new Point(450, 250));
        ToolTip.SetShowOnDisabled(disabled, true);
        Hover(window, disabled);
        ToolTip.GetIsOpen(disabled).Should().BeTrue();

        var second = new Button
        {
            Content = "Second", Margin = new Thickness(300, 100, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
        };
        ToolTip.SetTip(second, "Second hint");
        ToolTip.SetShowDelay(second, 1000);
        ((Panel)window.Content!).Children.Add(second);
        Pump();
        Hover(window, second);
        ToolTip.GetIsOpen(disabled).Should().BeFalse();
        ToolTip.GetIsOpen(second).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichToolTip_ShouldUpdateThemeAndSlots_AndContainItsShadow(bool overlay)
    {
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Anchor", Flyout = flyout };
        var window = Show(button);
        Hover(window, button);
        var presenter = (FlyoutPresenter)flyout.Popup.Child!;
        var light = ((ISolidColorBrush)presenter.Background!).Color;

        _theme.Mode = ThemeMode.Dark;
        FlyoutAssist.SetSubhead(flyout, "Updated title");
        Pump();
        ((ISolidColorBrush)presenter.Background!).Color.Should().NotBe(light);
        presenter.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text).Should().Contain("Updated title");

        var surface = presenter.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Surface");
        var shadows = (BoxShadows)surface.GetValue(Border.BoxShadowProperty)!;
        shadows.Count.Should().BeGreaterThan(0);
        var bounds = new Rect(presenter.Bounds.Size);
        foreach (var shadow in shadows)
            bounds.Contains(shadow.TransformBounds(surface.Bounds)).Should().BeTrue();
    }

    [Fact]
    public void Holding_ShouldNotConsumeGesture_AndShouldYieldToContextMenus()
    {
        var button = new Button { Content = "Hold" };
        ToolTip.SetTip(button, "Hint");
        ToolTipAssist.SetHideDelay(button, 20);
        Show(button);
        var holding = Holding(HoldingState.Started);
        button.RaiseEvent(holding);
        ToolTip.GetIsOpen(button).Should().BeTrue();
        holding.Handled.Should().BeFalse();
        button.RaiseEvent(Holding(HoldingState.Completed));
        Pump(50);
        ToolTip.GetIsOpen(button).Should().BeFalse();

        button.ContextMenu = new ContextMenu();
        button.RaiseEvent(Holding(HoldingState.Started));
        ToolTip.GetIsOpen(button).Should().BeFalse();
    }

    private static HoldingRoutedEventArgs Holding(HoldingState state)
    {
        // Avalonia exposes the routed event but keeps its event-args constructor internal.
        return (HoldingRoutedEventArgs)Activator.CreateInstance(typeof(HoldingRoutedEventArgs),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object?[] { state, default(Point), PointerType.Touch, null }, null)!;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HoverAppearance_ShouldRemainUntilToolTipCloses_WithoutChangingNativePointerOver(bool rich)
    {
        var button = new Button { Content = "Hover" };
        if (rich)
        {
            button.Flyout = new Flyout { Content = "Explanation" };
            FlyoutAssist.SetVariant(button.Flyout, FlyoutVariant.RichToolTip);
        }
        else
            ToolTip.SetTip(button, "Hint");

        ToolTipAssist.SetHideDelay(button, 40);
        var window = Show(button);
        Hover(window, button);
        window.MouseMove(new Point(450, 250));
        Pump();
        button.IsPointerOver.Should().BeFalse();
        button.Classes.Should().Contain("m3-hovered");
        ToolTipAssist.SetKeepAnchorHovered(button, false);
        button.Classes.Should().NotContain("m3-hovered");
        ToolTipAssist.SetKeepAnchorHovered(button, true);
        button.Classes.Should().Contain("m3-hovered");
        Pump(80);
        button.Classes.Should().NotContain("m3-hovered");
    }

    [Fact]
    public void TriggerSelection_ShouldKeepKeyboardAndManualOpeningIndependentOfHover()
    {
        var button = new Button { Content = "Focus" };
        ToolTip.SetTip(button, "Hint");
        var window = Show(button);
        ToolTipAssist.SetShowTriggers(window, ToolTipTriggers.Focus);
        Hover(window, button);
        ToolTip.GetIsOpen(button).Should().BeFalse();
        button.Focus(NavigationMethod.Tab);
        Pump();
        ToolTip.GetIsOpen(button).Should().BeTrue();
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        ToolTipAssist.SetShowTriggers(window, ToolTipTriggers.None);
        button.RaiseEvent(Holding(HoldingState.Started));
        ToolTip.GetIsOpen(button).Should().BeFalse();
        ToolTip.SetIsOpen(button, true);
        Pump();
        ToolTip.GetIsOpen(button).Should().BeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PlainPlacement_ShouldUseThemeGap_AndPreserveItWhenFlipped(bool text, bool flip)
    {
        Control anchor = text ? new TextBlock { Text = "Text anchor" } : new Button { Content = "Button anchor" };
        var tip = new ToolTip { Content = "Hint" };
        ToolTip.SetTip(anchor, tip);
        ToolTip.SetShouldUseOverlayLayer(anchor, true);
        var window = Show(anchor);
        if (flip)
        {
            anchor.Margin = new Thickness(100, 0, 0, 0);
            Pump();
        }

        ToolTip.SetIsOpen(anchor, true);
        Pump();
        ToolTip.GetIsOpen(anchor).Should().BeTrue(
            $"anchor={anchor.Bounds}, hitVisible={anchor.IsHitTestVisible}, hit={window.InputHitTest(anchor.TranslatePoint(new Point(anchor.Bounds.Width / 2, anchor.Bounds.Height / 2), window)!.Value)?.GetType().Name}, hover={anchor.IsPointerOver}, enabled={ToolTipAssist.GetIsMaterialBehaviorEnabled(anchor)}, delay={ToolTip.GetShowDelay(anchor)}");
        var target = anchor.PointToScreen(default);
        var popup = tip.PointToScreen(default);
        var gap = flip ? popup.Y - target.Y - anchor.Bounds.Height : target.Y - popup.Y - tip.Bounds.Height;
        var actualPopup = tip.FindLogicalAncestorOfType<Popup>()!;
        gap.Should().BeApproximately(text ? 8 : 4, 1,
            $"placement={actualPopup.Placement}, rect={actualPopup.PlacementRect}, offset={actualPopup.VerticalOffset}, ownerOffset={ToolTip.GetVerticalOffset(anchor)}, anchor={actualPopup.PlacementAnchor}, gravity={actualPopup.PlacementGravity}");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PlainPlacement_ShouldRememberFlippedSide_ForAnimation(bool overlay, bool preferTop)
    {
        var anchor = new Button { Content = "Anchor", Height = 40 };
        var tip = new ToolTip { Content = "Hint" };
        ToolTip.SetTip(anchor, tip);
        ToolTip.SetPlacement(anchor, preferTop ? PlacementMode.Top : PlacementMode.Bottom);
        ToolTip.SetShouldUseOverlayLayer(anchor, overlay);
        var window = Show(anchor);
        anchor.Margin = new Thickness(100, preferTop ? 0 : 260, 0, 0);
        if (!overlay && window.Screens.ScreenFromWindow(window) is { } screen)
        {
            var area = screen.WorkingArea;
            window.Position = new PixelPoint(area.X, preferTop ? area.Y : area.Bottom - 300);
        }

        Pump();
        ToolTip.SetIsOpen(anchor, true);
        Pump();
        var popup = tip.FindLogicalAncestorOfType<Popup>()!;
        var placement = typeof(ToolTipAssist).Assembly.GetType(
            "Material3.Avalonia.Attached.Controls.Internal.ToolTipPlacement")!;
        var remembered = (PlacementMode)placement.GetMethod("GetRequestedDirection")!.Invoke(null, [popup])!;
        remembered.Should().Be(preferTop ? PlacementMode.Bottom : PlacementMode.Top);
        var anchorPoint = anchor.PointToScreen(default);
        var tipPoint = tip.PointToScreen(default);
        var gap = preferTop
            ? tipPoint.Y - anchorPoint.Y - anchor.Bounds.Height * window.RenderScaling
            : anchorPoint.Y - tipPoint.Y - tip.Bounds.Height * window.RenderScaling;
        gap.Should().BeApproximately(4 * window.RenderScaling, 1);
        ToolTip.GetPlacement(anchor).Should().Be(preferTop ? PlacementMode.Top : PlacementMode.Bottom);
    }

    [Fact]
    public void AppBarStyle_AndExplicitOffset_ShouldOverridePlainDefaults()
    {
        var anchor = new TextBlock { Text = "Title" };
        var tip = new ToolTip { Content = "Hint" };
        ToolTip.SetTip(anchor, tip);
        ToolTip.SetShouldUseOverlayLayer(anchor, true);
        var window = Show(anchor);
        ((Control)window.Content!).Styles.Add(new Style(x => x.Is<Control>())
        {
            Setters = { new Setter(ToolTip.PlacementProperty, PlacementMode.Bottom) }
        });
        ToolTip.SetIsOpen(anchor, true);
        Pump();
        ToolTip.GetPlacement(anchor).Should().Be(PlacementMode.Bottom);
        var gap = tip.PointToScreen(default).Y - anchor.PointToScreen(default).Y - anchor.Bounds.Height;
        gap.Should().BeApproximately(8, 1);
        ToolTip.SetIsOpen(anchor, false);
        ToolTip.SetVerticalOffset(anchor, 19);
        ToolTip.SetIsOpen(anchor, true);
        Pump();
        gap = tip.PointToScreen(default).Y - anchor.PointToScreen(default).Y - anchor.Bounds.Height;
        gap.Should().BeApproximately(19, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichPlacement_ShouldPreferBottomEnd_AndStayInsideOverlay(bool rtl)
    {
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = true;
        var anchor = new Button
        {
            Content = "Anchor", Flyout = flyout,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
        var window = Show(anchor);
        anchor.Margin = new Thickness(220, 100, 0, 0);
        Pump();
        Hover(window, anchor);
        var presenter = (Control)flyout.Popup.Child!;
        var surface = presenter.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Surface");
        var surfaceBounds = new Rect(surface.Bounds.Size).TransformToAABB(surface.TransformToVisual(window)!.Value);
        var position = surfaceBounds.Position;
        position.Y.Should().BeApproximately(anchor.Bounds.Bottom + 8, 1);
        if (rtl)
            surfaceBounds.Right.Should().BeApproximately(anchor.Bounds.Left, 1);
        else
            position.X.Should().BeApproximately(anchor.Bounds.Right, 1);
        new Rect(window.ClientSize)
            .Contains(new Rect(presenter.TranslatePoint(default, window)!.Value, presenter.Bounds.Size))
            .Should().BeTrue();
    }

    [Fact]
    public void Reveal_ShouldClipStationaryContent_WithoutChangingOpacityOrLayout()
    {
        var tip = new ToolTip { Content = "A longer explanation that occupies multiple lines of fixed text." };
        var anchor = new Button { Content = "Anchor" };
        ToolTip.SetTip(anchor, tip);
        ToolTip.SetShouldUseOverlayLayer(anchor, true);
        var window = Show(anchor);
        Hover(window, anchor);
        var surface = tip.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Surface");
        var content = surface.Child!;
        var bounds = content.Bounds;
        var progress = AvaloniaPropertyRegistry.Instance.FindRegistered(surface, "Progress")!;
        surface.Transitions = null;
        surface.SetValue(progress, 0.25d);
        var clipped = content.Clip!.Bounds;
        surface.SetValue(progress, 0.75d);
        content.Bounds.Should().Be(bounds);
        content.Opacity.Should().Be(1);
        surface.Opacity.Should().Be(1);
        content.Clip!.Bounds.Height.Should().BeGreaterThan(clipped.Height);
        // An above-anchor tooltip grows from its bottom edge, towards the top.
        content.Clip.Bounds.Bottom.Should().BeApproximately(clipped.Bottom, 0.01);
    }

    [Fact]
    public void TextFieldHover_ShouldFollowSelectionCapture_AndDisabledState()
    {
        var field = new TextBox { Text = "Select this text", Width = 220 };
        var window = Show(field);
        Hover(window, field);
        field.Classes.Should().Contain("m3-hovered");
        var inside = field.TranslatePoint(new Point(30, 25), window)!.Value;
        window.MouseDown(inside, MouseButton.Left);
        window.MouseMove(new Point(490, 290));
        Pump();
        field.Classes.Should().NotContain("m3-hovered");
        window.MouseMove(inside);
        window.MouseUp(inside, MouseButton.Left);
        Pump();
        field.Classes.Should().Contain("m3-hovered");
        field.IsEnabled = false;
        field.Classes.Should().NotContain("m3-hovered");
    }

    [Fact]
    public void TextAnchorGap_ShouldUpdateFromLocalTokens_AndPreserveOffsetBinding()
    {
        var anchor = new TextBlock { Text = "Anchor" };
        ToolTip.SetTip(anchor, new ToolTip { Content = "Tip" });
        ToolTip.SetShouldUseOverlayLayer(anchor, true);
        Show(anchor);
        ToolTip.GetVerticalOffset(anchor).Should().Be(-8);
        anchor.Resources["MdImplPlainToolTipUnboundedAnchorGap"] = 12d;
        Pump();
        ToolTip.GetVerticalOffset(anchor).Should().Be(-12);
        var source = new Slider { Value = 21 };
        anchor.Bind(ToolTip.VerticalOffsetProperty, new Binding(nameof(Slider.Value)) { Source = source });
        ToolTip.SetIsOpen(anchor, true);
        Pump();
        source.Value = 23;
        Pump();
        ToolTip.GetVerticalOffset(anchor).Should().Be(23);
        ToolTip.SetIsOpen(anchor, false);
        source.Value = 25;
        ToolTip.GetVerticalOffset(anchor).Should().Be(25);
    }

    [Fact]
    public void CanceledRichOpening_ShouldReleasePassThrough_AndAllowRetry()
    {
        var flyout = new Flyout { Content = "Tip" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        var button = new Button { Content = "Anchor", Flyout = flyout };
        Show(button);
        var cancel = true;
        flyout.Opening += (_, e) => ((System.ComponentModel.CancelEventArgs)e).Cancel = cancel;
        flyout.ShowAt(button);
        Pump();
        flyout.IsOpen.Should().BeFalse();
        flyout.Popup.OverlayInputPassThroughElement.Should().BeNull();
        cancel = false;
        flyout.ShowAt(button);
        Pump();
        flyout.IsOpen.Should().BeTrue();
        flyout.Hide();
        flyout.Popup.OverlayInputPassThroughElement.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RichNearViewportEdge_ShouldFitWithoutCoveringAnchor(bool rtl)
    {
        var flyout = new Flyout { Content = "An explanation near the window boundary." };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Popup.ShouldUseOverlayLayer = true;
        var button = new Button
            { Content = "Edge", FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        button.Flyout = flyout;
        var window = Show(button);
        button.Margin = new Thickness(420, 240, 0, 0);
        Pump();
        flyout.ShowAt(button);
        Pump();
        var presenter = flyout.Popup.Child!;
        var bounds = new Rect(presenter.Bounds.Size).TransformToAABB(presenter.TransformToVisual(window)!.Value);
        var anchor = new Rect(button.Bounds.Size).TransformToAABB(button.TransformToVisual(window)!.Value);
        new Rect(window.ClientSize).Contains(bounds).Should().BeTrue();
        var surface = presenter.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Surface");
        var visible = new Rect(surface.Bounds.Size).TransformToAABB(surface.TransformToVisual(window)!.Value);
        visible.Intersects(anchor).Should().BeFalse();
    }

    [Fact]
    public void TextFieldClick_ShouldNotTemporarilyDropHover()
    {
        var field = new TextBox { Text = "Click this text", Width = 220 };
        var window = Show(field);
        Hover(window, field);
        var states = new List<bool>();
        field.Classes.CollectionChanged += (_, _) => states.Add(field.Classes.Contains("m3-hovered"));
        var inside = field.TranslatePoint(new Point(30, 25), window)!.Value;
        window.MouseDown(inside, MouseButton.Left);
        Pump();
        window.MouseUp(inside, MouseButton.Left);
        Pump();
        states.Should().NotContain(false);
    }

    [Fact]
    public void KeyboardFocus_ShouldOpenWithoutHoverDelay()
    {
        var button = new Button { Content = "Focus" };
        ToolTip.SetTip(button, "Hint");
        Show(button);
        ToolTip.SetShowDelay(button, 1000);
        button.Focus(NavigationMethod.Tab);
        Pump();
        ToolTip.GetIsOpen(button).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelledHover_ShouldAllowKeyboardOpening_AfterClickingAnotherControl(bool overlay)
    {
        var previous = new Button { Content = "Previous" };
        var flyout = new Flyout { Content = "Explanation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Subhead and body", Flyout = flyout };
        var window = Show(new StackPanel { Children = { previous, button } });
        ToolTip.SetShowDelay(button, 10000);

        Hover(window, button);
        flyout.IsOpen.Should().BeFalse();
        window.MouseMove(new Point(490, 290));
        Pump();
        Hover(window, previous);
        var point = previous.TranslatePoint(new Point(previous.Bounds.Width / 2, previous.Bounds.Height / 2), window)!
            .Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Pump();
        previous.IsFocused.Should().BeTrue();

        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Pump();

        button.IsFocused.Should().BeTrue();
        flyout.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimatedToolTip_ShouldRemainRevealed_AfterTransitionCompletes(bool rich)
    {
        MotionSettings.ReduceMotion = false;
        var tip = new ToolTip { Content = "Animation" };
        var flyout = new Flyout { Content = "Animation" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        var button = new Button { Content = "Anchor" };
        if (rich)
            button.Flyout = flyout;
        else
            ToolTip.SetTip(button, tip);
        var window = Show(button);
        for (var opening = 0; opening < 2; opening++)
        {
            if (rich)
                flyout.ShowAt(button);
            else
                ToolTip.SetIsOpen(button, true);
            Pump();
            var presenter = rich ? flyout.Popup.Child! : tip;
            var surface = presenter.GetVisualDescendants().OfType<Decorator>().Single(x => x.Name == "PART_Surface");
            for (var frame = 0; frame < 12; frame++)
            {
                Pump(40);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            var progress = AvaloniaPropertyRegistry.Instance.FindRegistered(surface, "Progress")!;
            surface.GetValue(progress).Should().Be(1d);
            surface.Child!.Clip!.Bounds.Height.Should().BeApproximately(surface.Bounds.Height, 0.01);
            if (rich)
                flyout.Hide();
            else
                ToolTip.SetIsOpen(button, false);
        }
    }

    [Fact]
    public void RoundedButtonExit_ShouldClearHoverInsideRectangularBounds()
    {
        var button = new Button { Content = "Rounded", Width = 120, Height = 40 };
        var window = Show(button);
        Hover(window, button);
        button.Classes.Should().Contain("m3-hovered");
        window.MouseMove(button.TranslatePoint(new Point(0.1, 0.1), window)!.Value);
        Pump();
        button.IsPointerOver.Should().BeFalse();
        button.Classes.Should().NotContain("m3-hovered");
        Hover(window, button);
        button.Classes.Should().Contain("m3-hovered");
        window.MouseMove(new Point(490, 290));
        button.Classes.Should().NotContain("m3-hovered");
    }

    [Fact]
    public void PersistentKeyboardOpening_ShouldShowFocusOnAction_AndSpaceShouldInvokeIt()
    {
        var action = new Button { Content = "Dismiss" };
        var flyout = new Flyout { Content = "Details", ShowMode = FlyoutShowMode.Standard };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        FlyoutAssist.SetActions(flyout, action);
        var invoked = 0;
        action.Click += (_, _) =>
        {
            invoked++;
            flyout.Hide();
        };
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Show(button);
        button.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Pump();
        flyout.IsOpen.Should().BeTrue();
        action.IsFocused.Should().BeTrue();
        action.Classes.Should().Contain(":focus-visible");
        action.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
        action.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space });
        Pump();
        invoked.Should().Be(1);
        flyout.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScrollingAnchorAncestor_ShouldDismissToolTip(bool rich)
    {
        var button = new Button { Content = "Anchor", HorizontalAlignment = HorizontalAlignment.Left };
        var flyout = new Flyout { Content = "Details" };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        if (rich) button.Flyout = flyout;
        else ToolTip.SetTip(button, "Hint");
        var scroll = new ScrollViewer
        {
            Width = 250, Height = 100,
            Content = new StackPanel { Children = { button, new Border { Height = 600 } } }
        };
        Show(scroll);
        if (rich) flyout.ShowAt(button);
        else ToolTip.SetIsOpen(button, true);
        Pump();
        scroll.Offset = new Vector(0, 40);
        Pump();
        (rich ? flyout.IsOpen : ToolTip.GetIsOpen(button)).Should().BeFalse();
    }

    [Fact]
    public void ScrollingToolTipContent_ShouldKeepPersistentToolTipOpen()
    {
        var content = new ScrollViewer { Height = 60, Content = new Border { Height = 400 } };
        var flyout = new Flyout { Content = content, ShowMode = FlyoutShowMode.Standard };
        FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        var button = new Button { Content = "Open", Flyout = flyout };
        Show(button);
        flyout.ShowAt(button);
        Pump();
        content.Offset = new Vector(0, 40);
        Pump();
        content.Offset.Y.Should().Be(40);
        flyout.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void ScrollingDuringShowDelay_ShouldCancelPendingToolTip()
    {
        var button = new Button { Content = "Anchor", HorizontalAlignment = HorizontalAlignment.Left };
        ToolTip.SetTip(button, "Hint");
        ToolTip.SetShowDelay(button, 80);
        ToolTip.SetBetweenShowDelay(button, -1);
        var scroll = new ScrollViewer
        {
            Width = 250, Height = 100,
            Content = new StackPanel { Children = { button, new Border { Height = 600 } } }
        };
        var window = Show(scroll);
        Hover(window, button);
        ToolTip.GetIsOpen(button).Should().BeFalse();
        scroll.Offset = new Vector(0, 40);
        Pump(120);
        ToolTip.GetIsOpen(button).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FlyoutScrollInset_ShouldFollowShape_AndAllowOverride(bool rich)
    {
        var flyout = new Flyout { Content = new Border { Width = 120, Height = 800 } };
        if (rich)
            FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
        var button = new Button { Content = "Open", Flyout = flyout };
        Show(button);
        flyout.ShowAt(button);
        Pump();
        var presenter = (FlyoutPresenter)flyout.Popup.Child!;
        presenter.MaxHeight = 120;
        presenter.CornerRadius = new CornerRadius(0, 8, 12, 16);
        Pump();
        var scroll = presenter.GetVisualDescendants().OfType<ScrollViewer>().First();
        ScrollViewerAssist.GetScrollBarInset(scroll).Should().Be(new Thickness(16, 8, 12, 12));
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>().Single(x => x.Orientation == Orientation.Vertical);
        bar.IsVisible.Should().BeTrue();
        ScrollViewerAssist.GetScrollBarInset(bar).Should().Be(new Thickness(16, 8, 12, 12));
        ScrollViewerAssist.SetScrollBarInset(presenter, new Thickness(3));
        Pump();
        ScrollViewerAssist.GetScrollBarInset(bar).Should().Be(new Thickness(3));
    }

    private Window Show(Control anchor)
    {
        anchor.Margin = new Thickness(100, 100, 0, 0);
        anchor.HorizontalAlignment = HorizontalAlignment.Left;
        anchor.VerticalAlignment = VerticalAlignment.Top;
        ToolTip.SetShowDelay(anchor, 0);
        var window = new Window { Width = 500, Height = 300, Content = new Grid { Children = { anchor } } };
        window.Styles.Add((Styles)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeStyles.axaml")));
        _windows.Add(window);
        window.Show();
        Pump();
        return window;
    }

    private static void Hover(TopLevel window, Control anchor)
    {
        var point = anchor.TranslatePoint(new Point(anchor.Bounds.Width / 2, anchor.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        Pump();
    }

    private static void Pump(int milliseconds = 0)
    {
        Dispatcher.UIThread.RunJobs();
        if (milliseconds > 0)
        {
            using var cancellation = new CancellationTokenSource(milliseconds);
            Dispatcher.UIThread.MainLoop(cancellation.Token);
            Dispatcher.UIThread.RunJobs();
        }
    }
}