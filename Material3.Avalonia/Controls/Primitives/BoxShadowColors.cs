using Avalonia.Media;

namespace Material3.Avalonia.Controls.Primitives;

internal static class BoxShadowColors
{
    public static BoxShadows WithColor(BoxShadows source, Color color, double opacity = 1)
    {
        if (source.Count == 0)
            return source;
        var shadows = new BoxShadow[source.Count];
        for (var i = 0; i < shadows.Length; i++)
        {
            var shadow = source[i];
            shadows[i] = shadow with
            {
                Color = Color.FromArgb((byte)(shadow.Color.A * color.A / 255 * opacity),
                    color.R, color.G, color.B)
            };
        }

        return new BoxShadows(shadows[0], shadows[1..]);
    }
}