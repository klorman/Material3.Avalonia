using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Density;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class RadioButtonTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];
    private readonly bool _reduceMotion;

    public RadioButtonTests()
    {
        TestApp.EnsureStarted();
        _reduceMotion = MotionSettings.ReduceMotion;
        _theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
        MotionSettings.ReduceMotion = true;
    }

    [Fact]
    public void EntireRow_SelectsOnceAndPreservesNativeBindingCommandAndAutomation()
    {
        var source = new CheckBox();
        var command = new CountingCommand();
        var radio = new RadioButton
            { Content = "Label", Command = command, HorizontalAlignment = HorizontalAlignment.Stretch };
        using var binding = radio.Bind(ToggleButton.IsCheckedProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = source, Mode = BindingMode.TwoWay });
        var previous = new RadioButton { Content = "Previous", IsChecked = true };
        var panel = new StackPanel { Children = { previous, radio } };
        var window = Show(panel);
        var clicks = 0;
        radio.Click += (_, _) => clicks++;
        foreach (var point in new[] { new Point(0.5, 0.5), new Point(24, 24), new Point(65, 24), new Point(350, 47.5) })
        {
            Click(window, radio, point);
            radio.IsChecked.Should().BeTrue();
            previous.IsChecked.Should().BeFalse();
            source.IsChecked.Should().BeTrue();
        }

        clicks.Should().Be(4);
        command.Count.Should().Be(4);
        var peer = ControlAutomationPeer.CreatePeerForElement(radio)!;
        peer.GetAutomationControlType().Should().Be(AutomationControlType.RadioButton);
        ((ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(previous)!).Select();
        source.IsChecked.Should().BeFalse();
        radio.Focus(NavigationMethod.Tab);
        PressKey(window, Key.Space);
        radio.IsChecked.Should().BeTrue();
        PressKey(window, Key.Space);
        radio.IsChecked.Should().BeTrue();
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Density_PreservesLabelAlignmentAndIndicatorSizeUntilTheCircleIsTooSmall(FlowDirection direction)
    {
        var radio = new RadioButton { Content = "Density", IsChecked = true, FlowDirection = direction };
        var panel = new StackPanel { Children = { radio } };
        DensityAssist.SetDensity(panel, MaterialDensity.Dense5);
        var window = Show(panel);
        radio.Bounds.Height.Should().Be(48);
        var label = radio.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "Density");
        var labelX = label.TranslatePoint(default, radio)!.Value.X;
        foreach (var (density, target) in new[]
                 {
                     (MaterialDensity.Default, 48d), (MaterialDensity.Dense1, 44d), (MaterialDensity.Dense2, 40d),
                     (MaterialDensity.Dense3, 36d), (MaterialDensity.Dense4, 32d), (MaterialDensity.Dense5, 28d)
                 })
        {
            DensityAssist.SetDensity(radio, density);
            Layout(window);
            radio.Bounds.Height.Should().Be(target);
            label.TranslatePoint(default, radio)!.Value.X.Should().Be(labelX);
            var circle = Part<Grid>(radio, "PART_FocusTarget");
            circle.Bounds.Size.Should().Be(new Size(Math.Min(40, target), Math.Min(40, target)));
            var indicator = Part<RadioButtonIndicator>(radio, "PART_Indicator");
            indicator.IndicatorScale.Should().Be(1);
            (indicator.DotSize / indicator.IconSize).Should().Be(0.5);
            (indicator.StrokeThickness / indicator.IconSize).Should().Be(0.1);
            radio.Focus(NavigationMethod.Tab);
            Layout(window);
            var layer = AdornerLayer.GetAdornerLayer(radio)!;
            layer.Children.Should().Contain(x => AdornerLayer.GetAdornedElement(x) == circle);
            foreach (var point in new[] { new Point(0.5, 0.5), new Point(radio.Bounds.Width - 0.5, target - 0.5) })
            {
                radio.IsChecked = false;
                Click(window, radio, point);
                radio.IsChecked.Should().BeTrue();
            }
        }

        radio.Content = null;
        Layout(window);
        radio.Bounds.Size.Should().Be(new Size(28, 28));

        radio.Resources["MdCompRadioButtonStateLayerSize"] = 16d;
        Layout(window);
        var smallIndicator = Part<RadioButtonIndicator>(radio, "PART_Indicator");
        smallIndicator.IndicatorScale.Should().BeApproximately(0.8, 0.001);
        (smallIndicator.IconSize * smallIndicator.IndicatorScale).Should().BeApproximately(16, 0.001);
        (smallIndicator.DotSize * smallIndicator.IndicatorScale).Should().BeApproximately(8, 0.001);
        (smallIndicator.StrokeThickness * smallIndicator.IndicatorScale).Should().BeApproximately(1.6, 0.001);
    }

    [Fact]
    public void NativeGroups_RemainIndependentAndSupportNamedMembersAcrossContainers()
    {
        var a = new RadioButton { GroupName = "named", IsChecked = true };
        var b = new RadioButton { GroupName = "named" };
        var c = new RadioButton { IsChecked = true };
        var d = new RadioButton();
        var window = Show(new StackPanel
        {
            Children = { new Border { Child = a }, new Border { Child = b }, new StackPanel { Children = { c, d } } }
        });
        Click(window, b, new Point(24, 24));
        a.IsChecked.Should().BeFalse();
        c.IsChecked.Should().BeTrue();
        Click(window, d, new Point(24, 24));
        c.IsChecked.Should().BeFalse();
        b.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void StateTokens_UpdateAtRuntimeAndDisabledValidationPreservesTheLabel()
    {
        var radio = new RadioButton { Content = "Preference" };
        radio.Resources["Local"] = Brushes.Blue;
        radio.Resources["MdCompRadioButtonUnselectedIconBrush"] = new TokenAlias("Local");
        radio.Resources["MdCompRadioButtonUnselectedHoverIconBrush"] = Brushes.Green;
        radio.Resources["MdCompRadioButtonUnselectedFocusIconBrush"] = Brushes.Magenta;
        radio.Resources["MdCompRadioButtonUnselectedPressedIconBrush"] = Brushes.Red;
        radio.Resources["MdCompRadioButtonUnselectedPressedStateLayerBrush"] = Brushes.Yellow;
        var window = Show(radio);
        var indicator = Part<RadioButtonIndicator>(radio, "PART_Indicator");
        indicator.Brush.Should().BeSameAs(Brushes.Blue);
        radio.Resources["Local"] = Brushes.Orange;
        indicator.Brush.Should().BeSameAs(Brushes.Orange);
        radio.Classes.Add("m3-hovered");
        indicator.Brush.Should().BeSameAs(Brushes.Green);
        radio.Focus(NavigationMethod.Tab);
        indicator.Brush.Should().BeSameAs(Brushes.Magenta);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        indicator.Brush.Should().BeSameAs(Brushes.Red);
        Part<InkRipple>(radio, "PART_Ripple").Brush.Should().BeSameAs(Brushes.Yellow);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        DataValidationErrors.SetErrors(radio, new[] { "Choose an available preference." });
        Layout(window);
        radio.GetVisualDescendants().OfType<TextBlock>().Should()
            .Contain(x => x.Text == "Choose an available preference.");
        radio.IsEnabled = false;
        indicator.Opacity.Should().Be(0.38);
        Part<StateLayer>(radio, "PART_StateLayer").Opacity.Should().Be(0);
        Part<InkRipple>(radio, "PART_Ripple").IsVisible.Should().BeFalse();
        var selected = radio.IsChecked;
        Click(window, radio, new Point(24, 24));
        radio.IsChecked.Should().Be(selected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionMotion_StartsAtHalfSizeCanReverseAndDoesNotChangeLayout(bool reduceMotion)
    {
        MotionSettings.ReduceMotion = reduceMotion;
        var radio = new RadioButton { Content = "Animated" };
        var window = Show(radio, keepTransitions: true);
        var indicator = Part<RadioButtonIndicator>(radio, "PART_Indicator");
        var bounds = radio.Bounds;
        indicator.DotScale.Should().Be(0.5);
        indicator.DotOpacity.Should().Be(0);
        radio.IsChecked = true;
        if (!reduceMotion)
        {
            indicator.DotScale.Should().Be(0.5);
            PumpUntil(() => indicator.DotScale is > 0.5 and < 0.95 && indicator.DotOpacity > 0);
            var scale = indicator.DotScale;
            radio.IsChecked = false;
            indicator.DotScale.Should().BeApproximately(scale, 0.001);
            PumpUntil(() => indicator.DotOpacity < 0.01);
            radio.IsChecked = true;
        }

        PumpUntil(() => indicator.DotScale >= 0.999 && indicator.DotOpacity >= 0.999);
        Layout(window);
        radio.Bounds.Should().Be(bounds);
        radio.IsChecked = false;
        PumpUntil(() => indicator.DotOpacity <= 0.001);
        radio.IsEnabled = false;
        radio.IsChecked = true;
        indicator.DotScale.Should().Be(1);
        indicator.DotOpacity.Should().Be(1);
    }

    [Fact]
    public void InitialSelectionAndReattachment_DoNotPlayAnEntranceAnimation()
    {
        MotionSettings.ReduceMotion = false;
        var radio = new RadioButton { IsChecked = true };
        var panel = new StackPanel { Children = { radio } };
        var window = Show(panel, keepTransitions: true);
        Part<RadioButtonIndicator>(radio, "PART_Indicator").DotOpacity.Should().Be(1);
        panel.Children.Remove(radio);
        radio.IsChecked = false;
        panel.Children.Add(radio);
        Layout(window);
        Part<RadioButtonIndicator>(radio, "PART_Indicator").DotOpacity.Should().Be(0);
    }

    [Fact]
    public void GroupNavigation_EntersSelectedWrapsSkipsUnavailableAndLeavesWithTab()
    {
        var first = new RadioButton { Content = "First", GroupName = "navigation" };
        var disabled = new RadioButton { IsEnabled = false, GroupName = "navigation" };
        var hidden = new RadioButton { IsVisible = false, GroupName = "navigation" };
        var last = new RadioButton { Content = "Last", GroupName = "navigation", IsChecked = true };
        var group = new StackPanel
            { Children = { new Border { Child = first }, disabled, hidden, new Border { Child = last } } };
        RadioButtonAssist.SetGroupNavigation(group, true);
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var window = Show(new StackPanel { Children = { before, group, after } });
        before.Focus();
        PressKey(window, Key.Tab);
        last.IsFocused.Should().BeTrue();
        PressKey(window, Key.Down);
        first.IsFocused.Should().BeTrue();
        first.IsChecked.Should().BeTrue();
        last.IsChecked.Should().BeFalse();
        PressKey(window, Key.Up);
        last.IsChecked.Should().BeTrue();
        PressKey(window, Key.Tab);
        after.IsFocused.Should().BeTrue();
        PressKey(window, Key.Tab, RawInputModifiers.Shift);
        last.IsFocused.Should().BeTrue();
        RadioButtonAssist.SetGroupNavigation(group, false);
        KeyboardNavigation.GetTabNavigation(group).Should().Be(KeyboardNavigationMode.Continue);
        PressKey(window, Key.Up);
        first.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void EmptyGroupNavigation_UsesFirstOrLastWithoutSelectingAndTracksDynamicMembers()
    {
        var first = new RadioButton { Content = "First" };
        var last = new RadioButton { Content = "Last" };
        var group = new StackPanel { Children = { first, last } };
        RadioButtonAssist.SetGroupNavigation(group, true);
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var window = Show(new StackPanel { Children = { before, group, after } });
        after.Focus();
        PressKey(window, Key.Tab, RawInputModifiers.Shift);
        last.IsFocused.Should().BeTrue();
        last.IsChecked.Should().NotBeTrue();
        before.Focus();
        PressKey(window, Key.Tab);
        first.IsFocused.Should().BeTrue();
        var added = new RadioButton { Content = "Added" };
        group.Children.Add(added);
        group.Children.Remove(last);
        Layout(window);
        PressKey(window, Key.Up);
        added.IsFocused.Should().BeTrue();
        added.IsChecked.Should().BeTrue();
        group.FlowDirection = FlowDirection.RightToLeft;
        PressKey(window, Key.Right);
        first.IsChecked.Should().BeTrue();
    }

    private Window Show(Control content, bool keepTransitions = false)
    {
        var window = new Window { Width = 400, Height = 350, Content = content };
        _windows.Add(window);
        window.Show();
        Layout(window);
        if (!keepTransitions)
            foreach (var visual in content.GetVisualDescendants().OfType<Animatable>().Prepend(content))
                visual.Transitions = null;
        return window;
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static T Part<T>(RadioButton radio, string name) where T : Control =>
        radio.GetVisualDescendants().OfType<T>().Single(x => x.Name == name && x.TemplatedParent == radio);

    private static void Click(Window window, Control control, Point point)
    {
        var position = control.TranslatePoint(point, window)!.Value;
        window.MouseDown(position, MouseButton.Left);
        window.MouseUp(position, MouseButton.Left);
    }

    private static void PressKey(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, key == Key.Space ? " " : null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, key == Key.Space ? " " : null);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        condition().Should().BeTrue();
    }

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Application.Current!.Styles.Remove(_theme);
        MotionSettings.ReduceMotion = _reduceMotion;
    }

    private sealed class CountingCommand : ICommand
    {
        public int Count { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Count++;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}