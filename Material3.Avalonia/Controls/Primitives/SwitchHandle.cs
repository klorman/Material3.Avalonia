using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Transitions;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class SwitchHandle : Decorator
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SwitchHandle, bool>(nameof(IsSelected), false);

    public static readonly StyledProperty<bool> IsPressedProperty =
        AvaloniaProperty.Register<SwitchHandle, bool>(nameof(IsPressed), false);

    public static readonly StyledProperty<SwitchIconConfiguration> IconConfigurationProperty =
        AvaloniaProperty.Register<SwitchHandle, SwitchIconConfiguration>(nameof(IconConfiguration),
            SwitchIconConfiguration.None);

    public static readonly StyledProperty<double> UnselectedWidthProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(UnselectedWidth), 0);

    public static readonly StyledProperty<double> UnselectedHeightProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(UnselectedHeight), 0);

    public static readonly StyledProperty<double> SelectedWidthProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(SelectedWidth), 0);

    public static readonly StyledProperty<double> SelectedHeightProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(SelectedHeight), 0);

    public static readonly StyledProperty<double> WithIconWidthProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(WithIconWidth), 0);

    public static readonly StyledProperty<double> WithIconHeightProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(WithIconHeight), 0);

    public static readonly StyledProperty<double> PressedWidthProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(PressedWidth), 0);

    public static readonly StyledProperty<double> PressedHeightProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(PressedHeight), 0);

    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<SwitchHandle, IBrush?>(nameof(Brush), null);

    public static readonly StyledProperty<double> HandleOpacityProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(HandleOpacity), 1);

    public static readonly StyledProperty<CornerRadius> HandleShapeProperty =
        AvaloniaProperty.Register<SwitchHandle, CornerRadius>(nameof(HandleShape), default);

    public static readonly StyledProperty<double> SelectedIconOpacityProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(SelectedIconOpacity), 1);

    public static readonly StyledProperty<double> UnselectedIconOpacityProperty =
        AvaloniaProperty.Register<SwitchHandle, double>(nameof(UnselectedIconOpacity), 1);

    private static readonly StyledProperty<double> HandleWidthProperty =
        AvaloniaProperty.Register<SwitchHandle, double>("HandleWidth");

    private static readonly StyledProperty<double> HandleHeightProperty =
        AvaloniaProperty.Register<SwitchHandle, double>("HandleHeight");

    private static readonly StyledProperty<double> SelectedVisibilityProperty =
        AvaloniaProperty.Register<SwitchHandle, double>("SelectedVisibility");

    private static readonly StyledProperty<double> UnselectedVisibilityProperty =
        AvaloniaProperty.Register<SwitchHandle, double>("UnselectedVisibility");

    private static readonly StyledProperty<double> SelectedAngleProperty =
        AvaloniaProperty.Register<SwitchHandle, double>("SelectedAngle");

    private bool _arranged;
    private IconPresenter? _selected;
    private IconPresenter? _unselected;
    private readonly RotateTransform _rotation = new();

    static SwitchHandle() => AffectsRender<SwitchHandle>(BrushProperty, HandleOpacityProperty, HandleShapeProperty,
        HandleWidthProperty, HandleHeightProperty);

    public SwitchHandle()
    {
        IsHitTestVisible = false;
        UseLayoutRounding = false;
        Transitions =
        [
            new SpringDoubleTransition
                { Property = HandleWidthProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = HandleHeightProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = SelectedAngleProperty, Style = MotionStyle.Spatial, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = SelectedVisibilityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = UnselectedVisibilityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = HandleOpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = SelectedIconOpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast },
            new SpringDoubleTransition
                { Property = UnselectedIconOpacityProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast },
            new SpringBrushTransition
                { Property = BrushProperty, Style = MotionStyle.Effects, Speed = MotionSpeed.Fast }
        ];
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsPressed
    {
        get => GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public SwitchIconConfiguration IconConfiguration
    {
        get => GetValue(IconConfigurationProperty);
        set => SetValue(IconConfigurationProperty, value);
    }

    public double UnselectedWidth
    {
        get => GetValue(UnselectedWidthProperty);
        set => SetValue(UnselectedWidthProperty, value);
    }

    public double UnselectedHeight
    {
        get => GetValue(UnselectedHeightProperty);
        set => SetValue(UnselectedHeightProperty, value);
    }

    public double SelectedWidth
    {
        get => GetValue(SelectedWidthProperty);
        set => SetValue(SelectedWidthProperty, value);
    }

    public double SelectedHeight
    {
        get => GetValue(SelectedHeightProperty);
        set => SetValue(SelectedHeightProperty, value);
    }

    public double WithIconWidth
    {
        get => GetValue(WithIconWidthProperty);
        set => SetValue(WithIconWidthProperty, value);
    }

    public double WithIconHeight
    {
        get => GetValue(WithIconHeightProperty);
        set => SetValue(WithIconHeightProperty, value);
    }

    public double PressedWidth
    {
        get => GetValue(PressedWidthProperty);
        set => SetValue(PressedWidthProperty, value);
    }

    public double PressedHeight
    {
        get => GetValue(PressedHeightProperty);
        set => SetValue(PressedHeightProperty, value);
    }

    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    public double HandleOpacity
    {
        get => GetValue(HandleOpacityProperty);
        set => SetValue(HandleOpacityProperty, value);
    }

    public CornerRadius HandleShape
    {
        get => GetValue(HandleShapeProperty);
        set => SetValue(HandleShapeProperty, value);
    }

    public double SelectedIconOpacity
    {
        get => GetValue(SelectedIconOpacityProperty);
        set => SetValue(SelectedIconOpacityProperty, value);
    }

    public double UnselectedIconOpacity
    {
        get => GetValue(UnselectedIconOpacityProperty);
        set => SetValue(UnselectedIconOpacityProperty, value);
    }

    internal double HandleWidth => GetValue(HandleWidthProperty);
    internal double HandleHeight => GetValue(HandleHeightProperty);
    internal double SelectedVisibility => Math.Clamp(GetValue(SelectedVisibilityProperty), 0, 1);
    internal double UnselectedVisibility => Math.Clamp(GetValue(UnselectedVisibilityProperty), 0, 1);
    internal double SelectedAngle => GetValue(SelectedAngleProperty);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Math.Max(0, HandleWidth);
        var height = Math.Max(0, HandleHeight);
        var rect = new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
        using (context.PushOpacity(Math.Clamp(HandleOpacity, 0, 1)))
            context.DrawRectangle(Brush, null, new RoundedRect(rect, HandleShape));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is Panel icons && icons.Children.Count == 2)
        {
            if (_selected != icons.Children[0] || _unselected != icons.Children[1])
            {
                UnsubscribeIcons();
                _selected = (IconPresenter)icons.Children[0];
                _unselected = (IconPresenter)icons.Children[1];
                _selected.RenderTransformOrigin = RelativePoint.Center;
                _selected.RenderTransform = _rotation;
                _selected.PropertyChanged += OnIconChanged;
                _unselected.PropertyChanged += OnIconChanged;
            }

            Child.Arrange(new Rect(finalSize));
        }

        if (!_arranged) UpdateState();
        _arranged = true;
        UpdateIcons();
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty || change.Property == IsPressedProperty ||
            change.Property == IconConfigurationProperty ||
            change.Property == UnselectedWidthProperty || change.Property == UnselectedHeightProperty ||
            change.Property == SelectedWidthProperty || change.Property == SelectedHeightProperty ||
            change.Property == WithIconWidthProperty || change.Property == WithIconHeightProperty ||
            change.Property == PressedWidthProperty || change.Property == PressedHeightProperty ||
            change.Property == IsEffectivelyEnabledProperty)
            UpdateState();
        if (change.Property == SelectedVisibilityProperty || change.Property == UnselectedVisibilityProperty ||
            change.Property == SelectedAngleProperty || change.Property == SelectedIconOpacityProperty ||
            change.Property == UnselectedIconOpacityProperty)
            UpdateIcons();
    }

    private void OnIconChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IconPresenter.ValueProperty) UpdateState();
    }

    private void UpdateState()
    {
        var withIcon = IconConfiguration == SwitchIconConfiguration.SelectedAndUnselected &&
                       _unselected?.Value is not null;
        var width = IsPressed ? PressedWidth : IsSelected ? SelectedWidth : withIcon ? WithIconWidth : UnselectedWidth;
        var height = IsPressed ? PressedHeight :
            IsSelected ? SelectedHeight :
            withIcon ? WithIconHeight : UnselectedHeight;
        var transitions = Transitions;
        if (!_arranged || !IsEffectivelyEnabled || MotionSettings.ReduceMotion) Transitions = null;
        SetValue(HandleWidthProperty, width);
        SetValue(HandleHeightProperty, height);
        SetValue(SelectedVisibilityProperty,
            IsSelected && IconConfiguration != SwitchIconConfiguration.None && _selected?.Value is not null ? 1 : 0);
        SetValue(UnselectedVisibilityProperty, !IsSelected && withIcon ? 1 : 0);
        SetValue(SelectedAngleProperty, IconConfiguration == SwitchIconConfiguration.Selected && !IsSelected ? -45 : 0);
        Transitions = transitions;
    }

    private void UpdateIcons()
    {
        if (_selected is not null) _selected.Opacity = SelectedVisibility * Math.Clamp(SelectedIconOpacity, 0, 1);
        if (_unselected is not null)
            _unselected.Opacity = UnselectedVisibility * Math.Clamp(UnselectedIconOpacity, 0, 1);
        _rotation.Angle = SelectedAngle;
    }

    private void UnsubscribeIcons()
    {
        if (_selected is not null) _selected.PropertyChanged -= OnIconChanged;
        if (_unselected is not null) _unselected.PropertyChanged -= OnIconChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _arranged = false;
        UpdateState();
        UnsubscribeIcons();
        _selected = null;
        _unselected = null;
        base.OnDetachedFromVisualTree(e);
    }
}