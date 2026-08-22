using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>
/// Applies the Material text-field label transition.
/// </summary>
public sealed class TextFieldLabelMotion : Control
{
    /// <summary>
    /// Defines the expanded label endpoint.
    /// </summary>
    public static readonly StyledProperty<Control?> ExpandedLabelProperty =
        AvaloniaProperty.Register<TextFieldLabelMotion, Control?>(nameof(ExpandedLabel));

    /// <summary>
    /// Defines the filled minimized label endpoint.
    /// </summary>
    public static readonly StyledProperty<Control?> FilledMinimizedLabelProperty =
        AvaloniaProperty.Register<TextFieldLabelMotion, Control?>(nameof(FilledMinimizedLabel));

    /// <summary>
    /// Defines the outlined minimized label endpoint.
    /// </summary>
    public static readonly StyledProperty<Control?> OutlinedMinimizedLabelProperty =
        AvaloniaProperty.Register<TextFieldLabelMotion, Control?>(nameof(OutlinedMinimizedLabel));

    /// <summary>
    /// Defines whether the outlined minimized endpoint is active.
    /// </summary>
    public static readonly StyledProperty<bool> IsOutlinedProperty =
        AvaloniaProperty.Register<TextFieldLabelMotion, bool>(nameof(IsOutlined));

    /// <summary>
    /// Defines the label transition progress.
    /// </summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<TextFieldLabelMotion, double>(nameof(Progress));

    private Control? _activeMinimizedLabel;
    private ScaleTransform? _scaleTransform;
    private TranslateTransform? _translateTransform;

    private double _restScale = 1d;
    private double _restTranslateX;
    private double _restTranslateY;

    /// <summary>
    /// Gets or sets the expanded label endpoint.
    /// </summary>
    public Control? ExpandedLabel
    {
        get => GetValue(ExpandedLabelProperty);
        set => SetValue(ExpandedLabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the filled minimized label endpoint.
    /// </summary>
    public Control? FilledMinimizedLabel
    {
        get => GetValue(FilledMinimizedLabelProperty);
        set => SetValue(FilledMinimizedLabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the outlined minimized label endpoint.
    /// </summary>
    public Control? OutlinedMinimizedLabel
    {
        get => GetValue(OutlinedMinimizedLabelProperty);
        set => SetValue(OutlinedMinimizedLabelProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the outlined minimized endpoint is active.
    /// </summary>
    public bool IsOutlined
    {
        get => GetValue(IsOutlinedProperty);
        set => SetValue(IsOutlinedProperty, value);
    }

    /// <summary>
    /// Gets or sets the label transition progress.
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    static TextFieldLabelMotion()
    {
        ProgressProperty.Changed.AddClassHandler<TextFieldLabelMotion>(
            static (control, _) => control.ApplyProgress());

        IsOutlinedProperty.Changed.AddClassHandler<TextFieldLabelMotion>(
            static (control, _) => control.RefreshGeometry());

        ExpandedLabelProperty.Changed.AddClassHandler<TextFieldLabelMotion>(
            static (control, _) => control.RefreshGeometry());

        FilledMinimizedLabelProperty.Changed.AddClassHandler<TextFieldLabelMotion>(
            static (control, _) => control.RefreshGeometry());

        OutlinedMinimizedLabelProperty.Changed.AddClassHandler<TextFieldLabelMotion>(
            static (control, _) => control.RefreshGeometry());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFieldLabelMotion" /> class.
    /// </summary>
    public TextFieldLabelMotion()
    {
        IsHitTestVisible = false;
        LayoutUpdated += (_, _) => RefreshGeometry();
    }

    private void RefreshGeometry()
    {
        var expanded = ExpandedLabel;
        var minimized = IsOutlined ? OutlinedMinimizedLabel : FilledMinimizedLabel;

        EnsureActiveMinimizedLabel(minimized);

        if (expanded is null || minimized is null || _scaleTransform is null || _translateTransform is null)
        {
            ApplyProgress();
            return;
        }

        // Measure endpoints without feeding our current render transform back into TranslatePoint.
        _scaleTransform.ScaleX = 1d;
        _scaleTransform.ScaleY = 1d;
        _translateTransform.X = 0d;
        _translateTransform.Y = 0d;

        var expandedOrigin = expanded.TranslatePoint(default, this);
        var minimizedOrigin = minimized.TranslatePoint(default, this);

        if (expandedOrigin is null || minimizedOrigin is null)
        {
            ApplyProgress();
            return;
        }

        var expandedWidth = GetTextWidth(expanded);
        var minimizedWidth = GetTextWidth(minimized);
        var expandedHeight = GetTextHeight(expanded);
        var minimizedHeight = GetTextHeight(minimized);

        if (expandedWidth <= 0d || minimizedWidth <= 0d || expandedHeight <= 0d || minimizedHeight <= 0d)
        {
            ApplyProgress();
            return;
        }

        // Material Web deliberately derives scale from measured text width rather than font-size
        // ratio so tracking/letter-spacing is compensated during the transform.
        _restScale = expandedWidth / minimizedWidth;
        _restTranslateX = expandedOrigin.Value.X - minimizedOrigin.Value.X;
        _restTranslateY =
            expandedOrigin.Value.Y - minimizedOrigin.Value.Y +
            Math.Round((expandedHeight - minimizedHeight * _restScale) / 2d);

        ApplyProgress();
    }

    private void EnsureActiveMinimizedLabel(Control? minimized)
    {
        if (ReferenceEquals(_activeMinimizedLabel, minimized))
            return;

        if (_activeMinimizedLabel is not null)
        {
            _activeMinimizedLabel.RenderTransform = null;
            _activeMinimizedLabel.Opacity = 0d;
        }

        _activeMinimizedLabel = minimized;
        _scaleTransform = null;
        _translateTransform = null;

        if (minimized is null)
            return;

        var scale = new ScaleTransform();
        var translate = new TranslateTransform();
        var transforms = new TransformGroup();

        // Declared order: scale first, then translate. With a top-left origin this gives us
        // the same transform model as Material Web's floating-label transition.
        transforms.Children.Add(scale);
        transforms.Children.Add(translate);

        minimized.RenderTransformOrigin = RelativePoint.TopLeft;
        minimized.RenderTransform = transforms;

        _scaleTransform = scale;
        _translateTransform = translate;
    }

    private void ApplyProgress()
    {
        var expanded = ExpandedLabel;
        var active = _activeMinimizedLabel;

        if (expanded is null || active is null || _scaleTransform is null || _translateTransform is null)
            return;

        // Keep spring overshoot for spatial motion. Do not clamp Progress here.
        var p = double.IsFinite(Progress) ? Progress : 0d;

        _scaleTransform.ScaleX = Lerp(_restScale, 1d, p);
        _scaleTransform.ScaleY = Lerp(_restScale, 1d, p);
        _translateTransform.X = _restTranslateX * (1d - p);
        _translateTransform.Y = _restTranslateY * (1d - p);

        // As in Material Web, render only one label during movement for crisp glyphs.
        // During the reverse spring the minimized label stays active through any overshoot;
        // the resting label is restored only at the exact resting endpoint.
        var atRest = Math.Abs(p) <= 1e-9;
        expanded.Opacity = atRest ? 1d : 0d;
        active.Opacity = atRest ? 0d : 1d;

        var inactive = IsOutlined ? FilledMinimizedLabel : OutlinedMinimizedLabel;
        if (inactive is not null)
            inactive.Opacity = 0d;
    }

    private static double GetTextWidth(Control control)
    {
        if (control is Decorator { Child: Control child })
            return PositiveOrFallback(child.DesiredSize.Width, child.Bounds.Width);

        return PositiveOrFallback(control.DesiredSize.Width, control.Bounds.Width);
    }

    private static double GetTextHeight(Control control)
    {
        if (control is Decorator { Child: Control child })
            return PositiveOrFallback(child.Bounds.Height, child.DesiredSize.Height);

        return PositiveOrFallback(control.Bounds.Height, control.DesiredSize.Height);
    }

    private static double PositiveOrFallback(double preferred, double fallback) =>
        double.IsFinite(preferred) && preferred > 0d ? preferred : Math.Max(0d, fallback);

    private static double Lerp(double from, double to, double progress) =>
        from + (to - from) * progress;
}
