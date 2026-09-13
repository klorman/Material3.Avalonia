using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Density;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class MenuTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly Styles _styles;
    private readonly IResourceDictionary _resources;
    private readonly List<Window> _windows = [];
    private readonly bool _reduceMotion;

    public MenuTests()
    {
        TestApp.EnsureStarted();
        _resources =
            (IResourceDictionary)AvaloniaXamlLoader.Load(
                new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"));
        Application.Current!.Resources.MergedDictionaries.Add(_resources);
        _styles = (Styles)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeStyles.axaml"));
        Application.Current.Styles.Add(_styles);
        _theme = new MaterialTheme { Mode = ThemeMode.Light, MotionScheme = null };
        Application.Current.Styles.Add(_theme);
        _reduceMotion = MotionSettings.ReduceMotion;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeyboardRipple_ShouldReleaseAndRestartAcrossMenuNavigation(bool overlay)
    {
        MotionSettings.ReduceMotion = false;
        var first = new MenuItem { Header = "First", ToggleType = MenuItemToggleType.CheckBox };
        var second = new MenuItem { Header = "Second", ToggleType = MenuItemToggleType.CheckBox };
        var flyout = new MenuFlyout { Items = { first, second } };
        flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Flyout = flyout };
        Window(button);
        flyout.ShowAt(button);
        Pump(300);

        object? Held(MenuItem item) => Part(item, "PART_Ripple").GetType()
            .GetField("_keyboardPress",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(Part(item, "PART_Ripple"));

        var root = TopLevel.GetTopLevel(first)!;
        foreach (var item in new[] { first, first, second })
        {
            if (!item.IsFocused)
                root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            var held = Held(item);
            held.Should().NotBeNull();
            root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            Held(item).Should().BeSameAs(held);
            root.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
            Held(item).Should().BeNull();
            Pump(200);
        }

        root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        root.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        Held(second).Should().BeNull();
        root.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        flyout.Hide();
    }

    [Fact]
    public void CancelledClose_ShouldDisplayTheNewSelection()
    {
        var item = new MenuItem
            { Header = "Check", ToggleType = MenuItemToggleType.CheckBox, StaysOpenOnClick = false };
        var menu = new ContextMenu { Items = { item } };
        menu.Closing += (_, e) => e.Cancel = true;
        var button = new Button { ContextMenu = menu };
        Window(button);
        menu.Open(button);
        Pump(250);
        PressEnter(item);
        Pump(300);
        menu.IsOpen.Should().BeTrue();
        item.IsChecked.Should().BeTrue();
        Part(item, "PART_LeadingCheckHost").IsVisible.Should().BeTrue();
    }

    [Theory]
    [InlineData(MenuItemToggleType.CheckBox)]
    [InlineData(MenuItemToggleType.Radio)]
    public void ClosingSelection_ShouldKeepItsPreviousAppearanceUntilReopened(MenuItemToggleType toggle)
    {
        MotionSettings.ReduceMotion = false;
        var previous = new MenuItem { Header = "Previous", ToggleType = toggle, IsChecked = true };
        var next = new MenuItem { Header = "Next", ToggleType = toggle, StaysOpenOnClick = false };
        var clicks = 0;
        next.Click += (_, _) => clicks++;
        var menu = Open(previous, next);
        MenuAssist.SetIsAnimationEnabled(menu, true);
        Pump(250);
        var background = next.Background;
        var previousBackground = previous.Background;
        var corners = next.CornerRadius;
        var label = Part(next, "PART_Label");
        var x = label.Bounds.X;
        PressEnter(next);
        next.IsChecked.Should().BeTrue();
        clicks.Should().Be(1);
        if (toggle == MenuItemToggleType.Radio) previous.IsChecked.Should().BeFalse();
        Pump(30);
        next.Background.Should().Be(background);
        previous.Background.Should().Be(previousBackground);
        next.CornerRadius.Should().Be(corners);
        Part(next, "PART_LeadingCheckHost").IsVisible.Should().BeFalse();
        label.Bounds.X.Should().Be(x);
        Pump(350);
        menu.IsOpen.Should().BeFalse();
        menu.Open((Control)_windows.Last().Content!);
        Layout();
        Part(next, "PART_LeadingCheckHost").IsVisible.Should().BeTrue();
        if (toggle == MenuItemToggleType.Radio)
            Part(previous, "PART_LeadingCheckHost").IsVisible.Should().BeFalse();
    }

    [Fact]
    public void InterruptedHover_ShouldNotFlashAnOpaqueStateLayer()
    {
        MotionSettings.ReduceMotion = false;
        var first = new MenuItem { Header = "First" };
        var second = new MenuItem { Header = "Second" };
        var menu = Open(first, second);
        MenuAssist.SetIsAnimationEnabled(menu, true);
        Pump(200);
        var layers = new[] { Part(first, "PART_StateLayer"), Part(second, "PART_StateLayer") };
        var maximum = 0d;
        foreach (var layer in layers)
            layer.PropertyChanged += (_, e) =>
            {
                if (e.Property == Visual.OpacityProperty) maximum = Math.Max(maximum, layer.Opacity);
            };
        for (var i = 0; i < 20; i++)
        {
            first.Classes.Set("m3-hovered", i % 2 == 0);
            second.Classes.Set("m3-hovered", i % 2 != 0);
            Pump(15);
        }

        Pump(200);
        maximum.Should().BeLessThanOrEqualTo(0.08 + 0.0001);
    }

    [Fact]
    public void Groups_ShouldKeepNativeContainersAndUpdateItemShapes()
    {
        var first = new MenuItem { Header = "First" };
        var second = new MenuItem { Header = "Second" };
        var third = new MenuItem { Header = "Third" };
        var gap = new MenuGap();
        var label = new MenuGroupLabel { Text = "Section" };
        var menu = Open(first, new Separator(), second, gap, label, third);
        menu.ContainerFromIndex(3).Should().BeSameAs(gap);
        menu.ContainerFromIndex(4).Should().BeSameAs(label);
        label.Focus().Should().BeFalse();
        ControlAutomationPeer.CreatePeerForElement(label)!.GetName().Should().Be("Section");
        first.CornerRadius.Should().Be(new CornerRadius(12, 12, 4, 4));
        second.CornerRadius.Should().Be(new CornerRadius(4, 4, 12, 12));
        third.CornerRadius.Should().Be(new CornerRadius(4, 4, 12, 12));
        first.IsVisible = false;
        Layout();
        second.CornerRadius.Should().Be(new CornerRadius(12, 12, 4, 4));
        third.IsChecked = true;
        Layout();
        third.CornerRadius.Should().Be(new CornerRadius(12));
    }

    [Fact]
    public void SingleItemGroups_ShouldUseTheirOuterEdges()
    {
        var first = new MenuItem { Header = "First" };
        var middle = new MenuItem { Header = "Middle" };
        var last = new MenuItem { Header = "Last" };
        var menu = Open(first, new MenuGap(), middle, new MenuGap(), last);
        first.CornerRadius.Should().Be(new CornerRadius(12, 12, 4, 4));
        middle.CornerRadius.Should().Be(new CornerRadius(4));
        last.CornerRadius.Should().Be(new CornerRadius(4, 4, 12, 12));
        menu.Items.Clear();
        menu.Items.Add(middle);
        Layout();
        middle.CornerRadius.Should().Be(new CornerRadius(12));
    }

    [Fact]
    public void VirtualizedMenu_ShouldNavigateAndKeepChecksOutsideViewport()
    {
        var items = Enumerable.Range(0, 60).Select(i => new MenuItem
            { Header = $"Item {i}", ToggleType = MenuItemToggleType.CheckBox }).ToArray();
        var menu = new ContextMenu { MaxHeight = 180 };
        foreach (var item in items) menu.Items.Add(item);
        MenuAssist.SetIsAnimationEnabled(menu, false);
        var button = new Button { ContextMenu = menu };
        Window(button);
        menu.Open(button);
        Layout();
        menu.GetVisualDescendants().OfType<MenuItem>().Count().Should().BeLessThan(30);
        var root = TopLevel.GetTopLevel(menu)!;
        items[50].IsVisible = false;
        for (var i = 0; i < 45; i++)
        {
            root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Layout();
        }

        items[44].IsFocused.Should().BeTrue();
        items[50].IsVisible.Should().BeFalse();
        root.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout();
        items[44].IsChecked.Should().BeTrue();
        for (var i = 0; i < 10; i++)
        {
            root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Layout();
        }

        items[55].IsFocused.Should().BeTrue();
        items[50].IsVisible.Should().BeFalse();
        for (var i = 0; i < 54; i++)
            root.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        Layout();
        items[0].IsFocused.Should().BeTrue();
        items[44].IsChecked.Should().BeTrue();
        MenuAssist.SetIsAnimationEnabled(menu, true);
        menu.ScrollIntoView(items[44]);
        Layout();
        Part(items[44], "PART_Label").RenderTransform.Should().BeNull();
        Part(items[44], "PART_LeadingCheckHost").Opacity.Should().Be(1);
    }

    [Fact]
    public void StandardSubmenu_ShouldPaintAnOpaqueSurfaceBetweenRows()
    {
        var parent = new MenuItem { Header = "Export", Items = { new MenuItem { Header = "PDF" } } };
        var menu = Open(parent);
        parent.IsSubMenuOpen = true;
        Layout();
        var popup = parent.GetVisualDescendants().OfType<Popup>().First();
        var surface = popup.Child!.GetVisualDescendants().OfType<Control>()
            .First(c => c.Name == "PART_MenuSurface");
        var brush = surface.GetValue(Border.BackgroundProperty).Should().BeAssignableTo<ISolidColorBrush>().Subject;
        brush.Color.A.Should().Be(255);
        brush.Opacity.Should().Be(1);
        parent.IsSubMenuOpen = false;
        Layout();
    }

    [Fact]
    public void GeneratedUnboundChecks_ShouldNotTransferSelectionWhenScrolling()
    {
        var theme = new ControlTheme(typeof(MenuItem))
        {
            BasedOn = (ControlTheme)Application.Current!.FindResource(typeof(MenuItem))!,
            Setters = { new Setter(MenuItem.ToggleTypeProperty, MenuItemToggleType.CheckBox) }
        };
        var menu = new ContextMenu
        {
            MaxHeight = 180, ItemContainerTheme = theme,
            ItemsSource = Enumerable.Range(0, 60).Select(i => $"Choice {i}").ToArray()
        };
        MenuAssist.SetIsAnimationEnabled(menu, false);
        var button = new Button { ContextMenu = menu };
        Window(button);
        menu.Open(button);
        Layout();
        var first = (MenuItem)menu.ContainerFromIndex(0)!;
        PressEnter(first);
        first.IsChecked.Should().BeTrue();
        var scroll = menu.GetVisualDescendants().OfType<ScrollViewer>().First();
        scroll.Offset = new Vector(0, 1800);
        Layout();
        scroll.Offset = default;
        Layout();
        ((MenuItem)menu.ContainerFromIndex(0)!).IsChecked.Should().BeTrue();
        ((MenuItem)menu.ContainerFromIndex(1)!).IsChecked.Should().BeFalse();
    }

    [Theory]
    [InlineData(MaterialDensity.Default, 44, 8)]
    [InlineData(MaterialDensity.Dense1, 40, 6)]
    [InlineData(MaterialDensity.Dense2, 36, 4)]
    [InlineData(MaterialDensity.Dense3, 32, 2)]
    [InlineData(MaterialDensity.Dense5, 32, 2)]
    public void Density_ShouldReduceOnlyRowGeometry(MaterialDensity density, double height, double padding)
    {
        var item = new MenuItem { Header = "Label" };
        var menu = Open(item);
        DensityAssist.SetDensity(menu, density);
        Layout();
        item.MinHeight.Should().Be(height);
        item.Padding.Should().Be(new Thickness(16, padding));
        item.Bounds.Height.Should().Be(height);
        MenuAssist.SetSupportingText(item, "More detail");
        Layout();
        item.Bounds.Height.Should().BeGreaterThan(height);
        item.FontSize.Should().Be(14);
    }

    [Fact]
    public void CheckboxAndRadio_ShouldUseNativeSelectionAndCloseOverrides()
    {
        var check = new MenuItem { Header = "Check", ToggleType = MenuItemToggleType.CheckBox };
        var a = new MenuItem
            { Header = "A", ToggleType = MenuItemToggleType.Radio, GroupName = "group", IsChecked = true };
        var b = new MenuItem { Header = "B", ToggleType = MenuItemToggleType.Radio, GroupName = "group" };
        var clicks = 0;
        check.Click += (_, _) => clicks++;
        var menu = Open(check, a, b);
        PressEnter(check);
        check.IsChecked.Should().BeTrue();
        clicks.Should().Be(1);
        menu.IsOpen.Should().BeTrue();
        PressEnter(b);
        b.IsChecked.Should().BeTrue();
        a.IsChecked.Should().BeFalse();
        menu.IsOpen.Should().BeFalse();
        check.StaysOpenOnClick = false;
        menu.Open();
        Layout();
        PressEnter(check);
        menu.IsOpen.Should().BeFalse();
        clicks.Should().Be(2);
    }

    [Fact]
    public void Overflow_ShouldReplaceGapsWithoutChangingItems()
    {
        var gap = new MenuGap();
        var menu = Open(new MenuItem { Header = "A" }, gap, new MenuItem { Header = "B" },
            new MenuItem { Header = "C" });
        gap.Classes.Should().Contain("m3-menu-gap");
        menu.MaxHeight = 100;
        Layout();
        gap.Classes.Should().NotContain("m3-menu-gap");
        menu.Items[1].Should().BeSameAs(gap);
        var scroll = menu.GetVisualDescendants().OfType<ScrollViewer>().First();
        scroll.Extent.Height.Should().BeGreaterThan(scroll.Viewport.Height);
        scroll.AllowAutoHide.Should().BeFalse();
        menu.MaxHeight = double.PositiveInfinity;
        Layout();
        gap.Classes.Should().Contain("m3-menu-gap");
    }

    [Fact]
    public void Flyout_ShouldBridgeSettingsAndLocalTokenChangesToSubmenu()
    {
        var child = new MenuItem { Header = "Child" };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var flyout = new MenuFlyout { Items = { parent } };
        MenuAssist.SetColorStyle(flyout, MenuColorStyle.Vibrant);
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Content = "Open", Flyout = flyout };
        DensityAssist.SetDensity(button, MaterialDensity.Dense2);
        var window = Window(button);
        flyout.ShowAt(button);
        Layout();
        parent.Bounds.Height.Should().Be(36);
        MenuAssist.GetColorStyle(parent).Should().Be(MenuColorStyle.Vibrant);
        parent.IsSubMenuOpen = true;
        Layout();
        child.Bounds.Height.Should().Be(36);
        MenuAssist.GetColorStyle(child).Should().Be(MenuColorStyle.Vibrant);
        parent.Resources["MdCompMenusVibrantItemLabelTextBrush"] = Brushes.Lime;
        Layout();
        child.Foreground.Should().Be(Brushes.Lime);
        parent.Resources["MdCompMenusVibrantItemLabelTextBrush"] = new TokenAlias { ResourceKey = "CustomMenuBrush" };
        parent.Resources["CustomMenuBrush"] = Brushes.Orange;
        Layout();
        child.Foreground.Should().Be(Brushes.Orange);
        flyout.Hide();
        Layout();
        flyout.IsOpen.Should().BeFalse();
        button.Classes.Should().NotContain("m3-menu-open");
    }

    [Fact]
    public void CancelledAnimatedClose_ShouldLeaveMenuOpenAndExecuteClickOnlyOnce()
    {
        var item = new MenuItem { Header = "Action" };
        var menu = Open(item);
        MenuAssist.SetIsAnimationEnabled(menu, true);
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        CancelEventHandler cancel = (_, e) => e.Cancel = true;
        menu.Closing += cancel;
        PressEnter(item);
        Pump(400);
        menu.IsOpen.Should().BeTrue();
        clicks.Should().Be(1);
        menu.Closing -= cancel;
        menu.Close();
        Pump(400);
        menu.IsOpen.Should().BeFalse();
        clicks.Should().Be(1);
    }

    [Fact]
    public void KeyboardNavigation_ShouldSkipSeparatorsAndDisabledItems()
    {
        var first = new MenuItem { Header = "First" };
        var last = new MenuItem { Header = "Last" };
        var menu = Open(first, new MenuGap(), new MenuGroupLabel { Text = "Group" }, new Separator(),
            new MenuItem { Header = "Disabled", IsEnabled = false }, last);
        var root = TopLevel.GetTopLevel(menu)!;
        root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Layout();
        first.IsFocused.Should().BeTrue();
        root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Layout();
        last.IsFocused.Should().BeTrue();
        root.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Layout();
        menu.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FlyoutOpening_ShouldFocusAnItemAndNavigate(bool overlay)
    {
        var first = new MenuItem { Header = "First" };
        var last = new MenuItem { Header = "Last" };
        var flyout = new MenuFlyout { Items = { first, new Separator(), last } };
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        flyout.Popup.ShouldUseOverlayLayer = overlay;
        var button = new Button { Content = "Open", Flyout = flyout };
        Window(button);
        button.Focus();
        flyout.ShowAt(button);
        Layout();
        first.IsFocused.Should().BeTrue();
        TopLevel.GetTopLevel(first)!.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Layout();
        last.IsFocused.Should().BeTrue();
        flyout.Hide();
        Layout();
        button.IsFocused.Should().BeTrue();
    }

    [Fact]
    public void SubmenuExit_ShouldAllowReopeningAndImmediateAnimationDisable()
    {
        var parent = new MenuItem { Header = "Parent" };
        var child = new MenuItem { Header = "Child" };
        parent.Items.Add(child);
        var menu = Open(parent);
        MenuAssist.SetIsAnimationEnabled(parent, true);
        parent.IsSubMenuOpen = true;
        Pump(50);
        var popup = parent.GetVisualDescendants().OfType<Popup>().First(p => p.TemplatedParent == parent);
        parent.IsSubMenuOpen = false;
        Pump(30);
        parent.IsSubMenuOpen = true;
        Pump(400);
        popup.IsOpen.Should().BeTrue();
        popup.Child!.Opacity.Should().Be(1);
        parent.IsSubMenuOpen = false;
        MenuAssist.SetIsAnimationEnabled(parent, false);
        Layout();
        popup.IsOpen.Should().BeFalse();
        menu.Close();
    }

    [Fact]
    public void SubmenuPointerPress_ShouldBelongOnlyToTheChild()
    {
        var child = new MenuItem { Header = "Child", StaysOpenOnClick = true };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var flyout = new MenuFlyout { Items = { parent } };
        flyout.Popup.ShouldUseOverlayLayer = true;
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Window(button);
        flyout.ShowAt(button);
        Layout();
        parent.IsSubMenuOpen = true;
        Layout();
        var point = child.TranslatePoint(new Point(child.Bounds.Width / 2, child.Bounds.Height / 2), window)!.Value;
        // Headless's consecutive-triangle approximation cannot hit the center of a rounded path.
        // Keep these input regressions independent of Skia's clipping verification.
        var clips = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Where(scroll => scroll.Clip is not null)
            .Select(scroll => scroll.SetValue(Visual.ClipProperty, new RectangleGeometry(scroll.Clip!.Bounds),
                global::Avalonia.Data.BindingPriority.Animation)).ToArray();
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        parent.Classes.Should().NotContain("m3-menu-pressed");
        child.Classes.Should().Contain("m3-menu-pressed");
        window.MouseUp(point, MouseButton.Left);
        foreach (var clip in clips) clip?.Dispose();
        flyout.Hide();
    }

    [Fact]
    public void PointerLeavingPressedRow_ShouldCancelItsVisualState()
    {
        var item = new MenuItem { Header = "Stay open", StaysOpenOnClick = true };
        var flyout = new MenuFlyout { Items = { item } };
        flyout.Popup.ShouldUseOverlayLayer = true;
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Window(button);
        flyout.ShowAt(button);
        Layout();
        var point = item.TranslatePoint(new Point(20, 20), window)!.Value;
        // Headless's consecutive-triangle approximation cannot hit the center of a rounded path.
        // Keep these input regressions independent of Skia's clipping verification.
        var clips = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Where(scroll => scroll.Clip is not null)
            .Select(scroll => scroll.SetValue(Visual.ClipProperty, new RectangleGeometry(scroll.Clip!.Bounds),
                global::Avalonia.Data.BindingPriority.Animation)).ToArray();
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        item.Classes.Should().Contain("m3-menu-pressed");
        window.MouseMove(new Point(590, 490));
        item.Classes.Should().NotContain("m3-menu-pressed");
        window.MouseUp(new Point(590, 490), MouseButton.Left);
        item.Classes.Should().NotContain("m3-menu-pressed");
        foreach (var clip in clips) clip?.Dispose();
        flyout.Hide();
    }

    [Fact]
    public void Overflow_ShouldKeepTheViewportWidthAndClipItsContent()
    {
        var menu = Open(Enumerable.Range(0, 8).Select(i => new MenuItem { Header = $"Project {i}" }).ToArray());
        var scroll = menu.GetVisualDescendants().OfType<ScrollViewer>().First();
        var width = scroll.Viewport.Width;
        menu.MaxHeight = 150;
        Layout();
        scroll.Viewport.Width.Should().Be(width);
        scroll.Clip.Should().NotBeNull();
        scroll.Offset = new Vector(0, 30);
        Layout();
        scroll.Clip!.FillContains(new Point(0, 0)).Should().BeFalse();
    }

    [Fact]
    public void CustomPanel_ShouldUseVisibleDividerAndPreserveLocalOverrides()
    {
        var first = new MenuItem { Header = "First", CornerRadius = new CornerRadius(7) };
        var gap = new MenuGap();
        var second = new MenuItem { Header = "Second", ToggleType = MenuItemToggleType.CheckBox };
        var menu = Open(first, gap, second);
        menu.ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel());
        Layout();
        gap.Classes.Should().NotContain("m3-menu-gap");
        gap.Bounds.Height.Should().BeGreaterThan(0);
        first.CornerRadius.Should().Be(new CornerRadius(7));
        PressEnter(second);
        first.CornerRadius.Should().Be(new CornerRadius(7));
    }

    [Fact]
    public void Dividers_ShouldHaveMenuInsetsAndPreserveOverrides()
    {
        var first = new MenuItem { Header = "First" };
        var divider = new Separator();
        var last = new MenuItem { Header = "Last" };
        var menu = Open(first, divider, last);
        first.Bounds.X.Should().Be(4);
        first.Bounds.Y.Should().Be(4);
        divider.Margin.Top.Should().Be(4);
        divider.Margin.Bottom.Should().Be(4);
        DividerAssist.GetVariant(divider).Should().Be(DividerVariant.MiddleInset);
        divider.Padding.Left.Should().Be(16);
        divider.Padding.Right.Should().Be(16);
        divider.Bounds.X.Should().Be(0);
        divider.Bounds.Top.Should().Be(first.Bounds.Bottom + 4);
        last.Bounds.Top.Should().Be(divider.Bounds.Bottom + 4);
        DividerAssist.SetInsetStart(divider, 24);
        divider.Margin = new Thickness(0, 3);
        Layout();
        divider.Padding.Left.Should().Be(24);
        divider.Margin.Top.Should().Be(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContextClose_ShouldRestoreFocusWithoutScrollingThePreviousControl(bool useFlyout)
    {
        var previous = new Button { Content = "Previous focus" };
        var target = new Border { Height = 80, Background = Brushes.Gray };
        var panel = new StackPanel { Children = { previous, new Border { Height = 900 }, target } };
        var scroll = new ScrollViewer { Content = panel };
        var window = Window(scroll);
        previous.Focus();
        scroll.Offset = new Vector(0, 600);
        Layout();
        var offset = scroll.Offset;
        if (useFlyout)
        {
            var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Action" } } };
            MenuAssist.SetIsAnimationEnabled(flyout, false);
            target.ContextFlyout = flyout;
            flyout.ShowAt(target, true);
            Layout();
            flyout.Hide();
        }
        else
        {
            var menu = new ContextMenu { Items = { new MenuItem { Header = "Action" } } };
            MenuAssist.SetIsAnimationEnabled(menu, false);
            target.ContextMenu = menu;
            menu.Open(target);
            Layout();
            menu.Close();
        }

        Layout();
        previous.IsFocused.Should().BeTrue();
        scroll.Offset.Should().Be(offset);
        scroll.BringIntoViewOnFocusChange.Should().BeTrue();
        previous.BringIntoView();
        Layout();
        scroll.Offset.Y.Should().Be(0, "explicit requests remain enabled after restoring focus");
    }

    [Fact]
    public void TextBoxMenu_ShouldUseNativeSelectionAndRetargetCommands()
    {
        var first = new TextBox { Text = "Project title" };
        var second = new TextBox { Text = "Another document", IsReadOnly = true };
        var window = Window(new StackPanel { Children = { first, second } });
        first.ContextFlyout.Should().BeOfType<MenuFlyout>();
        var flyout = (MenuFlyout)first.ContextFlyout!;
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        first.Focus();
        first.SelectAll();
        flyout.ShowAt(first);
        Layout();
        var items = flyout.Items.OfType<MenuItem>().ToArray();
        items[0].IsEffectivelyEnabled.Should().BeTrue();
        items[1].IsEffectivelyEnabled.Should().BeTrue();
        foreach (var item in items)
        {
            item.InputGesture.Should().BeNull();
            item.Icon.Should().BeNull();
            MenuAssist.GetTrailingIcon(item).Should().NotBeNull();
            Part(item, "PART_TrailingText").IsEffectivelyVisible.Should().BeFalse();
        }

        items[3].Command!.Execute(null);
        first.SelectedText.Should().Be("Project title");
        items[0].Command!.Execute(null);
        Pump(20);
        first.Text.Should().BeEmpty();
        flyout.ShowAt(first);
        Layout();
        items[2].Command!.Execute(null);
        Pump(20);
        first.Text.Should().Be("Project title");
        flyout.Hide();
        second.SelectAll();
        flyout.ShowAt(second);
        Layout();
        items[0].IsEffectivelyEnabled.Should().BeFalse();
        items[1].IsEffectivelyEnabled.Should().BeTrue();
        items[2].IsEffectivelyEnabled.Should().BeFalse();
        second.SelectionStart = second.SelectionEnd = 0;
        items[3].Command!.Execute(null);
        second.SelectedText.Should().Be("Another document");
        flyout.Hide();
        first.PasswordChar = '*';
        first.SelectAll();
        flyout.ShowAt(first);
        Layout();
        items[0].IsEffectivelyEnabled.Should().BeFalse();
        items[1].IsEffectivelyEnabled.Should().BeFalse();
        flyout.Hide();
        var custom = new MenuFlyout();
        first.ContextFlyout = custom;
        Layout();
        first.ContextFlyout.Should().BeSameAs(custom);
    }

    [Theory]
    [InlineData(MenuColorStyle.Standard)]
    [InlineData(MenuColorStyle.Vibrant)]
    public void SelectedDisabledDetails_ShouldUseOpacityTokensAndRestoreWhenEnabled(MenuColorStyle colorStyle)
    {
        var item = new MenuItem { Header = "Action", IsChecked = true, IsEnabled = false };
        MenuAssist.SetSupportingText(item, "Details");
        MenuAssist.SetTrailingText(item, "Status");
        MenuAssist.SetColorStyle(item, colorStyle);
        Open(item);
        var supporting = Part(item, "PART_SupportingText");
        var trailing = Part(item, "PART_TrailingText");
        supporting.Opacity.Should().Be(0.38);
        trailing.Opacity.Should().Be(0.38);
        var prefix = "MdCompMenus" + colorStyle + "Item" +
                     (colorStyle == MenuColorStyle.Vibrant ? "Selected" : "") + "Disabled";
        item.Resources[prefix + "SupportingTextOpacity"] = 0.25;
        item.Resources[prefix + "TrailingSupportingTextOpacity"] = 0.3;
        Layout();
        supporting.Opacity.Should().Be(0.25);
        trailing.Opacity.Should().Be(0.3);
        item.IsEnabled = true;
        Layout();
        supporting.Opacity.Should().Be(1);
        trailing.Opacity.Should().Be(1);
        item.IsEnabled = false;
        Layout();
        supporting.Opacity.Should().Be(0.25);
        trailing.Opacity.Should().Be(0.3);
    }

    [Fact]
    public void StateTokens_ShouldFollowSelectionAndKeepLocalOverrides()
    {
        var item = new MenuItem { Header = "Action" };
        var menu = Open(item);
        item.Resources["MdCompMenusStandardItemSelectedLabelTextBrush"] = Brushes.HotPink;
        item.IsChecked = true;
        Layout();
        item.Foreground.Should().Be(Brushes.HotPink);
        MenuAssist.SetSupportingText(item, "Details");
        item.Resources["MdCompMenusStandardItemSelectedHoverSupportingTextBrush"] = Brushes.Orange;
        item.Classes.Add("m3-hovered");
        Layout();
        item.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Details")
            .Foreground.Should().Be(Brushes.Orange);
        item.Classes.Remove("m3-hovered");
        item.Resources["MdCompMenusVibrantItemSelectedLabelTextBrush"] = Brushes.Lime;
        MenuAssist.SetColorStyle(menu, MenuColorStyle.Vibrant);
        Layout();
        item.Foreground.Should().Be(Brushes.Lime);
        item.Foreground = Brushes.Blue;
        item.IsChecked = false;
        item.IsEnabled = false;
        Layout();
        item.Foreground.Should().Be(Brushes.Blue);
    }

    [Fact]
    public void DisabledMotion_ShouldShowFinalGeometryBeforeTheNextDispatcherJob()
    {
        var item = new MenuItem { Header = "Action" };
        var flyout = new MenuFlyout { Items = { item } };
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Flyout = flyout };
        Window(button);
        flyout.ShowAt(button);
        var surface = flyout.Popup.Child!.GetVisualDescendants().OfType<Control>()
            .First(c => c.Name == "PART_MenuSurface");
        surface.Opacity.Should().Be(1);
        flyout.Hide();
        flyout.ShowAt(button);
        surface.Opacity.Should().Be(1);
        flyout.Hide();
    }

    [Fact]
    public void ExpressiveOpening_ShouldMoveRowsWithoutScalingAndRemoveTransforms()
    {
        var previous = MotionSettings.GlobalScheme;
        try
        {
            MotionSettings.GlobalScheme = MotionScheme.Expressive;
            var items = Enumerable.Range(0, 6).Select(i => new MenuItem { Header = $"Item {i}" }).ToArray();
            var flyout = new MenuFlyout();
            foreach (var item in items) flyout.Items.Add(item);
            var button = new Button { Content = "Open", Flyout = flyout };
            Window(button);
            flyout.Popup.ShouldUseOverlayLayer = true;
            flyout.ShowAt(button);
            Layout();
            var row = items[^1].GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Row");
            var header = items[^1].GetVisualDescendants().OfType<ContentPresenter>()
                .First(c => c.Name == "PART_HeaderPresenter");
            var height = header.Bounds.Height;
            row.RenderTransform.Should().NotBeNull();
            items[^1].ClipToBounds.Should().BeFalse();
            row.RenderTransform!.Value.M11.Should().Be(1);
            row.RenderTransform.Value.M22.Should().Be(1);
            var greatestDisplacement = double.NegativeInfinity;
            for (var i = 0; i < 15; i++)
            {
                Pump(20);
                greatestDisplacement = Math.Max(greatestDisplacement, row.RenderTransform?.Value.M32 ?? 0);
            }

            greatestDisplacement.Should().BeGreaterThan(0, "Expressive motion retains its spring overshoot");
            Pump(300);
            header.Bounds.Height.Should().Be(height);
            row.RenderTransform.Should().BeNull();
            items[^1].ClipToBounds.Should().BeTrue();
            flyout.Hide();
            Pump(600);
            flyout.IsOpen.Should().BeFalse();
        }
        finally
        {
            MotionSettings.GlobalScheme = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Escape_ShouldNotIntroduceFocusOnTheDepartingMenu(bool overlay)
    {
        MotionSettings.ReduceMotion = false;
        var child = new MenuItem { Header = "Child" };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var menu = new ContextMenu { Items = { parent } };
        var button = new Button { ContextMenu = menu };
        var window = Window(button);
        menu.AttachedToLogicalTree += (_, _) =>
            menu.FindLogicalAncestorOfType<Popup>()!.ShouldUseOverlayLayer = overlay;
        menu.Open(button);
        Pump(300);
        parent.Focus();
        var focus = Part(parent, "PART_MenuFocus");
        focus.IsVisible.Should().BeFalse();
        var appeared = false;
        focus.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty && focus.IsVisible) appeared = true;
        };
        TopLevel.GetTopLevel(parent)!.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump(300);
        appeared.Should().BeFalse();
        menu.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RtlAndSpace_ShouldUseNativeSubmenuAndSelection(bool overlay, bool flyoutHost)
    {
        var clicks = 0;
        var check = new MenuItem { Header = "Check", ToggleType = MenuItemToggleType.CheckBox };
        check.Click += (_, _) => clicks++;
        var other = new MenuItem { Header = "Other" };
        var parent = new MenuItem { Header = "Parent", Items = { check, other } };
        var button = new Button();
        var window = Window(button);
        if (flyoutHost)
        {
            var flyout = new MenuFlyout { Items = { parent } };
            flyout.Popup.ShouldUseOverlayLayer = overlay;
            MenuAssist.SetIsAnimationEnabled(flyout, false);
            button.FlowDirection = FlowDirection.RightToLeft;
            button.Flyout = flyout;
            flyout.ShowAt(button);
        }
        else
        {
            var menu = new ContextMenu
            {
                Items = { parent }, FlowDirection = FlowDirection.RightToLeft
            };
            MenuAssist.SetIsAnimationEnabled(menu, false);
            menu.AttachedToLogicalTree += (_, _) =>
                menu.FindLogicalAncestorOfType<Popup>()!.ShouldUseOverlayLayer = overlay;
            button.ContextMenu = menu;
            menu.Open(button);
        }

        Layout();
        var root = TopLevel.GetTopLevel(parent)!;
        if (!parent.IsFocused) root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        root.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Layout();
        parent.IsSubMenuOpen.Should().BeTrue();
        check.IsFocused.Should().BeTrue();
        root = TopLevel.GetTopLevel(check)!;
        root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        clicks.Should().Be(0);
        root.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        clicks.Should().Be(1);
        check.IsChecked.Should().BeTrue();
        parent.IsSubMenuOpen.Should().BeTrue();
        root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        root.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        clicks.Should().Be(1);
        root.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Layout();
        parent.IsSubMenuOpen.Should().BeFalse();
        parent.IsFocused.Should().BeTrue();
        Part(parent, "PART_MenuFocus").IsVisible.Should().BeTrue();
        root = TopLevel.GetTopLevel(parent)!;
        root.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        root.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        Layout();
        check.IsFocused.Should().BeTrue();
        TopLevel.GetTopLevel(check)!.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Layout();
        parent.IsFocused.Should().BeTrue();
        Part(parent, "PART_MenuFocus").IsVisible.Should().BeTrue();
    }

    [Fact]
    public void ArrowNavigation_ShouldShowFocusWithoutChangingChecks()
    {
        var first = new MenuItem { Header = "First", ToggleType = MenuItemToggleType.CheckBox };
        var second = new MenuItem { Header = "Second" };
        var menu = Open(first, second);
        var root = TopLevel.GetTopLevel(menu)!;
        root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Layout();
        Part(first, "PART_MenuFocus").IsVisible.Should().BeTrue();
        Part(first, "PART_StateLayer").Opacity.Should().BeGreaterThan(0);
        first.IsChecked.Should().BeFalse();
        root.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Layout();
        Part(first, "PART_MenuFocus").IsVisible.Should().BeFalse();
        Part(second, "PART_MenuFocus").IsVisible.Should().BeTrue();
    }

    [Fact]
    public void KeyboardOpening_ShouldShowTheInitialItemFocus()
    {
        var first = new MenuItem { Header = "First" };
        var flyout = new MenuFlyout { Items = { first } };
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Content = "Open", Flyout = flyout };
        var window = Window(button);
        button.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        Layout();
        first.IsFocused.Should().BeTrue();
        Part(first, "PART_MenuFocus").IsVisible.Should().BeTrue();
        flyout.Hide();
        Layout();
    }

    [Fact]
    public void RowSpacing_ShouldSeparateRowsWithoutReservingMissingIcons()
    {
        var first = new MenuItem { Header = "First", Icon = new PathIcon(), ToggleType = MenuItemToggleType.CheckBox };
        var second = new MenuItem { Header = "Second", ToggleType = MenuItemToggleType.CheckBox };
        var third = new MenuItem { Header = "Third" };
        var menu = Open(first, second, third, new Separator(), new MenuItem { Header = "Last" });
        var firstRow = Part(first, "PART_Row");
        var secondRow = Part(second, "PART_Row");
        secondRow.TranslatePoint(default, firstRow)!.Value.Y.Should().BeApproximately(firstRow.Bounds.Height + 4, .01);
        firstRow.Bounds.Height.Should().Be(44);
        var before = Part(third, "PART_Row").PointToScreen(default);
        Part(second, "PART_Check").IsEffectivelyVisible.Should().BeFalse();
        var firstLabel = Part(first, "PART_HeaderPresenter").TranslatePoint(default, firstRow)!.Value.X;
        var secondLabel = Part(second, "PART_HeaderPresenter").TranslatePoint(default, secondRow)!.Value.X;
        secondLabel.Should().BeLessThan(firstLabel);
        second.IsChecked = true;
        Layout();
        Part(second, "PART_Check").IsEffectivelyVisible.Should().BeTrue();
        Part(third, "PART_Row").PointToScreen(default).Should().Be(before);
        second.IsVisible = false;
        Layout();
        Part(third, "PART_Row").TranslatePoint(default, firstRow)!.Value.Y
            .Should().BeApproximately(firstRow.Bounds.Height + 4, .01);
    }

    [Fact]
    public void ClosingRoot_ShouldAnimateItsOpenSubmenuAtTheSameTime()
    {
        var child = new MenuItem { Header = "Child" };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var menu = Open(parent);
        parent.IsSubMenuOpen = true;
        Layout();
        var popup = parent.GetVisualDescendants().OfType<Popup>().Single();
        var rootSurface = Part(menu, "PART_MenuSurface");
        var subSurface = Part(popup.Child!, "PART_MenuSurface");
        MenuAssist.SetIsAnimationEnabled(menu, true);
        menu.Close();
        Pump(40);
        rootSurface.Opacity.Should().BeLessThan(1);
        subSurface.Opacity.Should().BeLessThan(1);
        popup.IsOpen.Should().BeTrue();
        Pump(700);
        menu.IsOpen.Should().BeFalse();
        popup.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ButtonKeyboardActivation_ShouldClickOnSpaceReleaseAndEnterPress()
    {
        var button = new Button { Content = "Action" };
        var window = Window(button);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        button.Focus();
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        Pump(30);
        clicks.Should().Be(0);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        Layout();
        clicks.Should().Be(1);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout();
        clicks.Should().Be(2);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout();
        clicks.Should().Be(2);
    }

    [Fact]
    public void CancelledRootExit_ShouldRestoreTheOpenSubmenu()
    {
        var child = new MenuItem { Header = "Child" };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var menu = Open(parent);
        parent.IsSubMenuOpen = true;
        Layout();
        var popup = parent.GetVisualDescendants().OfType<Popup>().Single();
        MenuAssist.SetIsAnimationEnabled(menu, true);
        CancelEventHandler cancel = (_, args) => args.Cancel = true;
        menu.Closing += cancel;
        menu.Close();
        Pump(1000);
        menu.IsOpen.Should().BeTrue();
        popup.IsOpen.Should().BeTrue();
        var surface = Part(popup.Child!, "PART_MenuSurface");
        surface.Opacity.Should().Be(1);
        surface.IsHitTestVisible.Should().BeTrue();
        menu.Closing -= cancel;
        menu.Close();
        Pump(700);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubmenuSurface_ShouldMeetTheParentRowEdge(bool rtl)
    {
        var parent = new MenuItem { Header = "Parent", Items = { new MenuItem { Header = "Child" } } };
        var menu = new MenuFlyout { Items = { parent } };
        menu.Popup.ShouldUseOverlayLayer = true;
        MenuAssist.SetIsAnimationEnabled(menu, false);
        var button = new Button
        {
            Content = "Open", Flyout = menu,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
        Window(button);
        menu.ShowAt(button);
        Layout();
        parent.IsSubMenuOpen = true;
        Layout();
        var popup = parent.GetVisualDescendants().OfType<Popup>().Single();
        var scroll = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().First();
        var row = Part(parent, "PART_Row");
        var rowStart = row.PointToScreen(default).X;
        var rowEnd = row.PointToScreen(new Point(row.Bounds.Width, 0)).X;
        var subStart = scroll.PointToScreen(default).X;
        var subEnd = scroll.PointToScreen(new Point(scroll.Bounds.Width, 0)).X;
        Math.Min(Math.Abs(Math.Max(rowStart, rowEnd) - Math.Min(subStart, subEnd)),
            Math.Abs(Math.Min(rowStart, rowEnd) - Math.Max(subStart, subEnd))).Should().BeLessThanOrEqualTo(1);
        menu.Hide();
        Layout();
    }

    private static Control Part(Control owner, string name) =>
        owner.GetVisualDescendants().OfType<Control>().First(c => c.Name == name);

    [Fact]
    public void Gestures_ShouldRenderAndRespectTrailingOverrides()
    {
        var item = new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        Open(item);
        var text = (TextBlock)Part(item, "PART_TrailingText");
        text.IsVisible.Should().BeTrue();
        text.Text.Should().Be(item.InputGesture.ToString("p", null));
        item.InputGesture = new KeyGesture(Key.A, KeyModifiers.Meta);
        Layout();
        text.Text.Should().Be(item.InputGesture.ToString("p", null));
        MenuAssist.SetTrailingText(item, "Available offline");
        Layout();
        text.Text.Should().Be("Available offline");
        MenuAssist.SetTrailingText(item, "");
        Layout();
        text.IsVisible.Should().BeFalse();
        MenuAssist.SetTrailingText(item, null);
        Layout();
        text.Text.Should().Be(item.InputGesture.ToString("p", null));
    }

    [Fact]
    public void SelectableTextMenu_ShouldUseATrailingIconWithoutAGesture()
    {
        var owner = new SelectableTextBlock { Text = "Project brief" };
        Window(owner);
        var menu = (MenuFlyout)owner.ContextFlyout!;
        MenuAssist.SetIsAnimationEnabled(menu, false);
        menu.ShowAt(owner);
        Layout();
        var copy = menu.Items.OfType<MenuItem>().Single();
        copy.InputGesture.Should().BeNull();
        copy.Icon.Should().BeNull();
        MenuAssist.GetTrailingIcon(copy).Should().NotBeNull();
        Part(copy, "PART_TrailingText").IsEffectivelyVisible.Should().BeFalse();
        menu.Hide();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Checkmarks_ShouldReplaceOnlyTheirChosenIcon(bool rtl)
    {
        var icon = new PathIcon();
        var trailing = new PathIcon();
        var item = new MenuItem
        {
            Header = "Choice", Icon = icon, ToggleType = MenuItemToggleType.CheckBox,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
        MenuAssist.SetTrailingIcon(item, trailing);
        var menu = Open(item);
        var label = Part(item, "PART_Label");
        var position = label.Bounds;
        item.IsChecked = true;
        Layout();
        Part(item, "PART_LeadingCheckHost").IsVisible.Should().BeTrue();
        Part(item, "PART_LeadingIconHost").Opacity.Should().Be(0);
        label.Bounds.Should().Be(position);
        MenuAssist.SetCheckmarkPlacement(menu, MenuCheckmarkPlacement.Trailing);
        Layout();
        Part(item, "PART_LeadingIconHost").Opacity.Should().Be(1);
        Part(item, "PART_TrailingCheckHost").IsVisible.Should().BeTrue();
        Part(item, "PART_TrailingIconHost").Opacity.Should().Be(0);
        MenuAssist.SetCheckmarkPlacement(item, MenuCheckmarkPlacement.None);
        Layout();
        Part(item, "PART_TrailingCheckHost").IsVisible.Should().BeFalse();
        Part(item, "PART_TrailingIconHost").Opacity.Should().Be(1);
        item.IsChecked.Should().BeTrue();
        item.Icon.Should().BeSameAs(icon);
        MenuAssist.GetTrailingIcon(item).Should().BeSameAs(trailing);
    }

    [Fact]
    public void CheckmarkMotion_ShouldKeepTheCheckAndTextTogetherAndCancelCleanly()
    {
        var previous = MotionSettings.GlobalScheme;
        MotionSettings.GlobalScheme = MotionScheme.Expressive;
        try
        {
            var item = new MenuItem { Header = "Show guides", ToggleType = MenuItemToggleType.CheckBox };
            var menu = Open(item);
            MenuAssist.SetIsAnimationEnabled(menu, true);
            var label = Part(item, "PART_Label");
            var check = Part(item, "PART_Check");
            var row = Part(item, "PART_Row");
            var start = label.TranslatePoint(default, row)!.Value.X;
            item.IsChecked = true;
            Layout();
            label.TranslatePoint(default, row)!.Value.X.Should().BeApproximately(start, .2);
            (label.RenderTransform?.Value.M31 ?? 0).Should().BeLessThan(-1);
            double? distance = null;
            for (var i = 0; i < 5; i++)
            {
                Pump(20);
                var current = label.TranslatePoint(default, row)!.Value.X - check.TranslatePoint(default, row)!.Value.X;
                if (distance is { } expected) current.Should().BeApproximately(expected, .1);
                distance = current;
            }

            item.IsChecked = false;
            Pump(30);
            item.IsChecked = true;
            Pump(500);
            label.RenderTransform.Should().BeNull();
            Part(item, "PART_LeadingCheckHost").Opacity.Should().Be(1);
            item.IsChecked = false;
            Pump(25);
            MenuAssist.SetIsAnimationEnabled(menu, false);
            Layout();
            label.RenderTransform.Should().BeNull();
            Part(item, "PART_LeadingCheckHost").IsVisible.Should().BeFalse();
            MenuAssist.SetIsAnimationEnabled(menu, true);
            MotionSettings.ReduceMotion = true;
            item.IsChecked = true;
            Layout();
            (label.RenderTransform?.Value.M31 ?? 0).Should().Be(0);
            Pump(500);
            label.RenderTransform.Should().BeNull();
        }
        finally
        {
            MotionSettings.GlobalScheme = previous;
        }
    }

    [Fact]
    public void Flyout_ShouldCarryCheckmarkPlacementIntoSubmenusWithLocalPriority()
    {
        var child = new MenuItem { Header = "Child", IsChecked = true };
        var parent = new MenuItem { Header = "Parent", Items = { child } };
        var flyout = new MenuFlyout { Items = { parent } };
        MenuAssist.SetCheckmarkPlacement(flyout, MenuCheckmarkPlacement.Trailing);
        MenuAssist.SetIsAnimationEnabled(flyout, false);
        var button = new Button { Content = "Open", Flyout = flyout };
        Window(button);
        flyout.ShowAt(button);
        Layout();
        parent.IsSubMenuOpen = true;
        Layout();
        MenuAssist.GetCheckmarkPlacement(parent).Should().Be(MenuCheckmarkPlacement.Trailing);
        MenuAssist.GetCheckmarkPlacement(child).Should().Be(MenuCheckmarkPlacement.Trailing);
        Part(child, "PART_TrailingCheckHost").IsVisible.Should().BeTrue();
        MenuAssist.SetCheckmarkPlacement(child, MenuCheckmarkPlacement.None);
        MenuAssist.SetCheckmarkPlacement(flyout, MenuCheckmarkPlacement.Leading);
        Layout();
        MenuAssist.GetCheckmarkPlacement(parent).Should().Be(MenuCheckmarkPlacement.Leading);
        MenuAssist.GetCheckmarkPlacement(child).Should().Be(MenuCheckmarkPlacement.None);
        flyout.Hide();
    }

    [Fact]
    public void AddingInsideAnOpenGroup_ShouldKeepGapOrderAndAnimateItsSize()
    {
        var menu = Open(new MenuItem { Header = "Open" }, new MenuGap(),
            new MenuGroupLabel { Text = "Recent" }, new MenuItem { Header = "Existing" }, new MenuGap(),
            new MenuItem { Header = "Preferences" });
        MenuAssist.SetIsAnimationEnabled(menu, true);
        Pump(50);
        var panel = menu.GetVisualDescendants().OfType<Panel>().Single(p => p.GetType().Name == "MenuItemsPanel");
        var start = panel.Bounds.Height;
        var inserted = new MenuItem { Header = "Untitled project" };
        menu.Items.Insert(4, inserted);
        Pump(30);
        var early = panel.Bounds.Height;
        Pump(550);
        panel.Bounds.Height.Should().BeGreaterThan(early + 1);
        early.Should().BeLessThan(start + inserted.Bounds.Height);
        panel.Children.IndexOf(inserted).Should().BeGreaterThanOrEqualTo(0);
        var existing = (MenuItem)menu.Items[3]!;
        inserted.Bounds.Y.Should().BeGreaterThan(existing.Bounds.Bottom - 1);
        menu.Items.Remove(inserted);
        Pump(600);
        panel.Bounds.Height.Should().BeApproximately(start, 1);
        menu.Items.Insert(4, inserted);
        Pump(25);
        menu.Items.Insert(4, new MenuItem { Header = "Another" });
        Pump(600);
        foreach (var row in menu.Items.OfType<MenuItem>()) row.RenderTransform.Should().BeNull();
        MenuAssist.SetIsAnimationEnabled(menu, false);
    }

    [Fact]
    public void TrailingCheckmark_ShouldExpandItsSlotWithoutMovingTheLabel()
    {
        var item = new MenuItem { Header = "Print copy", ToggleType = MenuItemToggleType.CheckBox };
        MenuAssist.SetCheckmarkPlacement(item, MenuCheckmarkPlacement.Trailing);
        var menu = Open(item);
        MenuAssist.SetIsAnimationEnabled(menu, true);
        var label = Part(item, "PART_Label");
        var slot = Part(item, "PART_TrailingSlot");
        var start = label.Bounds.X;
        item.IsChecked = true;
        Pump(30);
        slot.Width.Should().BeInRange(.1, 19.9);
        label.Bounds.X.Should().Be(start);
        Pump(600);
        slot.Width.Should().Be(20);
        label.RenderTransform.Should().BeNull();
        Part(item, "PART_TrailingCheckHost").RenderTransform.Should().BeNull();
        item.IsChecked = false;
        Pump(30);
        slot.Width.Should().BeInRange(.1, 19.9);
        MenuAssist.SetIsAnimationEnabled(menu, false);
        Layout();
        slot.Width.Should().Be(0);
    }

    private ContextMenu Open(params Control[] items)
    {
        var menu = new ContextMenu();
        foreach (var item in items) menu.Items.Add(item);
        MenuAssist.SetIsAnimationEnabled(menu, false);
        var button = new Button { Content = "Menu", ContextMenu = menu };
        Window(button);
        menu.Open(button);
        Layout();
        return menu;
    }

    private Window Window(Control content)
    {
        var window = new Window { Width = 600, Height = 500, Content = content };
        _windows.Add(window);
        window.Show();
        Layout();
        return window;
    }

    private void PressEnter(MenuItem item)
    {
        item.Focus();
        TopLevel.GetTopLevel(item)!.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Layout();
    }

    private void Layout()
    {
        Dispatcher.UIThread.RunJobs();
        foreach (var window in _windows) window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private void Pump(int milliseconds)
    {
        Layout();
        using var cancellation = new CancellationTokenSource(milliseconds);
        Dispatcher.UIThread.MainLoop(cancellation.Token);
        Layout();
    }

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Dispatcher.UIThread.RunJobs();
        MotionSettings.ReduceMotion = _reduceMotion;
        Application.Current!.Styles.Remove(_theme);
        Application.Current.Styles.Remove(_styles);
        Application.Current.Resources.MergedDictionaries.Remove(_resources);
    }
}