using Avalonia;
using Avalonia.Media;
using Avalonia.Svg;

namespace Material3.Avalonia.Controls.Icons;

internal sealed class SvgIconImage : IImage, IDisposable
{
    private readonly AvaloniaPicture _picture;
    private readonly Rect _bounds;

    public SvgIconImage(Stream stream)
    {
        var picture = SvgSource.LoadPicture(stream) ??
                      throw new InvalidDataException("The SVG has no renderable picture.");
        _bounds = new Rect(picture.CullRect.Left, picture.CullRect.Top, picture.CullRect.Width,
            picture.CullRect.Height);
        _picture = AvaloniaPicture.Record(picture);
    }

    public Size Size => _bounds.Size;

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0) return;
        var transform = Matrix.CreateTranslation(-_bounds.X - sourceRect.X, -_bounds.Y - sourceRect.Y)
                        * Matrix.CreateScale(destRect.Width / sourceRect.Width, destRect.Height / sourceRect.Height)
                        * Matrix.CreateTranslation(destRect.X, destRect.Y);
        using (context.PushClip(destRect))
        using (context.PushTransform(transform))
            _picture.Draw(context);
    }

    public void Dispose() => _picture.Dispose();
}