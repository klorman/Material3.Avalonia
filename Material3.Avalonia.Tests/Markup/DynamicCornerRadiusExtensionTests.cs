using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Material3.Avalonia.Markup;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Markup;

public sealed class DynamicCornerRadiusExtensionTests
{
    static DynamicCornerRadiusExtensionTests()
    {
        TestApp.EnsureStarted();
    }

    [Theory]
    [InlineData("MdSysShapeCornerFull")]
    [InlineData("MdCompBadgeShape")]
    public void FullTokenTracksTargetBounds(string resourceKey)
    {
        var border = CreateBorder(resourceKey);
        var window = Show(border);
        try
        {
            border.CornerRadius.Should().Be(new CornerRadius(20));

            border.Width = 100;
            border.Height = 60;
            Layout(window);

            border.CornerRadius.Should().Be(new CornerRadius(30));
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void UserAliasSupportsConcreteAndRuntimeReplacement()
    {
        var border = CreateBorder("UserShape",
            control => control.Resources["UserShape"] = new TokenAlias("MdSysShapeCornerFull"));
        var window = Show(border);
        try
        {
            border.CornerRadius.Should().Be(new CornerRadius(20));

            border.Resources["UserShape"] = new CornerRadius(7);
            Dispatcher.UIThread.RunJobs();
            border.CornerRadius.Should().Be(new CornerRadius(7));

            border.Resources["CustomShape"] = new CornerRadius(11);
            border.Resources["UserShape"] = new TokenAlias("CustomShape");
            Dispatcher.UIThread.RunJobs();
            border.CornerRadius.Should().Be(new CornerRadius(11));
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void FullTokenWorksInsideControlTemplate()
    {
        var button = AvaloniaXamlLoader.Load(new Uri(
                "avares://Material3.Avalonia.Tests/Markup/DynamicCornerRadiusExtensionTemplateSmoke.axaml"))
            .Should().BeOfType<Button>()
            .Subject;
        var window = new Window { Width = 200, Height = 100, Content = button };
        try
        {
            window.Show();
            button.ApplyStyling();
            button.ApplyTemplate();
            Layout(window);

            var border = button.GetVisualDescendants().OfType<Border>()
                .Should().ContainSingle(x => x.Name == "PART_DynamicCornerRadius").Subject;
            border.CornerRadius.Should().Be(new CornerRadius(20));

            border.Width = 100;
            border.Height = 60;
            Layout(window);
            border.CornerRadius.Should().Be(new CornerRadius(30));
        }
        finally
        {
            window.Close();
        }
    }

    private static Border CreateBorder(object resourceKey, Action<Border>? configure = null)
    {
        var border = new Border
        {
            Width = 80,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        border.Resources.MergedDictionaries.Add(LoadResources());
        configure?.Invoke(border);
        border.Bind(Border.CornerRadiusProperty,
            (MultiBinding)new DynamicCornerRadiusExtension(resourceKey).ProvideValue(null!));
        return border;
    }

    private static Window Show(Control content)
    {
        var window = new Window { Width = 200, Height = 100, Content = content };
        window.Show();
        Layout(window);
        return window;
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static IResourceDictionary LoadResources() =>
        (IResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml"));
}