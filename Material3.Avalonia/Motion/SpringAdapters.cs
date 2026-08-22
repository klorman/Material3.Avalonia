using Avalonia;
using Avalonia.Media;

namespace Material3.Avalonia.Motion;

/// <summary>
/// Adapts a value type to spring interpolation components.
/// </summary>
public interface ISpringValueAdapter<T>
{
    /// <summary>
    /// Gets the number of scalar components.
    /// </summary>
    int Components { get; }

    /// <summary>
    /// Reads scalar components from a value.
    /// </summary>
    void Read(T value, Span<double> into);

    /// <summary>
    /// Creates a value from scalar components.
    /// </summary>
    T Make(ReadOnlySpan<double> components);
}

/// <summary>
/// Adapts <see cref="double" /> values for spring interpolation.
/// </summary>
public sealed class DoubleAdapter : ISpringValueAdapter<double>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly DoubleAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 1;

    /// <inheritdoc />
    public void Read(double value, Span<double> into) => into[0] = value;

    /// <inheritdoc />
    public double Make(ReadOnlySpan<double> components) => components[0];
}

/// <summary>
/// Adapts <see cref="Point" /> values for spring interpolation.
/// </summary>
public sealed class PointAdapter : ISpringValueAdapter<Point>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly PointAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 2;

    /// <inheritdoc />
    public void Read(Point value, Span<double> into)
    {
        into[0] = value.X;
        into[1] = value.Y;
    }

    /// <inheritdoc />
    public Point Make(ReadOnlySpan<double> components) => new(components[0], components[1]);
}

/// <summary>
/// Adapts <see cref="Vector" /> values for spring interpolation.
/// </summary>
public sealed class VectorAdapter : ISpringValueAdapter<Vector>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly VectorAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 2;

    /// <inheritdoc />
    public void Read(Vector value, Span<double> into)
    {
        into[0] = value.X;
        into[1] = value.Y;
    }

    /// <inheritdoc />
    public Vector Make(ReadOnlySpan<double> components) => new(components[0], components[1]);
}

/// <summary>
/// Adapts <see cref="Thickness" /> values for spring interpolation.
/// </summary>
public sealed class ThicknessAdapter : ISpringValueAdapter<Thickness>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly ThicknessAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 4;

    /// <inheritdoc />
    public void Read(Thickness value, Span<double> into)
    {
        into[0] = value.Left;
        into[1] = value.Top;
        into[2] = value.Right;
        into[3] = value.Bottom;
    }

    /// <inheritdoc />
    public Thickness Make(ReadOnlySpan<double> components) =>
        new(components[0], components[1], components[2], components[3]);
}

/// <summary>
/// Adapts <see cref="CornerRadius" /> values for spring interpolation.
/// </summary>
public sealed class CornerRadiusAdapter : ISpringValueAdapter<CornerRadius>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly CornerRadiusAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 4;

    /// <inheritdoc />
    public void Read(CornerRadius value, Span<double> into)
    {
        into[0] = value.TopLeft;
        into[1] = value.TopRight;
        into[2] = value.BottomRight;
        into[3] = value.BottomLeft;
    }

    /// <inheritdoc />
    public CornerRadius Make(ReadOnlySpan<double> components) =>
        new(components[0], components[1], components[2], components[3]);
}

/// <summary>
/// Adapts <see cref="Color" /> values for spring interpolation.
/// </summary>
public sealed class ColorAdapter : ISpringValueAdapter<Color>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly ColorAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 4;

    /// <inheritdoc />
    public void Read(Color value, Span<double> into)
    {
        into[0] = value.A;
        into[1] = value.R;
        into[2] = value.G;
        into[3] = value.B;
    }

    /// <inheritdoc />
    public Color Make(ReadOnlySpan<double> components) =>
        Color.FromArgb((byte)components[0], (byte)components[1], (byte)components[2], (byte)components[3]);
}

/// <summary>
/// Adapts solid brushes for spring interpolation.
/// </summary>
public sealed class BrushAdapter : ISpringValueAdapter<IBrush?>
{
    /// <summary>
    /// Gets the shared adapter instance.
    /// </summary>
    public static readonly BrushAdapter Instance = new();

    /// <inheritdoc />
    public int Components => 4;

    /// <inheritdoc />
    public void Read(IBrush? value, Span<double> into)
    {
        var color = value is ISolidColorBrush solid ? solid.Color : Colors.Transparent;
        into[0] = color.A;
        into[1] = color.R;
        into[2] = color.G;
        into[3] = color.B;
    }

    /// <inheritdoc />
    public IBrush? Make(ReadOnlySpan<double> components) =>
        new SolidColorBrush(Color.FromArgb(
            ToByte(components[0]),
            ToByte(components[1]),
            ToByte(components[2]),
            ToByte(components[3])));

    private static byte ToByte(double value) =>
        (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
}
