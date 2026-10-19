using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
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
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class SwitchTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];
    private readonly bool _reduceMotion;

    public SwitchTests()
    {
        TestApp.EnsureStarted();
        _reduceMotion = MotionSettings.ReduceMotion;
        _theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
        MotionSettings.ReduceMotion = true;
    }

    [Fact]
    public void EntireRow_PreservesNativeBindingCommandsKeyboardAndAutomation()
    {
        var source = new CheckBox();
        var command = new CountingCommand();
        var control = new ToggleSwitch
            { Content = "_Notifications", HorizontalAlignment = HorizontalAlignment.Stretch, Command = command };
        using var binding = control.Bind(ToggleButton.IsCheckedProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = source, Mode = BindingMode.TwoWay });
        var window = Show(control);
        var clicks = 0;
        control.Click += (_, _) => clicks++;
        foreach (var point in new[] { new Point(.5, .5), new Point(90, 24), new Point(250, 24), new Point(399, 47) })
        {
            var previous = control.IsChecked;
            Click(window, control, point);
            control.IsChecked.Should().Be(!previous);
            source.IsChecked.Should().Be(control.IsChecked);
        }

        command.Count.Should().Be(4);
        clicks.Should().Be(4);
        control.Focus(NavigationMethod.Tab);
        PressKey(window, Key.Space);
        control.IsChecked.Should().BeTrue();
        PressKey(window, Key.Enter);
        control.IsChecked.Should().BeFalse();
        command.Count.Should().Be(6);
        var peer = ControlAutomationPeer.CreatePeerForElement(control)!;
        ((IToggleProvider)peer).Toggle();
        source.IsChecked.Should().BeTrue();
        peer.GetName().Should().Contain("Notifications");
        control.IsEnabled = false;
        Click(window, control, new Point(250, 24));
        source.IsChecked.Should().BeTrue();
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Density_ShrinksSurroundingsBeforeTheTrackAndPreservesLabelPosition(FlowDirection direction)
    {
        var control = new ToggleSwitch { Content = "Label", FlowDirection = direction };
        var panel = new StackPanel { Children = { control } };
        DensityAssist.SetDensity(panel, MaterialDensity.Dense5);
        var window = Show(panel);
        control.Bounds.Height.Should().Be(48);
        var label = control.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "Label");
        var labelX = label.TranslatePoint(default, control)!.Value.X;
        foreach (var (density, size) in new[]
                 {
                     (MaterialDensity.Default, 48d), (MaterialDensity.Dense1, 44d),
                     (MaterialDensity.Dense2, 40d), (MaterialDensity.Dense3, 36d), (MaterialDensity.Dense4, 32d),
                     (MaterialDensity.Dense5, 28d)
                 })
        {
            DensityAssist.SetDensity(control, density);
            control.IsChecked = false;
            Layout(window);
            control.Bounds.Height.Should().Be(size);
            label.TranslatePoint(default, control)!.Value.X.Should().BeApproximately(labelX, .01);
            var layout = Part<SwitchContentMotion>(control, "PART_InteractionArea");
            layout.IndicatorScale.Should().BeApproximately(Math.Min(1, size / 32), .001);
            var host = Part<SwitchKnobHost>(control, "PART_SwitchKnob");
            (host.CircleSize * layout.IndicatorScale).Should().BeApproximately(Math.Min(40, size), .001);
            var handle = Part<SwitchHandle>(control, "PART_Handle");
            handle.HandleWidth.Should().Be(16);
            control.Focus(NavigationMethod.Tab);
            Layout(window);
            var track = Part<Border>(control, "PART_Track");
            var adorner =
                AdornerLayer.GetAdornerLayer(control)!.Children.Single(x =>
                    AdornerLayer.GetAdornedElement(x) == track);
            adorner.Bounds.Width.Should().BeApproximately(track.Bounds.Width + 10, 1);
            adorner.Bounds.Height.Should().BeApproximately(track.Bounds.Height + 10, 1);
            FocusRing.GetCornerRadius(track).Should().Be(track.CornerRadius);
            var position = track.TranslatePoint(default, control);
            control.IsChecked = !control.IsChecked;
            Layout(window);
            track.TranslatePoint(default, control).Should().Be(position);
            AdornerLayer.GetAdornerLayer(control)!.Children.Should().Contain(adorner);
            Part<StateLayer>(control, "PART_StateLayer").GetVisualAncestors()
                .TakeWhile(x => x != control).Should().OnlyContain(x => !x.ClipToBounds);
        }

        control.Content = null;
        Layout(window);
        control.Bounds.Height.Should().Be(28);
        control.Bounds.Width.Should().BeInRange(45.5, 46);
        control.Resources["MdCompSwitchTrackHeight"] = 40d;
        Layout(window);
        Part<SwitchContentMotion>(control, "PART_InteractionArea").IndicatorScale.Should().BeApproximately(.7, .001);
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void NativeDrag_SelectsInBothDirectionsAndDoesNotDoubleToggleOnRelease(FlowDirection direction)
    {
        var control = new ToggleSwitch { Content = "Drag", FlowDirection = direction };
        var window = Show(control);
        var moving = Part<Grid>(control, "PART_MovingKnobs");
        var host = Part<SwitchKnobHost>(control, "PART_SwitchKnob");
        var track = Part<Border>(control, "PART_Track");
        var trackPosition = track.TranslatePoint(default, control);

        void Drag(double delta, bool? expected)
        {
            var start = moving.TranslatePoint(new Point(moving.Bounds.Width / 2, moving.Bounds.Height / 2), window)!
                .Value;
            var initial = host.Position;
            window.MouseDown(start, MouseButton.Left);
            Part<SwitchHandle>(control, "PART_Handle").HandleWidth.Should().Be(28);
            window.MouseMove(start + new Vector(direction == FlowDirection.LeftToRight ? delta : -delta, 0));
            host.Position.Should().BeApproximately(Math.Clamp(initial + delta, 0, 20), .01);
            track.TranslatePoint(default, control).Should().Be(trackPosition);
            window.MouseUp(start + new Vector(direction == FlowDirection.LeftToRight ? delta : -delta, 0),
                MouseButton.Left);
            control.IsChecked.Should().Be(expected);
            control.IsPressed.Should().BeFalse();
            host.Position.Should().Be(expected == true ? 20 : 0);
        }

        Drag(18, true);
        Drag(-18, false);
        Drag(5, false);
        Click(window, control, new Point(1, 1));
        control.IsChecked.Should().BeTrue();
        Drag(-18, false);
    }

    [Fact]
    public void FocusRing_FollowsTrackShapeAndPreservesExplicitOverrides()
    {
        var control = new ToggleSwitch();
        var window = Show(control);
        var track = Part<Border>(control, "PART_Track");
        control.Resources["MdCompSwitchTrackShape"] = new CornerRadius(8);
        Layout(window);
        track.CornerRadius.Should().Be(new CornerRadius(8));
        FocusRing.GetCornerRadius(track).Should().Be(track.CornerRadius);
        control.CornerRadius = new CornerRadius(12);
        Layout(window);
        track.CornerRadius.Should().Be(control.CornerRadius);
        FocusRing.GetCornerRadius(track).Should().Be(track.CornerRadius);
        FocusRing.SetCornerRadius(control, new CornerRadius(4));
        FocusRing.SetBrush(control, Brushes.Red);
        FocusRing.SetThickness(control, new Thickness(2));
        FocusRing.SetOffset(control, new Thickness(3));
        Layout(window);
        FocusRing.GetCornerRadius(track).Should().Be(new CornerRadius(4));
        FocusRing.GetBrush(track).Should().BeSameAs(Brushes.Red);
        FocusRing.GetThickness(track).Should().Be(new Thickness(2));
        FocusRing.GetOffset(track).Should().Be(new Thickness(3));
        track.CornerRadius.Should().Be(new CornerRadius(12));
    }

    [Fact]
    public void ConfigurationsAndNativeContent_UpdateWithoutChangingSwitchLayout()
    {
        var control = new ToggleSwitch
            { Content = "Setting", OnContent = "Enabled detail", OffContent = "Disabled detail" };
        var window = Show(control);
        var handle = Part<SwitchHandle>(control, "PART_Handle");
        var bounds = control.Bounds;
        handle.HandleWidth.Should().Be(16);
        handle.UnselectedVisibility.Should().Be(0);
        SwitchAssist.SetIconConfiguration(control, SwitchIconConfiguration.SelectedAndUnselected);
        handle.HandleWidth.Should().Be(24);
        handle.UnselectedVisibility.Should().Be(1);
        SwitchAssist.GetSelectedIcon(control).Should().BeOfType<MaterialSymbol>();
        control.IsChecked = true;
        handle.SelectedVisibility.Should().Be(1);
        handle.UnselectedVisibility.Should().Be(0);
        Part<ContentPresenter>(control, "PART_OnContentPresenter").Opacity.Should().Be(1);
        Part<ContentPresenter>(control, "PART_OffContentPresenter").Opacity.Should().Be(0);
        SwitchAssist.SetSelectedIcon(control, Geometry.Parse("M0,0 L1,1"));
        Part<IconPresenter>(control, "PART_SelectedIcon").Value.Should().BeOfType<StreamGeometry>();
        SwitchAssist.SetIconConfiguration(control, SwitchIconConfiguration.Selected);
        control.IsChecked = false;
        handle.HandleWidth.Should().Be(16);
        handle.SelectedAngle.Should().Be(-45);
        Layout(window);
        control.Bounds.Should().Be(bounds);
        SwitchAssist.SetLabelPlacement(control, SwitchLabelPlacement.Trailing);
        Layout(window);
        Part<Grid>(control, "PART_Label").Bounds.X.Should().BeGreaterThan(52);
        control.IsChecked = true;
        control.IsChecked = null;
        handle.IsSelected.Should().BeFalse();
        Part<SwitchKnobHost>(control, "PART_SwitchKnob").Position.Should().Be(0);
    }

    [Fact]
    public void TokensValidationAndDisabledLayers_RemainIndependent()
    {
        var control = new ToggleSwitch { Content = "Setting", IsChecked = true };
        SwitchAssist.SetIconConfiguration(control, SwitchIconConfiguration.Selected);
        control.Resources["Local"] = Brushes.Red;
        control.Resources["MdCompSwitchSelectedTrackBrush"] = new TokenAlias("Local");
        var window = Show(control);
        var track = Part<Border>(control, "PART_Track");
        track.Background.Should().BeSameAs(Brushes.Red);
        control.Resources["Local"] = Brushes.Blue;
        track.Background.Should().BeSameAs(Brushes.Blue);
        control.Classes.Add("m3-hovered");
        var handle = Part<SwitchHandle>(control, "PART_Handle");
        handle.Brush.Should().NotBeNull();
        DataValidationErrors.SetErrors(control, new[] { "Preference is unavailable." });
        Layout(window);
        control.GetVisualDescendants().OfType<TextBlock>().Should()
            .Contain(x => x.Text == "Preference is unavailable.");
        control.IsEnabled = false;
        track.Opacity.Should().Be(.12);
        handle.HandleOpacity.Should().Be(1);
        handle.SelectedIconOpacity.Should().Be(.38);
        Part<StateLayer>(control, "PART_StateLayer").Opacity.Should().Be(0);
        Part<InkRipple>(control, "PART_Ripple").IsVisible.Should().BeFalse();
        control.IsChecked = false;
        handle.HandleOpacity.Should().Be(.38);
        handle.HandleWidth.Should().Be(16);
        control.IsEnabled = true;
        control.IsChecked = true;
        track.Opacity.Should().Be(1);
        handle.HandleOpacity.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Motion_ReversesInPlaceAndPreservesLayout(bool reduceMotion)
    {
        MotionSettings.ReduceMotion = reduceMotion;
        var control = new ToggleSwitch { Content = "Animated" };
        SwitchAssist.SetIconConfiguration(control, SwitchIconConfiguration.Selected);
        var window = Show(control, true);
        var handle = Part<SwitchHandle>(control, "PART_Handle");
        var host = Part<SwitchKnobHost>(control, "PART_SwitchKnob");
        var bounds = control.Bounds;
        control.IsChecked = true;
        if (!reduceMotion)
        {
            PumpUntil(() => host.Position is > 0 and < 19 && handle.HandleWidth is > 16 and < 23.9);
            var position = host.Position;
            control.IsChecked = false;
            host.Position.Should().BeApproximately(position, .01);
            PumpUntil(() => host.Position < .1);
            control.IsChecked = true;
        }

        PumpUntil(() => host.Position >= 19.99 && handle.SelectedVisibility >= .999 && handle.HandleWidth >= 23.99);
        Layout(window);
        control.Bounds.Should().Be(bounds);
        var center = host.Position;
        control.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        PumpUntil(() => handle.HandleWidth >= 27.99);
        host.Position.Should().BeApproximately(center, .1);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        PumpUntil(() => host.Position < .01 && handle.HandleWidth < 16.01);
        control.IsEnabled = false;
        control.IsChecked = true;
        host.Position.Should().Be(20);
        handle.HandleWidth.Should().Be(24);
    }

    [Fact]
    public void ExplicitKnobTransitions_RemainInControlOfNativePositionMotion()
    {
        var transitions = new Transitions
            { new DoubleTransition { Property = Canvas.LeftProperty, Duration = TimeSpan.FromMilliseconds(250) } };
        var control = new ToggleSwitch { KnobTransitions = transitions };
        var window = Show(control, true);
        Part<Grid>(control, "PART_MovingKnobs").Transitions.Should().BeSameAs(transitions);
        var host = Part<SwitchKnobHost>(control, "PART_SwitchKnob");
        control.IsChecked = true;
        PumpUntil(() => host.Position is > 0 and < 19);
        PumpUntil(() => host.Position >= 19.99);
        control.IsChecked.Should().BeTrue();
    }

    [Fact]
    public void InitialSelectionAndReattachment_DoNotAnimateFromTheOppositeState()
    {
        MotionSettings.ReduceMotion = false;
        var control = new ToggleSwitch { IsChecked = true };
        var panel = new StackPanel { Children = { control } };
        var window = Show(panel, true);
        Part<SwitchKnobHost>(control, "PART_SwitchKnob").Position.Should().Be(20);
        Part<SwitchHandle>(control, "PART_Handle").HandleWidth.Should().Be(24);
        panel.Children.Remove(control);
        control.IsChecked = false;
        panel.Children.Add(control);
        Layout(window);
        Part<SwitchKnobHost>(control, "PART_SwitchKnob").Position.Should().Be(0);
        Part<SwitchHandle>(control, "PART_Handle").HandleWidth.Should().Be(16);
    }

    private Window Show(Control content, bool keepTransitions = false)
    {
        var window = new Window { Width = 400, Height = 350, Content = content };
        _windows.Add(window);
        window.Show();
        Layout(window);
        if (!keepTransitions)
        {
            foreach (var visual in content.GetVisualDescendants().OfType<Animatable>().Prepend(content))
                visual.Transitions = null;
            foreach (var control in content.GetVisualDescendants().OfType<ToggleSwitch>()
                         .Concat(content is ToggleSwitch s ? new[] { s } : [])) control.KnobTransitions = null!;
        }

        return window;
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static T Part<T>(ToggleSwitch control, string name) where T : Control => control.GetVisualDescendants()
        .OfType<T>().Single(x => x.Name == name && x.TemplatedParent == control);

    private static void Click(Window window, Control control, Point point)
    {
        var position = control.TranslatePoint(point, window)!.Value;
        window.MouseDown(position, MouseButton.Left);
        window.MouseUp(position, MouseButton.Left);
    }

    private static void PressKey(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, key == Key.Space ? " " : null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, key == Key.Space ? " " : null);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(2))
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