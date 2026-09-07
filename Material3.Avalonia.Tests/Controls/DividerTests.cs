using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Controls;

public sealed class DividerTests
{
    [Theory]
    [InlineData("full-width", 0, 0)]
    [InlineData("inset", 16, 0)]
    [InlineData("middle-inset", 16, 16)]
    public void Variants_ShouldInsetTheLineAndSwitchOrientation(string variant, double start, double end)
    {
        using var scene = new DividerScene(variant);
        var separator = scene.Separator;

        scene.LineRect.Left.Should().Be(start);
        scene.LineRect.Width.Should().Be(scene.Host.Bounds.Width - start - end);
        scene.LineRect.Height.Should().Be(1);

        DividerAssist.SetOrientation(separator, Orientation.Vertical);
        scene.Layout();

        scene.LineRect.Top.Should().Be(start);
        scene.LineRect.Height.Should().Be(scene.Host.Bounds.Height - start - end);
        scene.LineRect.Width.Should().Be(1);

        DividerAssist.SetOrientation(separator, Orientation.Horizontal);
        separator.Classes.Clear();
        separator.Classes.Add("full-width");
        scene.Layout();

        scene.LineRect.Width.Should().Be(scene.Host.Bounds.Width);
        scene.LineRect.Height.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LogicalInsets_ShouldMirrorOnceInRtl(bool inherited)
    {
        using var scene = new DividerScene("inset");
        var flowHost = inherited ? (Control)scene.Host : scene.Separator;
        flowHost.FlowDirection = FlowDirection.RightToLeft;
        scene.Layout();

        scene.WindowLineRect.Left.Should().Be(0);
        scene.WindowLineRect.Right.Should().Be(scene.Host.Bounds.Width - 16);

        DividerAssist.SetOrientation(scene.Separator, Orientation.Vertical);
        DividerAssist.SetInsetEnd(scene.Separator, 24);
        scene.Layout();

        scene.WindowLineRect.Top.Should().Be(16);
        scene.WindowLineRect.Bottom.Should().Be(scene.Host.Bounds.Height - 24);

        flowHost.FlowDirection = FlowDirection.LeftToRight;
        DividerAssist.SetOrientation(scene.Separator, Orientation.Horizontal);
        scene.Layout();

        scene.WindowLineRect.Left.Should().Be(16);
        scene.WindowLineRect.Right.Should().Be(scene.Host.Bounds.Width - 24);
    }

    [Fact]
    public void LocalBindingsAndPadding_ShouldOverridePresetsWithoutConsumingMargin()
    {
        using var scene = new DividerScene("middle-inset");
        var source = new Border { Width = 40 };
        using var binding = scene.Separator.Bind(DividerAssist.InsetStartProperty,
            new Binding(nameof(Border.Width)) { Source = source });
        scene.Separator.Margin = new Thickness(8, 0);
        scene.Layout();

        scene.LineRect.Left.Should().Be(48);
        scene.LineRect.Right.Should().Be(scene.Host.Bounds.Width - 24);

        source.Width = 56;
        DividerAssist.SetVariant(scene.Separator, DividerVariant.FullWidth);
        scene.Layout();

        scene.LineRect.Left.Should().Be(64);
        scene.LineRect.Right.Should().Be(scene.Host.Bounds.Width - 8);

        scene.Separator.Padding = new Thickness(4, 2, 12, 3);
        scene.Layout();

        scene.LineRect.Left.Should().Be(12);
        scene.LineRect.Right.Should().Be(scene.Host.Bounds.Width - 20);
        scene.Separator.Bounds.Height.Should().Be(6);
        scene.LineRect.Height.Should().Be(1);
    }

    [Fact]
    public void TokenReplacement_ShouldUpdateExistingLineAndInsets()
    {
        using var scene = new DividerScene("middle-inset");
        var resources = scene.Separator.Resources;
        resources["MdSysOutlineVariantBrush"] = Brushes.Red;
        resources["MdSysSpacing200"] = 20d;
        resources["MdCompDividerThickness"] = 3d;
        scene.Layout();

        scene.Line.Background.Should().BeSameAs(Brushes.Red);
        scene.LineRect.Height.Should().Be(3);
        scene.LineRect.Left.Should().Be(20);

        resources["CustomDividerBrush"] = Brushes.Blue;
        resources["CustomDividerThickness"] = 2d;
        resources["CustomDividerInset"] = 12d;
        resources["MdCompDividerBrush"] = new TokenAlias("CustomDividerBrush");
        resources["MdCompDividerThickness"] = new TokenAlias("CustomDividerThickness");
        resources["MdImplDividerInset"] = new TokenAlias("CustomDividerInset");
        scene.Layout();

        scene.Line.Background.Should().BeSameAs(Brushes.Blue);
        scene.LineRect.Height.Should().Be(2);
        scene.LineRect.Left.Should().Be(12);

        resources["CustomDividerInset"] = 28d;
        resources["CustomDividerThickness"] = 4d;
        DividerAssist.SetOrientation(scene.Separator, Orientation.Vertical);
        scene.Layout();

        scene.LineRect.Width.Should().Be(4);
        scene.LineRect.Top.Should().Be(28);
        scene.LineRect.Bottom.Should().Be(scene.Host.Bounds.Height - 28);
    }

    [Fact]
    public void ThemeChangesAndLocalBackground_ShouldReachTheRenderedLine()
    {
        using var scene = new DividerScene("full-width");
        var theme = new MaterialTheme { Mode = ThemeMode.Light };
        scene.Host.Styles.Add(theme);
        scene.Layout();
        var light = ((ISolidColorBrush)scene.Line.Background!).Color;

        theme.Mode = ThemeMode.Dark;
        scene.Layout();
        ((ISolidColorBrush)scene.Line.Background!).Color.Should().NotBe(light);

        scene.Separator.Background = Brushes.Orange;
        scene.Host.IsEnabled = false;
        theme.Mode = ThemeMode.Light;
        scene.Layout();
        scene.Line.Background.Should().BeSameAs(Brushes.Orange);
        scene.Separator.Focus().Should().BeFalse();
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void OversizedInsets_ShouldCollapseTheLineWithoutNegativeGeometry(Orientation orientation)
    {
        using var scene = new DividerScene("middle-inset");
        DividerAssist.SetOrientation(scene.Separator, orientation);
        DividerAssist.SetInsetStart(scene.Separator, 500);
        DividerAssist.SetInsetEnd(scene.Separator, 500);
        scene.Layout();

        var length = orientation == Orientation.Horizontal ? scene.Line.Bounds.Width : scene.Line.Bounds.Height;
        length.Should().Be(0);
    }

    private sealed class DividerScene : IDisposable
    {
        public Separator Separator { get; } = new();
        public Grid Host { get; } = new();
        private Window Window { get; }

        public DividerScene(string variant)
        {
            TestApp.EnsureStarted();
            Host.Resources.MergedDictionaries.Add((IResourceDictionary)AvaloniaXamlLoader.Load(
                new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml")));
            Separator.Classes.Add(variant);
            Host.Children.Add(Separator);
            Window = new Window
            {
                Width = 240,
                Height = 160,
                WindowDecorations = WindowDecorations.None,
                Content = Host
            };
            Window.Show();
            Layout();
        }

        public Border Line => Separator.GetVisualDescendants().OfType<Border>().Last();
        public Rect LineRect => new Rect(Line.Bounds.Size).TransformToAABB(Line.TransformToVisual(Host)!.Value);
        public Rect WindowLineRect => new Rect(Line.Bounds.Size).TransformToAABB(Line.TransformToVisual(Window)!.Value);

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }

        public void Dispose() => Window.Close();
    }
}