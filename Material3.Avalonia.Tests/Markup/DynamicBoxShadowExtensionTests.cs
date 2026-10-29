using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAssertions;
using Material3.Avalonia.Markup;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tests.Markup;

public sealed class DynamicBoxShadowExtensionTests
{
    static DynamicBoxShadowExtensionTests() => TestApp.EnsureStarted();

    [Fact]
    public void AliasesAndThemeChangesRecolorAllLayersWithoutChangingGeometry()
    {
        var border = new Border { Width = 56, Height = 56 };
        var geometry = BoxShadows.Parse("0 2 3 1 #80000000, 0 4 8 2 #40000000, 1 1 2 0 #20000000");
        border.Resources["Geometry"] = new TokenAlias("GeometryLeaf");
        border.Resources["GeometryLeaf"] = geometry;
        border.Resources["Tint"] = new TokenAlias("TintLeaf");
        border.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary
            { ["TintLeaf"] = Color.FromArgb(128, 255, 0, 0) };
        border.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
            { ["TintLeaf"] = Colors.Blue };
        using var binding = border.Bind(Border.BoxShadowProperty,
            (MultiBinding)new DynamicBoxShadowExtension(new DynamicResourceExtension("Geometry"), "Tint")
                .ProvideValue(null!));
        var theme = new MaterialTheme { MotionScheme = null };
        var window = new Window
            { Width = 200, Height = 150, Content = border, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(theme);
        try
        {
            window.Show();
            Layout(window);
            Check(Color.FromArgb(128, 255, 0, 0));
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Layout(window);
            Check(Colors.Blue);
            border.Resources["OtherTint"] = Colors.Green;
            border.Resources["Tint"] = new TokenAlias("OtherTint");
            border.Resources["GeometryLeaf"] = geometry = BoxShadows.Parse("0 6 12 3 #60000000");
            Layout(window);
            Check(Colors.Green);
        }
        finally
        {
            window.Close();
        }

        void Check(Color color)
        {
            border.BoxShadow.Count.Should().Be(geometry.Count);
            for (var i = 0; i < geometry.Count; i++)
            {
                var expected = geometry[i] with
                {
                    Color = Color.FromArgb((byte)(geometry[i].Color.A * color.A / 255), color.R, color.G, color.B)
                };
                border.BoxShadow[i].Should().Be(expected);
            }
        }
    }

    [Fact]
    public void ExplicitBindingsKeepTheirSourceAndTrackChanges()
    {
        var source = new Border { BoxShadow = BoxShadows.Parse("0 2 3 1 #80000000"), Tag = Colors.Red };
        var border = new Border();
        using var binding = border.Bind(Border.BoxShadowProperty,
            (MultiBinding)new DynamicBoxShadowExtension(
                    new Binding(nameof(Border.BoxShadow)) { Source = source },
                    new Binding(nameof(Border.Tag)) { Source = source })
                .ProvideValue(null!));
        border.BoxShadow[0].Color.Should().Be(Color.FromArgb(128, 255, 0, 0));
        source.Tag = Colors.Blue;
        Dispatcher.UIThread.RunJobs();
        border.BoxShadow[0].Color.Should().Be(Color.FromArgb(128, 0, 0, 255));
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}