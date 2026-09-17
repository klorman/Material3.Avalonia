using Avalonia;
using Avalonia.Media;

namespace Material3.Avalonia.Symbols.Internal;

/// <summary>A symbol outline together with its original design field.</summary>
internal sealed record SymbolDrawing(Geometry Geometry, Rect ViewBox);

/// <summary>Resolves variable Material Symbols without relying on platform text layout.</summary>
internal static class SymbolRenderer
{
    private const int CacheCapacity = 256;
    private static readonly object Sync = new();
    private static readonly Dictionary<SymbolStyle, SymbolFont> Fonts = new();
    private static readonly Dictionary<OutlineKey, SymbolDrawing> Cache = new();
    private static readonly Queue<OutlineKey> CacheOrder = new();

    /// <summary>Gets an outline at the specified continuous axis coordinates.</summary>
    public static SymbolDrawing GetDrawing(MaterialSymbol symbol, SymbolStyle style = SymbolStyle.Outlined,
        double weight = 400, double grade = 0, double fill = 0, double opticalSize = 24)
    {
        if (!Enum.IsDefined(style))
            throw new ArgumentOutOfRangeException(nameof(style));
        var key = new OutlineKey(symbol, style, Coordinate(weight, 100, 700), Coordinate(grade, -50, 200),
            Coordinate(fill, 0, 1), Coordinate(opticalSize, 20, 48));
        lock (Sync)
        {
            if (Cache.TryGetValue(key, out var cached))
                return cached;
            var font = GetFont(style);
            if (!font.Contains(symbol))
                throw new InvalidOperationException(
                    $"Material Symbol '{symbol}' in style '{style}' is not included in the application symbol set. " +
                    $"Declare '{symbol}' in AXAML with the {style} family, or add it to MaterialSymbolsInclude.");

            var result = font.Draw(symbol, key.Weight, key.Grade, key.Fill, key.OpticalSize);
            if (Cache.Count == CacheCapacity)
                Cache.Remove(CacheOrder.Dequeue());
            Cache.Add(key, result);
            CacheOrder.Enqueue(key);
            return result;
        }
    }

    private static float Coordinate(double value, double min, double max) => double.IsFinite(value)
        ? (float)Math.Clamp(value, min, max)
        : throw new ArgumentOutOfRangeException(nameof(value), "Symbol coordinates must be finite.");

    private static SymbolFont GetFont(SymbolStyle style)
    {
        if (Fonts.TryGetValue(style, out var font))
            return font;
        using var stream = OpenFont(style) ?? throw new InvalidOperationException(
            $"The {style} Material Symbols font is not included in the application. " +
            $"Declare a symbol in AXAML with the {style} family, or add it to MaterialSymbolsInclude.");
        font = new SymbolFont(stream);
        Fonts.Add(style, font);
        return font;
    }

    private static Stream? OpenFont(SymbolStyle style)
    {
        var resourceName = $"Material3.Avalonia.Symbols.{style}.ttf";
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null) return stream;
        }

        return null;
    }

    private readonly record struct OutlineKey(
        MaterialSymbol Symbol,
        SymbolStyle Style,
        float Weight,
        float Grade,
        float Fill,
        float OpticalSize);
}
