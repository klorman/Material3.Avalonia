using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Draws a Material state layer.
/// </summary>
public class StateLayer : Control
{
    /// <summary>
    /// Defines the state-layer brush.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<StateLayer, IBrush?>(nameof(Brush));

    /// <summary>
    /// Defines the state-layer corner radius.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<StateLayer, CornerRadius>(nameof(CornerRadius));

    /// <summary>
    /// Gets or sets the state-layer brush.
    /// </summary>
    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the state-layer corner radius.
    /// </summary>
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static StateLayer()
    {
        AffectsRender<StateLayer>(BrushProperty, CornerRadiusProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateLayer" /> class.
    /// </summary>
    public StateLayer()
    {
        IsHitTestVisible = false;
        Transitions =
        [
            new SpringDoubleTransition
            {
                Property = OpacityProperty,
                Style = MotionStyle.Effects,
                Speed = MotionSpeed.Fast
            }
        ];
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Brush is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);

        if (CornerRadius == default)
        {
            context.FillRectangle(Brush, rect);
        }
        else
        {
            var roundedRect = new RoundedRect(rect, CornerRadius);
            context.DrawRectangle(Brush, null, roundedRect);
        }
    }
}
