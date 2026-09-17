using Avalonia;
using Avalonia.Headless;
using FluentAssertions;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Symbols.Internal;
using Xunit;

namespace Material3.Avalonia.Rendering.Tests;

public sealed class SymbolRenderingTests
{
    private static bool _started;

    [Fact]
    public void FilledCopyPreservesItsSolidInteriorAtHighGradeAndOpticalSize()
    {
        EnsureStarted();
        foreach (var style in Enum.GetValues<SymbolStyle>())
        {
            var drawing = SymbolRenderer.GetDrawing(MaterialSymbol.ContentCopy, style,
                grade: 200, fill: 1, opticalSize: 48);
            // The nominal cmap glyph leaves a gap without conditional GSUB substitution.
            for (var x = 360; x < 730; x += 10)
            for (var y = 200; y < 780; y += 10)
                drawing.Geometry.FillContains(new Point(x, -y)).Should().BeTrue($"the sheet is filled at {x}, {y}");
        }
    }

    [Fact]
    public void DrawingCacheReusesTheSameGeometry()
    {
        EnsureStarted();
        var first = SymbolRenderer.GetDrawing(MaterialSymbol.Check);
        var second = SymbolRenderer.GetDrawing(MaterialSymbol.Check);

        second.Should().BeSameAs(first);
        second.Geometry.Should().BeSameAs(first.Geometry);
    }

    [Theory]
    [InlineData(12, 20)]
    [InlineData(24, 24)]
    [InlineData(64, 48)]
    public void DefaultOpticalSizeFollowsIconSize(double size, double expected)
    {
        EnsureStarted();
        var icon = new IconPresenter { Size = size };

        icon.EffectiveOpticalSize.Should().Be(expected);
    }

    [Fact]
    public void MissingGlyphDiagnosticExplainsHowToImportTheFamily()
    {
        EnsureStarted();
        var action = () => SymbolRenderer.GetDrawing(MaterialSymbol.Home, SymbolStyle.Rounded);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Home*Rounded*Declare*MaterialSymbolsInclude*");
    }

    private static void EnsureStarted()
    {
        if (_started) return;
        AppBuilder.Configure<Application>().UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();
        _started = true;
    }
}