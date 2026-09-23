using System.Diagnostics;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Material3.Avalonia.Controls.Icons;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Internal;
using Material3.Avalonia.Symbols;
using Material3.Avalonia.Symbols.Internal;

namespace Material3.Avalonia.Controls.Primitives;

/// <summary>Presents a Material Symbol, vector, image, or control in a consistent icon field.</summary>
public class IconPresenter : Control
{
    /// <summary>Identifies the Value property.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<IconPresenter, object?>(nameof(Value), null);

    /// <summary>The icon source.</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Identifies the Size property.</summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<IconPresenter, double>(nameof(Size), 24d,
            validate: value => double.IsFinite(value) && value >= 0);

    /// <summary>The square icon field size in DIP.</summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the Padding property.</summary>
    public static readonly StyledProperty<Thickness?> PaddingProperty =
        AvaloniaProperty.Register<IconPresenter, Thickness?>(nameof(Padding), null);

    /// <summary>Explicit icon padding, or null for source-aware automatic padding.</summary>
    public Thickness? Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>Identifies the SymbolStyle property.</summary>
    public static readonly StyledProperty<SymbolStyle?> SymbolStyleProperty =
        AvaloniaProperty.Register<IconPresenter, SymbolStyle?>(nameof(SymbolStyle));

    /// <summary>The Material Symbols design family.</summary>
    public SymbolStyle? SymbolStyle
    {
        get => GetValue(SymbolStyleProperty);
        set => SetValue(SymbolStyleProperty, value);
    }

    /// <summary>Identifies the Weight property.</summary>
    public static readonly StyledProperty<double?> WeightProperty =
        AvaloniaProperty.Register<IconPresenter, double?>(nameof(Weight),
            validate: value => value is null || double.IsFinite(value.Value) && value is >= 100 and <= 700);

    /// <summary>The continuous symbol weight from 100 to 700, or null to use the application default.</summary>
    public double? Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    /// <summary>Identifies the Grade property.</summary>
    public static readonly StyledProperty<double?> GradeProperty =
        AvaloniaProperty.Register<IconPresenter, double?>(nameof(Grade),
            validate: value => value is null || double.IsFinite(value.Value) && value is >= -50 and <= 200);

    /// <summary>The continuous symbol grade from -50 to 200, or null to use the application default.</summary>
    public double? Grade
    {
        get => GetValue(GradeProperty);
        set => SetValue(GradeProperty, value);
    }

    /// <summary>Identifies the IsFilled property.</summary>
    public static readonly StyledProperty<bool?> IsFilledProperty =
        AvaloniaProperty.Register<IconPresenter, bool?>(nameof(IsFilled));

    /// <summary>Whether the symbol is filled; changes animate using Material effects motion.</summary>
    public bool? IsFilled
    {
        get => GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    /// <summary>Identifies the OpticalSize property.</summary>
    public static readonly StyledProperty<double?> OpticalSizeProperty =
        AvaloniaProperty.Register<IconPresenter, double?>(nameof(OpticalSize), null,
            validate: value => value is null || double.IsFinite(value.Value) && value is >= 20 and <= 48);

    /// <summary>The optical size, or null to follow Size between 20 and 48.</summary>
    public double? OpticalSize
    {
        get => GetValue(OpticalSizeProperty);
        set => SetValue(OpticalSizeProperty, value);
    }

    /// <summary>Identifies the IconChangeAnimation property.</summary>
    public static readonly StyledProperty<IconChangeAnimation> IconChangeAnimationProperty =
        AvaloniaProperty.Register<IconPresenter, IconChangeAnimation>(nameof(IconChangeAnimation),
            IconChangeAnimation.None);

    /// <summary>The source replacement animation.</summary>
    public IconChangeAnimation IconChangeAnimation
    {
        get => GetValue(IconChangeAnimationProperty);
        set => SetValue(IconChangeAnimationProperty, value);
    }

    /// <summary>Identifies the foreground property.</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TemplatedControl.ForegroundProperty.AddOwner<IconPresenter>();

    /// <summary>Gets or sets the brush used for monochrome icon geometry.</summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>Identifies the last source or rendering error.</summary>
    public static readonly DirectProperty<IconPresenter, Exception?> LoadErrorProperty =
        AvaloniaProperty.RegisterDirect<IconPresenter, Exception?>(nameof(LoadError), control => control.LoadError);

    /// <summary>Gets the last source error, or null after a successful load.</summary>
    public Exception? LoadError
    {
        get => _loadError;
        private set => SetAndRaise(LoadErrorProperty, ref _loadError, value);
    }

    private Exception? _loadError;
    private object? _displayedValue;
    private Geometry? _geometry;
    private Rect _viewBox;
    private IImage? _image;
    private IDisposable? _ownedImage;
    private Viewbox? _child;
    private GeometryIconSource? _geometrySource;
    private double _fill;
    private double _fromFill;
    private bool _filling;
    private readonly Stopwatch _fillElapsed = new();
    private double _scale = 1;
    private double _fromScale;
    private bool _shrinking;
    private bool _animating;
    private bool _framePending;
    private int _frameGeneration;
    private readonly ScaleTransform _childScale = new(1, 1);
    private readonly Stopwatch _elapsed = new();

    static IconPresenter()
    {
        AffectsMeasure<IconPresenter>(SizeProperty);
        AffectsArrange<IconPresenter>(PaddingProperty);
        AffectsRender<IconPresenter>(ForegroundProperty, PaddingProperty, SizeProperty);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
            ChangeSource();
        else if (change.Property == IsFilledProperty)
            ChangeFill();
        else if (change.Property == WeightProperty || change.Property == GradeProperty ||
                 change.Property == SymbolStyleProperty || change.Property == OpticalSizeProperty ||
                 change.Property == SizeProperty)
            UpdateSymbol();
        else if (change.Property == IconChangeAnimationProperty && IconChangeAnimation == IconChangeAnimation.None)
            FinishChange();
    }

    private void ChangeFill()
    {
        if (_displayedValue is MaterialSymbol && !MotionSettings.ReduceMotion &&
            TopLevel.GetTopLevel(this) is not null)
        {
            _fromFill = _fill;
            _filling = true;
            _fillElapsed.Restart();
            RequestFrame();
        }
        else FinishFill();
    }

    private void FinishFill()
    {
        _filling = false;
        _fillElapsed.Stop();
        _fill = EffectiveIsFilled ? 1 : 0;
        UpdateSymbol();
    }

    private void AnimateFill()
    {
        if (!_filling) return;
        var spring = MotionSettings.GlobalScheme.Resolve(MotionStyle.Effects, MotionSpeed.Fast);
        var time = _fillElapsed.Elapsed.TotalSeconds;
        if (MotionSettings.ReduceMotion ||
            time >= SpringDuration.ComputeSeconds(spring.Stiffness, spring.Damping, false))
        {
            FinishFill();
            return;
        }

        var progress = SpringAnalytic.Evaluate(time, spring.Stiffness, spring.Damping, 0);
        _fill = Math.Clamp(_fromFill + ((EffectiveIsFilled ? 1 : 0) - _fromFill) * progress, 0, 1);
        UpdateSymbol();
    }

    private void ChangeSource()
    {
        if (Equals(_displayedValue, Value) && !_animating) return;
        if (IconChangeAnimation == IconChangeAnimation.Scale && !MotionSettings.ReduceMotion &&
            TopLevel.GetTopLevel(this) is not null)
        {
            if (_displayedValue is not null && (!_animating || !_shrinking))
                BeginPhase(true);
            else if (_displayedValue is null && Value is not null)
            {
                LoadSource();
                _scale = 0;
                UpdateChildScale();
                BeginPhase(false);
            }
        }
        else FinishChange();
    }

    private void BeginPhase(bool shrinking)
    {
        _shrinking = shrinking;
        _fromScale = _scale;
        _animating = true;
        _elapsed.Restart();
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (_framePending || TopLevel.GetTopLevel(this) is not { } top) return;
        _framePending = true;
        var generation = _frameGeneration;
        top.RequestAnimationFrame(timestamp =>
        {
            if (generation == _frameGeneration) Animate(timestamp);
        });
    }

    private void Animate(TimeSpan timestamp)
    {
        _framePending = false;
        AnimateFill();
        if (!_animating)
        {
            if (_filling) RequestFrame();
            return;
        }

        if (MotionSettings.ReduceMotion)
        {
            FinishChange();
            return;
        }

        var spring = MotionSettings.GlobalScheme.Resolve(MotionStyle.Spatial, MotionSpeed.Fast);
        var duration = SpringDuration.ComputeSeconds(spring.Stiffness, spring.Damping, false);
        var time = _elapsed.Elapsed.TotalSeconds;
        var progress = time >= duration ? 1 : SpringAnalytic.Evaluate(time, spring.Stiffness, spring.Damping, 0);
        _scale = _shrinking
            ? Math.Max(0, _fromScale * (1 - progress))
            : Math.Max(0, _fromScale + (1 - _fromScale) * progress);
        if (_shrinking && (_scale <= 0 || time >= duration))
        {
            _scale = 0;
            LoadSource();
            if (Value is null)
            {
                _animating = false;
                _elapsed.Stop();
            }
            else BeginPhase(false);
        }
        else if (!_shrinking && time >= duration)
        {
            _scale = 1;
            _animating = false;
            _elapsed.Stop();
        }

        UpdateChildScale();
        InvalidateVisual();
        if (_animating || _filling) RequestFrame();
    }

    private void FinishChange()
    {
        _animating = false;
        _elapsed.Stop();
        _scale = 1;
        LoadSource();
        UpdateChildScale();
    }

    private void LoadSource()
    {
        ReleaseSource();
        _displayedValue = Value;
        LoadError = null;
        try
        {
            switch (_displayedValue)
            {
                case null: break;
                case MaterialSymbol:
                    if (!_filling) _fill = EffectiveIsFilled ? 1 : 0;
                    UpdateSymbol();
                    break;
                case GeometryIconSource source:
                    _geometrySource = source;
                    source.PropertyChanged += GeometrySourceChanged;
                    UpdateGeometrySource();
                    break;
                case Geometry geometry:
                    _geometry = geometry;
                    _viewBox = geometry.Bounds;
                    break;
                case IImage image: _image = image; break;
                case Control control:
                    _child = new Viewbox { Child = control, Stretch = Stretch.Uniform, IsHitTestVisible = false };
                    VisualChildren.Add(_child);
                    LogicalChildren.Add(_child);
                    break;
                case Uri uri: LoadImage(uri.OriginalString); break;
                case string text:
                    if (IsPathData(text))
                    {
                        _geometry = Geometry.Parse(text);
                        _viewBox = _geometry.Bounds;
                    }
                    else LoadImage(text);

                    break;
                default:
                    throw new NotSupportedException($"Unsupported icon source type: {_displayedValue.GetType().Name}.");
            }
        }
        catch (Exception error) when (IsExpectedSourceLoadException(error))
        {
            ReportError(error);
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    private static bool IsPathData(string value)
    {
        var text = value.AsSpan().TrimStart();
        return text.Length > 1 && (text[0] is 'M' or 'm' or 'F') &&
               (char.IsWhiteSpace(text[1]) || char.IsDigit(text[1]) || text[1] is '-' or '+' or '.');
    }

    private void LoadImage(string path)
    {
        using var stream = Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "avares"
            ? AssetLoader.Open(uri)
            : File.OpenRead(uri is { IsFile: true } ? uri.LocalPath : path);
        if (Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var svg = new SvgIconImage(stream);
            _image = svg;
            _ownedImage = svg;
        }
        else
        {
            var bitmap = new Bitmap(stream);
            _image = bitmap;
            _ownedImage = bitmap;
        }
    }

    private static bool IsExpectedSourceLoadException(Exception error) =>
        error is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or
            InvalidOperationException or NotSupportedException or System.Xml.XmlException or FormatException;

    private void ReportError(Exception error)
    {
        LoadError = error;
        Logger.TryGet(LogEventLevel.Warning, "MaterialIcons")
            ?.Log(this, "Icon could not be rendered: {Error}", error.Message);
    }

    private void GeometrySourceChanged(object? sender, AvaloniaPropertyChangedEventArgs args) => UpdateGeometrySource();

    private void UpdateGeometrySource()
    {
        _geometry = _geometrySource?.Data;
        _viewBox = _geometrySource?.ViewBox ?? _geometry?.Bounds ?? default;
        InvalidateVisual();
    }

    private void UpdateSymbol()
    {
        if (_displayedValue is not MaterialSymbol symbol) return;
        try
        {
            var drawing = SymbolRenderer.GetDrawing(symbol, EffectiveSymbolStyle, EffectiveWeight, EffectiveGrade,
                _fill, EffectiveOpticalSize);
            _geometry = drawing.Geometry;
            _viewBox = drawing.ViewBox;
            LoadError = null;
        }
        catch (Exception error) when (IsExpectedSourceLoadException(error))
        {
            _geometry = null;
            ReportError(error);
        }

        InvalidateVisual();
    }

    internal bool EffectiveIsFilled => IsFilled ?? false;

    internal SymbolStyle EffectiveSymbolStyle => SymbolStyle ?? Material3.Avalonia.Symbols.SymbolStyle.Outlined;
    internal double EffectiveWeight => Weight ?? 400;
    internal double EffectiveGrade => Grade ?? 0;

    internal double EffectiveOpticalSize => OpticalSize ?? Math.Clamp(Size, 20, 48);

    private void ReleaseSource()
    {
        _ownedImage?.Dispose();
        _ownedImage = null;
        _image = null;
        _geometry = null;
        if (_geometrySource is not null) _geometrySource.PropertyChanged -= GeometrySourceChanged;
        _geometrySource = null;
        if (_child is not null)
        {
            _child.Child = null;
            VisualChildren.Remove(_child);
            LogicalChildren.Remove(_child);
            _child = null;
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animating = false;
        _framePending = false;
        _frameGeneration++;
        _elapsed.Stop();
        FinishFill();
        _scale = 1;
        ReleaseSource();
        _displayedValue = null;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!Equals(_displayedValue, Value)) FinishChange();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        _child?.Measure(new Size(Size, Size));
        return new Size(Size, Size);
    }

    private Rect ContentRect(Size bounds)
    {
        var size = Math.Min(Size, Math.Min(bounds.Width, bounds.Height));
        var padding = Padding ?? (_displayedValue is MaterialSymbol ? default : new Thickness(Size / 12));
        var ratio = Size > 0 ? size / Size : 0;
        return new Rect((bounds.Width - size) / 2 + padding.Left * ratio,
            (bounds.Height - size) / 2 + padding.Top * ratio,
            Math.Max(0, size - (padding.Left + padding.Right) * ratio),
            Math.Max(0, size - (padding.Top + padding.Bottom) * ratio));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _child?.Arrange(ContentRect(finalSize));
        UpdateChildScale();
        return finalSize;
    }

    private void UpdateChildScale()
    {
        if (_child is null) return;
        _child.RenderTransformOrigin = new RelativePoint(Bounds.Width / 2 - _child.Bounds.X,
            Bounds.Height / 2 - _child.Bounds.Y, RelativeUnit.Absolute);
        _childScale.ScaleX = _scale;
        _childScale.ScaleY = _scale;
        _child.RenderTransform = _childScale;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var destination = ContentRect(Bounds.Size);
        if (destination.Width <= 0 || destination.Height <= 0 || _scale <= 0) return;
        var center = new Vector(Bounds.Width / 2, Bounds.Height / 2);
        using var animation = context.PushTransform(Matrix.CreateTranslation(-center) *
                                                    Matrix.CreateScale(_scale, _scale) *
                                                    Matrix.CreateTranslation(center));
        if (_geometry is not null && _viewBox.Width > 0 && _viewBox.Height > 0)
        {
            var ratio = Math.Min(destination.Width / _viewBox.Width, destination.Height / _viewBox.Height);
            var transform = Matrix.CreateTranslation(-_viewBox.X, -_viewBox.Y) * Matrix.CreateScale(ratio, ratio) *
                            Matrix.CreateTranslation(destination.X + (destination.Width - _viewBox.Width * ratio) / 2,
                                destination.Y + (destination.Height - _viewBox.Height * ratio) / 2);
            using (context.PushTransform(transform)) context.DrawGeometry(Foreground, null, _geometry);
        }
        else if (_image is { Size.Width: > 0, Size.Height: > 0 })
        {
            var ratio = Math.Min(destination.Width / _image.Size.Width, destination.Height / _image.Size.Height);
            var size = _image.Size * ratio;
            var target = new Rect(destination.X + (destination.Width - size.Width) / 2,
                destination.Y + (destination.Height - size.Height) / 2, size.Width, size.Height);
            context.DrawImage(_image, new Rect(_image.Size), target);
        }
    }
}