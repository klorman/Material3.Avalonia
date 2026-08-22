using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Draws the filled text-field active indicator.
/// </summary>
public sealed class TextFieldIndicatorLine : Control
{
    /// <summary>
    /// Defines the indicator brush.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<TextFieldIndicatorLine, IBrush?>(nameof(Brush));

    /// <summary>
    /// Defines the indicator thickness.
    /// </summary>
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<TextFieldIndicatorLine, double>(nameof(Thickness), 1d);

    private readonly Pen _pen = new()
    {
        LineCap = PenLineCap.Flat,
        LineJoin = PenLineJoin.Round
    };

    /// <summary>
    /// Gets or sets the indicator brush.
    /// </summary>
    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the indicator thickness.
    /// </summary>
    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    static TextFieldIndicatorLine()
    {
        AffectsRender<TextFieldIndicatorLine>(BrushProperty, ThicknessProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFieldIndicatorLine" /> class.
    /// </summary>
    public TextFieldIndicatorLine()
    {
        IsHitTestVisible = false;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var brush = Brush;
        var thickness = double.IsFinite(Thickness) ? Math.Max(0d, Thickness) : 0d;

        if (brush is null || thickness <= 0d || Bounds.Width <= 0d || Bounds.Height <= 0d)
            return;

        // Keep the complete stroke inside the field bounds.
        var y = Math.Max(0d, Bounds.Height - thickness / 2d);

        _pen.Brush = brush;
        _pen.Thickness = thickness;

        context.DrawLine(
            _pen,
            new Point(0d, y),
            new Point(Bounds.Width, y));
    }
}
