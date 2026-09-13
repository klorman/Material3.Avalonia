using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class MenuCheckmarkPresentation : IDisposable
{
    private readonly MenuItem _item;
    private readonly Control _label;
    private readonly Control _leadingSlot;
    private readonly Control _trailingSlot;
    private readonly Control _leadingIcon;
    private readonly Control _trailingIcon;
    private readonly Control _leadingCheck;
    private readonly Control _trailingCheck;
    private readonly Offset _textTranslation;
    private readonly Offset _leadingTranslation;
    private readonly Offset _trailingTranslation;
    private readonly Stopwatch _clock = new();
    private bool _pending;
    private bool _running;
    private bool _disposed;
    private int _generation;
    private double _offset;
    private double _startOffset;
    private double _leadingOpacity;
    private double _trailingOpacity;
    private double _startLeading;
    private double _startTrailing;
    private double _trailingRelative;
    private double _trailingExpansion;
    private double _startExpansion;
    private MenuCheckmarkPlacement _placement;

    internal MenuCheckmarkPresentation(MenuItem item, INameScope scope)
    {
        _item = item;
        _label = scope.Find<Control>("PART_Label")!;
        _leadingSlot = scope.Find<Control>("PART_LeadingSlot")!;
        _trailingSlot = scope.Find<Control>("PART_TrailingSlot")!;
        _leadingIcon = scope.Find<Control>("PART_LeadingIconHost")!;
        _trailingIcon = scope.Find<Control>("PART_TrailingIconHost")!;
        _leadingCheck = scope.Find<Control>("PART_LeadingCheckHost")!;
        _trailingCheck = scope.Find<Control>("PART_TrailingCheckHost")!;
        _textTranslation = new Offset(_label);
        _leadingTranslation = new Offset(_leadingCheck);
        _trailingTranslation = new Offset(_trailingCheck);
        _item.PropertyChanged += OnChanged;
        _item.LayoutUpdated += OnLayout;
        _item.DetachedFromVisualTree += OnDetached;
        _item.AttachedToVisualTree += OnAttached;
        Snap();
    }

    private double LeadingTarget =>
        MenuItemPresentation.GetDisplayChecked(_item) && _placement == MenuCheckmarkPlacement.Leading ? 1 : 0;

    private double TrailingTarget =>
        MenuItemPresentation.GetDisplayChecked(_item) && _placement == MenuCheckmarkPlacement.Trailing ? 1 : 0;

    private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MenuItemPresentation.DisplayCheckedProperty)
        {
            if (!_item.IsAttachedToVisualTree() || _label.Bounds.Width == 0 || !MenuAssist.GetIsAnimationEnabled(_item))
            {
                Snap();
                return;
            }

            var previousLeadingExtent = _leadingSlot.Width + _leadingSlot.Margin.Left + _leadingSlot.Margin.Right;
            _pending = true;
            _running = false;
            _generation++;
            ConfigureSlots();
            var nextLeadingExtent = _leadingSlot.Width + _leadingSlot.Margin.Left + _leadingSlot.Margin.Right;
            _startOffset = MotionSettings.ReduceMotion ? 0 : _offset + previousLeadingExtent - nextLeadingExtent;
            _offset = _startOffset;
            Apply();
            _item.InvalidateMeasure();
        }
        else if (e.Property == MenuAssist.CheckmarkPlacementProperty || e.Property == MenuItem.IconProperty ||
                 e.Property == MenuAssist.TrailingIconProperty || e.Property == Visual.FlowDirectionProperty ||
                 e.Property == MenuItemPresentation.LeadingIconSizeProperty ||
                 e.Property == MenuItemPresentation.TrailingIconSizeProperty ||
                 e.Property == MenuItemPresentation.LeadingGapProperty ||
                 e.Property == MenuItemPresentation.TrailingGapProperty ||
                 e.Property == MenuAssist.IsAnimationEnabledProperty)
            Snap();
    }

    private bool ExpandsTrailing =>
        _placement == MenuCheckmarkPlacement.Trailing && MenuAssist.GetTrailingIcon(_item) is null;

    private void UpdateTrailingSlot()
    {
        var size = MenuItemPresentation.GetTrailingIconSize(_item);
        var gap = MenuItemPresentation.GetTrailingGap(_item);
        var amount = ExpandsTrailing ? Math.Max(0, _trailingExpansion) :
            MenuAssist.GetTrailingIcon(_item) is not null ? 1 : 0;
        _trailingSlot.Width = size * amount;
        _trailingSlot.Margin = new Thickness(gap.Left * amount, gap.Top, gap.Right * amount, gap.Bottom);
    }

    private void ConfigureSlots()
    {
        _placement = MenuAssist.GetCheckmarkPlacement(_item);
        var leading = _item.Icon is not null || LeadingTarget > 0;
        _leadingSlot.Width = leading ? MenuItemPresentation.GetLeadingIconSize(_item) : 0;
        _leadingSlot.Margin = leading ? MenuItemPresentation.GetLeadingGap(_item) : default;
        UpdateTrailingSlot();
    }

    private void OnLayout(object? sender, EventArgs e)
    {
        if (_disposed) return;
        if (_pending)
        {
            _pending = false;
            if (MotionSettings.ReduceMotion) _offset = _startOffset = 0;
            _startLeading = _leadingOpacity;
            _startTrailing = _trailingOpacity;
            _startExpansion = _trailingExpansion;
            _running = true;
            _clock.Restart();
            RequestFrame(++_generation);
        }

        if (TrailingTarget > 0) _trailingRelative = _trailingSlot.Bounds.X - _label.Bounds.X;
        Apply();
    }

    private void RequestFrame(int generation) =>
        TopLevel.GetTopLevel(_item)?.RequestAnimationFrame(_ => Tick(generation));

    private void Tick(int generation)
    {
        if (_disposed || generation != _generation || !_running) return;
        if (!MenuAssist.GetIsAnimationEnabled(_item))
        {
            Snap();
            return;
        }

        var scheme = MotionSettings.GlobalScheme;
        var spatial = scheme.Resolve(MotionStyle.Spatial, MotionSpeed.Fast);
        var effects = scheme.Resolve(MotionStyle.Effects, MotionSpeed.Fast);
        var seconds = _clock.Elapsed.TotalSeconds;
        var movement = SpringAnalytic.Evaluate(seconds, spatial.Stiffness, spatial.Damping, 0);
        var fade = Math.Clamp(SpringAnalytic.Evaluate(seconds, effects.Stiffness, effects.Damping, 0), 0, 1);
        _offset = MotionSettings.ReduceMotion ? 0 : _startOffset * (1 - movement);
        _trailingExpansion = MotionSettings.ReduceMotion
            ? TrailingTarget
            : _startExpansion + (TrailingTarget - _startExpansion) * movement;
        UpdateTrailingSlot();
        _leadingOpacity = _startLeading + (LeadingTarget - _startLeading) * fade;
        _trailingOpacity = _startTrailing + (TrailingTarget - _startTrailing) * fade;
        var duration = Math.Max(
            MotionSettings.ReduceMotion ? 0 : SpringDuration.ComputeSeconds(spatial.Stiffness, spatial.Damping, false),
            SpringDuration.ComputeSeconds(effects.Stiffness, effects.Damping, true));
        if (seconds >= duration)
        {
            Snap();
            return;
        }

        Apply();
        RequestFrame(generation);
    }

    private void Apply()
    {
        _textTranslation.Set(_offset);
        _leadingTranslation.Set(_running || _pending
            ? _label.Bounds.X - _leadingSlot.Bounds.X -
            MenuItemPresentation.GetLeadingIconSize(_item) -
            MenuItemPresentation.GetLeadingGap(_item).Right + _offset
            : 0);
        _trailingTranslation.Set(!ExpandsTrailing && (_running || _pending)
            ? _label.Bounds.X + _trailingRelative - _trailingSlot.Bounds.X + _offset
            : 0);
        _leadingCheck.Opacity = _leadingOpacity;
        _trailingCheck.Opacity = _trailingOpacity;
        _leadingCheck.IsVisible = _leadingOpacity > 0 || LeadingTarget > 0;
        _trailingCheck.IsVisible = _trailingOpacity > 0 || TrailingTarget > 0;
        _leadingIcon.Opacity = 1 - _leadingOpacity;
        _trailingIcon.Opacity = 1 - _trailingOpacity;
        _leadingIcon.IsVisible = _item.Icon is not null;
        _trailingIcon.IsVisible = MenuAssist.GetTrailingIcon(_item) is not null;
    }

    internal void Snap()
    {
        _generation++;
        _pending = _running = false;
        _clock.Stop();
        _placement = MenuAssist.GetCheckmarkPlacement(_item);
        _trailingExpansion = TrailingTarget;
        ConfigureSlots();
        _offset = 0;
        _leadingOpacity = LeadingTarget;
        _trailingOpacity = TrailingTarget;
        Apply();
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Snap();
    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Snap();

    public void Dispose()
    {
        Snap();
        _disposed = true;
        _item.PropertyChanged -= OnChanged;
        _item.LayoutUpdated -= OnLayout;
        _item.DetachedFromVisualTree -= OnDetached;
        _item.AttachedToVisualTree -= OnAttached;
        _textTranslation.Set(0);
        _leadingTranslation.Set(0);
        _trailingTranslation.Set(0);
    }

    private sealed class Offset(Control target)
    {
        private readonly TranslateTransform _translation = new();
        private IDisposable? _value;

        internal void Set(double value)
        {
            if (Math.Abs(value) < .00001)
            {
                _value?.Dispose();
                _value = null;
                return;
            }

            if (_value is null)
            {
                ITransform transform = _translation;
                if (target.RenderTransform is { } original)
                    transform = new TransformGroup
                    {
                        Children = { original as Transform ?? new MatrixTransform(original.Value), _translation }
                    };
                _value = target.SetValue(Visual.RenderTransformProperty, transform, BindingPriority.Animation);
            }

            _translation.X = value;
        }
    }
}