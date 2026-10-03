using Avalonia;
using Avalonia.Animation;
using Avalonia.Input.Raw;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class IconButtonStateTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];

    public IconButtonStateTests()
    {
        TestApp.EnsureStarted();
        _theme = new MaterialTheme { MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
    }

    public static IEnumerable<object[]> VariantsAndSelections()
    {
        foreach (var variant in Enum.GetValues<IconButtonVariant>())
        foreach (var selection in new[] { "ordinary", "unselected", "selected", "indeterminate" })
            yield return [variant, selection];
    }

    [Theory]
    [MemberData(nameof(VariantsAndSelections))]
    public void StateTokensRespectPriorityAndRuntimeRemapping(IconButtonVariant variant, string selection)
    {
        var button = Create(variant, selection);
        var selected = selection == "selected";
        var prefix = $"MdCompIconButton{variant}" + (selected ? "Selected" :
            selection != "ordinary" && variant == IconButtonVariant.Filled ? "Unselected" : "");
        button.Resources[prefix + "HoveredIconBrush"] = Brushes.Red;
        button.Resources[prefix + "FocusedIconBrush"] = Brushes.Green;
        button.Resources[prefix + "PressedIconBrush"] = Brushes.Blue;
        button.Resources[prefix + "HoveredStateLayerBrush"] = Brushes.Yellow;
        button.Resources[prefix + "FocusedStateLayerBrush"] = Brushes.Orange;
        button.Resources[prefix + "PressedStateLayerBrush"] = Brushes.Purple;
        button.Resources["MdCompIconButtonHoveredStateLayerOpacity"] = .21;
        button.Resources["MdCompIconButtonFocusedStateLayerOpacity"] = .32;
        button.Resources["MdCompIconButtonPressedStateLayerOpacity"] = .43;
        button.Resources["MdCompIconButtonSelectedHoveredStateLayerOpacity"] = .21;
        button.Resources["MdCompIconButtonSelectedFocusedStateLayerOpacity"] = .32;
        button.Resources["MdCompIconButtonSelectedPressedStateLayerOpacity"] = .67;
        Show(button);
        var icon = Part<IconPresenter>(button);
        var layer = Part<StateLayer>(button);
        var ripple = Part<InkRipple>(button);
        icon.Transitions = null;
        icon.IsFilled.Should().Be(selection == "ordinary" || selected);
        ripple.Brush.Should().BeSameAs(Brushes.Purple);
        ripple.BaseOpacity.Should().Be(selected ? .67 : .43);

        var states = (IPseudoClasses)button.Classes;
        button.Classes.Add("m3-hovered");
        icon.Foreground.Should().BeSameAs(Brushes.Red);
        layer.Brush.Should().BeSameAs(Brushes.Yellow);
        layer.Opacity.Should().Be(.21);
        states.Set(":focus-visible", true);
        icon.Foreground.Should().BeSameAs(Brushes.Green);
        layer.Brush.Should().BeSameAs(Brushes.Orange);
        layer.Opacity.Should().Be(.32);
        states.Set(":pressed", true);
        button.Foreground.Should().BeSameAs(Brushes.Blue);
        icon.Foreground.Should().BeSameAs(Brushes.Blue);
        layer.Brush.Should().BeSameAs(Brushes.Purple);
        layer.Opacity.Should().Be(selected ? .67 : .43);

        button.Resources["CustomPressed"] = Brushes.Cyan;
        button.Resources[prefix + "PressedIconBrush"] = new TokenAlias("CustomPressed");
        button.Resources[prefix + "PressedStateLayerBrush"] = new TokenAlias("CustomPressed");
        icon.Foreground.Should().BeSameAs(Brushes.Cyan);
        ripple.Brush.Should().BeSameAs(Brushes.Cyan);
        button.Resources["CustomPressed"] = Brushes.Magenta;
        icon.Foreground.Should().BeSameAs(Brushes.Magenta);
        layer.Brush.Should().BeSameAs(Brushes.Magenta);
        ripple.Brush.Should().BeSameAs(Brushes.Magenta);

        button.IsEnabled = false;
        layer.Opacity.Should().Be(0);
        ripple.Brush.Should().BeNull();
        icon.Opacity.Should().Be(.38);
        states.Set(":pressed", false);
        button.IsEnabled = true;
        icon.Opacity.Should().Be(1);
        icon.Foreground.Should().BeSameAs(Brushes.Green);
        layer.Opacity.Should().Be(.32);
        ripple.Brush.Should().BeSameAs(Brushes.Magenta);
        button.Classes.Add("m3-hovered");
        states.Set(":focus-visible", false);
        icon.Foreground.Should().BeSameAs(Brushes.Red);
        if (button is IconToggleButton toggle)
        {
            toggle.Resources["CustomSelectedOpacity"] = .78;
            toggle.Resources["MdCompIconButtonSelectedPressedStateLayerOpacity"] =
                new TokenAlias("CustomSelectedOpacity");
            toggle.IsChecked = true;
            ripple.BaseOpacity.Should().Be(.78);
            toggle.Resources["CustomSelectedOpacity"] = .89;
            ripple.BaseOpacity.Should().Be(.89);
            toggle.IsChecked = false;
            ripple.BaseOpacity.Should().Be(.43);
        }
    }

    [Theory]
    [MemberData(nameof(VariantsAndSelections))]
    public void DisabledContainersRespectVariantAndSelection(IconButtonVariant variant, string selection)
    {
        var button = Create(variant, selection);
        button.Resources["MdCompIconButtonDisabledContainerBrush"] = Brushes.Magenta;
        button.Resources["MdCompIconButtonDisabledOutlineBrush"] = Brushes.Cyan;
        Show(button);
        var enabledBackground = button.Background;
        button.IsEnabled = false;
        var filled = variant is IconButtonVariant.Filled or IconButtonVariant.Tonal ||
                     variant == IconButtonVariant.Outlined && selection == "selected";
        var background = button.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "PART_Background");
        background.Opacity.Should().Be(filled ? .1 : 1);
        if (filled)
            button.Background.Should().BeSameAs(Brushes.Magenta);
        else
            ((ISolidColorBrush)button.Background!).Color.A.Should().Be(0);
        if (variant == IconButtonVariant.Outlined && selection != "selected")
        {
            button.BorderBrush.Should().BeSameAs(Brushes.Cyan);
            button.BorderThickness.Should().Be(new Thickness(1));
        }
        else
            button.BorderThickness.Should().Be(new Thickness(0));

        button.IsEnabled = true;
        button.Background.Should().BeSameAs(enabledBackground);
        background.Opacity.Should().Be(1);
    }

    [Theory]
    [InlineData(IconButtonVariant.Standard)]
    [InlineData(IconButtonVariant.Filled)]
    [InlineData(IconButtonVariant.Tonal)]
    [InlineData(IconButtonVariant.Outlined)]
    public void NativeInputSwitchesSelectionAndRestoresRippleAfterDisabling(IconButtonVariant variant)
    {
        var toggle = new IconToggleButton { Variant = variant, Content = MaterialSymbol.Favorite };
        var prefix = $"MdCompIconButton{variant}";
        toggle.Resources
                [prefix + (variant == IconButtonVariant.Filled ? "Unselected" : "") + "PressedStateLayerBrush"] =
            Brushes.Blue;
        toggle.Resources[prefix + "SelectedPressedStateLayerBrush"] = Brushes.Red;
        var window = Show(toggle);
        var ripple = Part<InkRipple>(toggle);
        var point = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        toggle.IsPressed.Should().BeTrue();
        ripple.Brush.Should().BeSameAs(Brushes.Blue);
        window.MouseUp(point, MouseButton.Left);
        toggle.IsChecked.Should().BeTrue();
        ripple.Brush.Should().BeSameAs(Brushes.Red);
        toggle.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        toggle.IsPressed.Should().BeTrue();
        toggle.IsEnabled = false;
        ripple.Brush.Should().BeNull();
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        toggle.IsEnabled = true;
        toggle.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        ripple.Brush.Should().BeSameAs(Brushes.Red);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        toggle.IsChecked.Should().BeFalse();
        ripple.Brush.Should().BeSameAs(Brushes.Blue);
    }

    [Fact]
    public void OrdinaryDescendantsKeepIndependentAssistAndClassMappings()
    {
        var button = new OrdinaryButton { Content = "Button" };
        var toggle = new OrdinaryToggle { Content = "Toggle" };
        button.Classes.Add("lg");
        button.Classes.Add("square");
        toggle.Classes.Add("md");
        toggle.Classes.Add("square");
        ButtonAssist.SetIcon(button, MaterialSymbol.Search);
        ToggleButtonAssist.SetIcon(toggle, MaterialSymbol.Favorite);
        Show(new StackPanel { Children = { button, toggle } });
        button.Bounds.Height.Should().Be(96);
        toggle.Bounds.Height.Should().Be(56);
        Part<IconPresenter>(button).Value.Should().Be(MaterialSymbol.Search);
        Part<IconPresenter>(toggle).Value.Should().Be(MaterialSymbol.Favorite);
        ButtonAssist.GetShape(button).Should().Be(ButtonShape.Square);
        ToggleButtonAssist.GetShape(toggle).Should().Be(ButtonShape.Square);
    }

    private static Button Create(IconButtonVariant variant, string selection) => selection == "ordinary"
        ? new IconButton { Variant = variant, Content = MaterialSymbol.Search }
        : new IconToggleButton
        {
            Variant = variant, Content = MaterialSymbol.Favorite,
            IsChecked = selection == "indeterminate" ? null : selection == "selected"
        };

    private Window Show(Control content)
    {
        var window = new Window { Width = 400, Height = 300, Content = content };
        _windows.Add(window);
        window.Show();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        foreach (var control in content.GetVisualDescendants().Prepend(content))
            if (control is Animatable animatable)
                animatable.Transitions = null;
        return window;
    }

    private static T Part<T>(Button button) where T : Control => button.GetVisualDescendants().OfType<T>().Single();

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Application.Current!.Styles.Remove(_theme);
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class OrdinaryButton : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);
    }

    private sealed class OrdinaryToggle : ToggleButton
    {
        protected override Type StyleKeyOverride => typeof(ToggleButton);
    }
}