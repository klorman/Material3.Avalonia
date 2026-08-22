using Avalonia;
using Avalonia.Media;

namespace Material3.Avalonia.Motion.Transitions;

/// <summary>
/// Spring transition for <see cref="double" /> values.
/// </summary>
public sealed class SpringDoubleTransition : SpringTransitionBase<double, DoubleAdapter> { }

/// <summary>
/// Spring transition for <see cref="Point" /> values.
/// </summary>
public sealed class SpringPointTransition : SpringTransitionBase<Point, PointAdapter> { }

/// <summary>
/// Spring transition for <see cref="Vector" /> values.
/// </summary>
public sealed class SpringVectorTransition : SpringTransitionBase<Vector, VectorAdapter> { }

/// <summary>
/// Spring transition for <see cref="Thickness" /> values.
/// </summary>
public sealed class SpringThicknessTransition : SpringTransitionBase<Thickness, ThicknessAdapter> { }

/// <summary>
/// Spring transition for <see cref="CornerRadius" /> values.
/// </summary>
public sealed class SpringCornerRadiusTransition : SpringTransitionBase<CornerRadius, CornerRadiusAdapter> { }

/// <summary>
/// Spring transition for solid brushes.
/// </summary>
public sealed class SpringBrushTransition : SpringTransitionBase<IBrush?, BrushAdapter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBrushTransition" /> class.
    /// </summary>
    public SpringBrushTransition()
    {
        Style = MotionStyle.Effects;
    }
}

/// <summary>
/// Spring transition for <see cref="Color" /> values.
/// </summary>
public sealed class SpringColorTransition : SpringTransitionBase<Color, ColorAdapter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpringColorTransition" /> class.
    /// </summary>
    public SpringColorTransition()
    {
        Style = MotionStyle.Effects;
    }
}
