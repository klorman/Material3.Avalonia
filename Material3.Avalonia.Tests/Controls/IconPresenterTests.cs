using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Controls.Icons;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Theme;

namespace Material3.Avalonia.Tests.Controls;

public sealed class IconPresenterTests : IDisposable
{
    private readonly MaterialTheme _theme;
    private readonly List<Window> _windows = [];

    public IconPresenterTests()
    {
        TestApp.EnsureStarted();
        _theme = new MaterialTheme { Mode = ThemeMode.Light, MotionScheme = null };
        Application.Current!.Styles.Add(_theme);
    }

    [Fact]
    public void LocalThemeKeepsPaletteAndIconOverridesIndependentWithoutStaticIncludes()
    {
        var outer = new Button { Content = "Outer" };
        var inner = new Button { Content = "Inner" };
        ButtonAssist.SetVariant(outer, ButtonVariant.Filled);
        ButtonAssist.SetVariant(inner, ButtonVariant.Filled);
        var local = new StackPanel { Children = { inner } };
        var theme = new MaterialTheme { SourceColor = Colors.Green, Mode = ThemeMode.Light, MotionScheme = null };
        local.Styles.Add(theme);
        Show(new StackPanel { Children = { outer, local } });
        outer.Transitions = null;
        inner.Transitions = null;
        var outerColor = ((ISolidColorBrush)outer.Background!).Color;
        var innerColor = ((ISolidColorBrush)inner.Background!).Color;
        innerColor.Should().NotBe(outerColor);
        theme.SourceColor = Colors.Orange;
        Dispatcher.UIThread.RunJobs();
        ((ISolidColorBrush)inner.Background!).Color.Should().NotBe(innerColor);
        ((ISolidColorBrush)outer.Background!).Color.Should().Be(outerColor);
        local.Resources["MdCompButtonFilledContainerBrush"] = Brushes.Magenta;
        Dispatcher.UIThread.RunJobs();
        ((ISolidColorBrush)inner.Background!).Color.Should().Be(Colors.Magenta);
        inner.FindResource("MdIconCheck").Should().Be(MaterialSymbol.Check);
        local.Resources["MdIconCheck"] = MaterialSymbol.ArrowRight;
        inner.FindResource("MdIconCheck").Should().Be(MaterialSymbol.ArrowRight);
        outer.FindResource("MdIconCheck").Should().Be(MaterialSymbol.Check);
    }

    [Theory]
    [InlineData(ButtonSize.ExtraSmall, 20)]
    [InlineData(ButtonSize.Small, 20)]
    [InlineData(ButtonSize.Medium, 24)]
    [InlineData(ButtonSize.Large, 32)]
    [InlineData(ButtonSize.ExtraLarge, 40)]
    public void ButtonIconFollowsSizeAndRuntimeToken(ButtonSize size, double expected)
    {
        var button = new Button { Content = "Action" };
        ButtonAssist.SetSize(button, size);
        ButtonAssist.SetIcon(button, MaterialSymbol.Check);
        Show(button);
        var presenter = button.GetVisualDescendants().OfType<IconPresenter>().Single();
        presenter.Size.Should().Be(expected);
        presenter.LoadError.Should().BeNull();
        var suffix = size switch
        {
            ButtonSize.ExtraSmall => "Xs", ButtonSize.Small => "Sm", ButtonSize.Medium => "Md",
            ButtonSize.Large => "Lg", _ => "Xl"
        };
        button.Resources[$"MdCompButton{suffix}IconSize"] = 28d;
        Dispatcher.UIThread.RunJobs();
        presenter.Size.Should().Be(28);
    }

    [Fact]
    public void TextFieldIconFollowsItsToken()
    {
        var text = new TextBox();
        TextFieldAssist.SetLeadingIcon(text, MaterialSymbol.Check);
        Show(text);
        var presenter = text.GetVisualDescendants().OfType<IconPresenter>().First(x => x.Value is MaterialSymbol);
        presenter.Size.Should().Be(24);
        text.Resources["MdCompFilledTextFieldLeadingIconSize"] = 32d;
        text.Resources["MdCompOutlinedTextFieldLeadingIconSize"] = 32d;
        Dispatcher.UIThread.RunJobs();
        presenter.Size.Should().Be(32);
    }

    [Fact]
    public void CustomImagesHaveAutomaticOverridablePaddingWithoutStretchingOrDisposal()
    {
        var image = new RecordingImage();
        var icon = new IconPresenter { Value = image, Size = 24 };
        Render(icon, 80);
        image.Destination.Should().Be(new Rect(30, 35, 20, 10));
        icon.Padding = new Thickness(0);
        Render(icon, 80);
        image.Destination.Should().Be(new Rect(28, 34, 24, 12));
        icon.Size = 48;
        icon.Padding = null;
        Render(icon, 80);
        image.Destination.Should().Be(new Rect(20, 30, 40, 20));
        icon.Value = null;
        image.Disposed.Should().BeFalse();
    }

    [Fact]
    public void SymbolsPreserveViewportAndIndependentAxes()
    {
        var first = new IconPresenter { Value = MaterialSymbol.ContentCopy, IsFilled = false };
        var second = new IconPresenter { Value = MaterialSymbol.ContentCopy, IsFilled = true };
        Show(new StackPanel { Children = { first, second } });
        first.IsFilled = true;
        second.IsFilled.Should().BeTrue();
        first.LoadError.Should().BeNull();
        first.DesiredSize.Should().Be(new Size(24, 24));
    }

    [Fact]
    public void MaterialThemeProvidesAndUpdatesSymbolDefaults()
    {
        var icon = new IconPresenter { Value = MaterialSymbol.Check };
        Show(icon);

        icon.SymbolStyle.Should().Be(SymbolStyle.Outlined);
        icon.Weight.Should().Be(400);
        icon.Grade.Should().Be(0);
        icon.IsFilled.Should().BeFalse();
        icon.OpticalSize.Should().BeNull();

        _theme.SymbolStyle = SymbolStyle.Rounded;
        _theme.SymbolWeight = 500;
        _theme.SymbolGrade = 100;
        _theme.SymbolIsFilled = true;
        _theme.SymbolOpticalSize = 40;
        Dispatcher.UIThread.RunJobs();

        icon.SymbolStyle.Should().Be(SymbolStyle.Rounded);
        icon.Weight.Should().Be(500);
        icon.Grade.Should().Be(100);
        icon.IsFilled.Should().BeTrue();
        icon.OpticalSize.Should().Be(40);

        var local = new IconPresenter
        {
            Value = MaterialSymbol.Check,
            SymbolStyle = SymbolStyle.Sharp,
            Weight = 600,
            Grade = -20,
            IsFilled = false,
            OpticalSize = 48
        };
        Show(local);

        local.SymbolStyle.Should().Be(SymbolStyle.Sharp);
        local.Weight.Should().Be(600);
        local.Grade.Should().Be(-20);
        local.IsFilled.Should().BeFalse();
        local.OpticalSize.Should().Be(48);
    }

    [Theory]
    [InlineData(nameof(IconPresenter.Weight), 99)]
    [InlineData(nameof(IconPresenter.Weight), 701)]
    [InlineData(nameof(IconPresenter.Grade), -51)]
    [InlineData(nameof(IconPresenter.Grade), 201)]
    [InlineData(nameof(IconPresenter.OpticalSize), 19)]
    [InlineData(nameof(IconPresenter.OpticalSize), 49)]
    public void SymbolAxesRejectValuesOutsideTheSupportedFontRanges(string property, double value)
    {
        var icon = new IconPresenter();

        Action action = property switch
        {
            nameof(IconPresenter.Weight) => () => icon.Weight = value,
            nameof(IconPresenter.Grade) => () => icon.Grade = value,
            nameof(IconPresenter.OpticalSize) => () => icon.OpticalSize = value,
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidSourceReportsErrorAndRecovers()
    {
        var icon = new IconPresenter { Value = new Uri("avares://Material3.Avalonia/missing.png") };
        icon.LoadError.Should().NotBeNull();
        icon.Value = MaterialSymbol.Check;
        icon.LoadError.Should().BeNull();
    }

    [Fact]
    public void UnauthorizedFileSourceReportsLoadError()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"material-icon-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var icon = new IconPresenter { Value = directory };
            icon.LoadError.Should().BeOfType<UnauthorizedAccessException>();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ChangingSizeAndForegroundDoesNotReloadTheImageFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"material-icon-{Guid.NewGuid():N}.svg");
        try
        {
            File.WriteAllText(path,
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path fill=\"red\" d=\"M2 2H22V22H2Z\"/></svg>");
            var icon = new IconPresenter { Value = path };
            Show(icon);
            icon.LoadError.Should().BeNull();
            File.Delete(path);
            icon.Size = 48;
            icon.Foreground = Brushes.Blue;
            Dispatcher.UIThread.RunJobs();
            icon.LoadError.Should().BeNull();
            icon.Value.Should().Be(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GeometrySourceCanChangeAtRuntimeWithoutChangingLayout()
    {
        var source = new GeometryIconSource
            { Data = Geometry.Parse("M-20 -20H20V20H-20Z"), ViewBox = new Rect(-24, -24, 48, 48) };
        var icon = new IconPresenter { Value = source };
        Show(icon);
        var bounds = icon.Bounds;
        source.ViewBox = new Rect(-48, -48, 96, 96);
        source.Data = Geometry.Parse("M0 0H10V10H0Z");
        Dispatcher.UIThread.RunJobs();
        icon.Bounds.Should().Be(bounds);
        icon.LoadError.Should().BeNull();
    }

    [Fact]
    public void SourceChangeShrinksAndReplacesWithLatestRequestWithoutChangingLayout()
    {
        var previousReducedMotion = MotionSettings.ReduceMotion;
        try
        {
            MotionSettings.ReduceMotion = false;
            var first = new Border { Width = 20, Height = 20 };
            var skipped = new Border { Width = 20, Height = 20 };
            var last = new Border { Width = 20, Height = 20 };
            var icon = new IconPresenter { Value = first, IconChangeAnimation = IconChangeAnimation.Scale };
            Show(icon);
            var bounds = icon.Bounds;
            Scale(first).Should().Be(1);
            icon.Value = skipped;
            PumpUntil(() => Scale(first) is > 0 and < .9);
            var intermediateScale = Scale(first);
            icon.Value = last;
            Scale(first).Should().Be(intermediateScale);
            PumpUntil(() => last.Parent is not null && Scale(last) is > 0 and < 1);
            skipped.Parent.Should().BeNull();
            icon.Bounds.Should().Be(bounds);
            PumpUntil(() => Math.Abs(Scale(last) - 1) < .001);
            MotionSettings.ReduceMotion = true;
            icon.Value = first;
            first.Parent.Should().NotBeNull();
            Scale(first).Should().Be(1);
            last.Parent.Should().BeNull();
            icon.Bounds.Should().Be(bounds);
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReducedMotion;
        }
    }

    private static double Scale(Control child) =>
        child.GetVisualAncestors().OfType<Viewbox>().FirstOrDefault()?.RenderTransform is ScaleTransform scale
            ? scale.ScaleX
            : double.NaN;

    [Fact]
    public void AxesInterpolateAndCanBeInterruptedWithoutChangingLayout()
    {
        var previousReducedMotion = MotionSettings.ReduceMotion;
        try
        {
            MotionSettings.ReduceMotion = false;
            var icon = new IconPresenter
            {
                Value = MaterialSymbol.ContentCopy, Weight = 400, OpticalSize = 20,
                Transitions = new Transitions
                {
                    new SpringNullableDoubleTransition
                    {
                        Property = IconPresenter.WeightProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast
                    },
                    new SpringNullableDoubleTransition
                    {
                        Property = IconPresenter.OpticalSizeProperty, Style = MotionStyle.Effects,
                        Speed = MotionSpeed.Fast
                    }
                }
            };
            Show(icon);
            var bounds = icon.Bounds;
            icon.Weight = 700;
            icon.OpticalSize = 48;
            PumpUntil(() => icon.Weight is > 410 and < 680 && icon.OpticalSize is > 20 and < 48);
            icon.Weight = 500;
            PumpUntil(() => Math.Abs(icon.Weight!.Value - 500) < .001 && icon.OpticalSize == 48);
            icon.Bounds.Should().Be(bounds);
            icon.OpticalSize = null;
            PumpUntil(() => icon.OpticalSize is null);
            icon.LoadError.Should().BeNull();
        }
        finally
        {
            MotionSettings.ReduceMotion = previousReducedMotion;
        }
    }

    [Fact]
    public void FilledStateAnimatesAndReversesFromTheCurrentContour()
    {
        var previous = MotionSettings.ReduceMotion;
        try
        {
            MotionSettings.ReduceMotion = false;
            var icon = new IconPresenter { Value = MaterialSymbol.ContentCopy, Foreground = Brushes.Black };
            Show(icon);
            var bounds = icon.Bounds;
            FillProgress(icon).Should().Be(0);
            icon.IsFilled = true;
            PumpUntil(() => FillProgress(icon) is > .05 and < .9);
            var intermediate = FillProgress(icon);
            icon.IsFilled = false;
            FillProgress(icon).Should().Be(intermediate);
            PumpUntil(() => FillProgress(icon) == 0);
            icon.Bounds.Should().Be(bounds);
            MotionSettings.ReduceMotion = true;
            icon.IsFilled = true;
            FillProgress(icon).Should().Be(1);
        }
        finally
        {
            MotionSettings.ReduceMotion = previous;
        }
    }

    private static double FillProgress(IconPresenter icon) => (double)typeof(IconPresenter)
        .GetField("_fill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        .GetValue(icon)!;

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

    private static void Render(IconPresenter icon, double size)
    {
        icon.Measure(new Size(size, size));
        icon.Arrange(new Rect(0, 0, size, size));
        using var context = new DrawingGroup().Open();
        icon.Render(context);
    }

    private void Show(Control content)
    {
        var window = new Window { Width = 500, Height = 300, Content = content };
        _windows.Add(window);
        window.Show();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        foreach (var window in _windows) window.Close();
        Application.Current!.Styles.Remove(_theme);
    }

    private sealed class RecordingImage : IImage, IDisposable
    {
        public Size Size => new(24, 12);
        public Rect Destination { get; private set; }
        public bool Disposed { get; private set; }
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) => Destination = destRect;
        public void Dispose() => Disposed = true;
    }
}