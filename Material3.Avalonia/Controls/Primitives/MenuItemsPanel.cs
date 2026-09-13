using System.Collections.Specialized;
using System.Diagnostics;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Motion.Internal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3.Avalonia.Attached.Controls.Internal;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Controls.Primitives;

internal sealed class MenuItemsPanel : VirtualizingStackPanel
{
    public static readonly StyledProperty<double> GroupPaddingProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(GroupPadding));

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(Gap));

    public static readonly StyledProperty<double> ItemInsetProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(ItemInset));

    public static readonly StyledProperty<double> CompressionProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(Compression), .8);

    public static readonly StyledProperty<double> MinimumContentGapProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(MinimumContentGap), 4);

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<MenuItemsPanel, double>(nameof(ItemSpacing));

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public double ItemInset
    {
        get => GetValue(ItemInsetProperty);
        set => SetValue(ItemInsetProperty, value);
    }

    public double Compression
    {
        get => GetValue(CompressionProperty);
        set => SetValue(CompressionProperty, value);
    }

    public double MinimumContentGap
    {
        get => GetValue(MinimumContentGapProperty);
        set => SetValue(MinimumContentGapProperty, value);
    }


    public double GroupPadding
    {
        get => GetValue(GroupPaddingProperty);
        set => SetValue(GroupPaddingProperty, value);
    }

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    private double _availableHeight = double.PositiveInfinity;

    internal double AvailableHeight
    {
        get => _availableHeight;
        set
        {
            if (_availableHeight == value) return;
            _availableHeight = value;
            InvalidateMeasure();
        }
    }

    internal bool HasOverflow { get; private set; }
    internal List<Rect> Surfaces { get; } = [];
    private List<List<Control>> _groups = [];
    private readonly List<MenuGap> _groupGaps = [];
    private double _width;
    private MenuSurface? _surface;
    private bool _motionDirty;

    static MenuItemsPanel() => AffectsMeasure<MenuItemsPanel>(GroupPaddingProperty, GapProperty, ItemInsetProperty,
        CompressionProperty, MinimumContentGapProperty, ItemSpacingProperty);

    public MenuItemsPanel()
    {
        CacheLength = .15;
        this.Bind(ItemSpacingProperty, TokenBindingFactory.Create("MdImplMenusItemSpacing"));
        this.Bind(GroupPaddingProperty, TokenBindingFactory.Create("MdCompMenusGroupPadding"));
        this.Bind(GapProperty, TokenBindingFactory.Create("MdCompMenusGap"));
        this.Bind(ItemInsetProperty, TokenBindingFactory.Create("MdImplMenusItemHorizontalInset"));
        this.Bind(CompressionProperty, TokenBindingFactory.Create("MdImplMenusContentCompression"));
        this.Bind(MinimumContentGapProperty, TokenBindingFactory.Create("MdImplMenusContentMinimumGap"));
        AttachedToVisualTree += (_, _) =>
        {
            _surface = this.GetVisualAncestors().OfType<MenuSurface>().FirstOrDefault();
            _surface?.SetItemsPanel(this);
        };
        LayoutUpdated += (_, _) =>
        {
            if (!_motionDirty) return;
            _motionDirty = false;
            BuildMotion();
            _surface?.GeometryChanged();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            FinishResize();
            _lastSize = default;
            _layoutSurfaces.Clear();
            _layoutRows.Clear();
            ResetMotion();
            foreach (var row in Items.OfType<MenuItem>()) MenuItemPresentation.SetRowSpacing(row, default);
            foreach (var row in GetRealizedContainers()?.OfType<MenuItem>() ?? [])
                MenuItemPresentation.SetRowSpacing(row, default);
            if (_surface?.ItemsPanel == this) _surface.SetItemsPanel(null);
            _surface = null;
        };
    }

    private readonly Dictionary<Control, bool> _visibility = new();
    private int _virtualizing;

    private void TrackVisibility()
    {
        var controls = Items.OfType<Control>().ToHashSet();
        foreach (var old in _visibility.Keys.Where(c => !controls.Contains(c)).ToArray())
        {
            old.PropertyChanged -= OnVisibilityChanged;
            old.SetCurrentValue(IsVisibleProperty, _visibility[old]);
            _visibility.Remove(old);
        }

        foreach (var control in controls)
            if (_visibility.TryAdd(control, control.IsVisible))
                control.PropertyChanged += OnVisibilityChanged;
    }

    private void OnVisibilityChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != IsVisibleProperty || sender is not Control control) return;
        if (_virtualizing == 0)
        {
            _visibility[control] = control.IsVisible;
            _resizeRequested = true;
        }
        else if (!_visibility[control] && control.IsVisible)
            control.SetCurrentValue(IsVisibleProperty, false);
    }

    protected override Control? ScrollIntoView(int index)
    {
        _virtualizing++;
        try
        {
            return base.ScrollIntoView(index);
        }
        finally
        {
            _virtualizing--;
        }
    }

    private sealed class SelectionState
    {
        internal bool? Checked;
        internal string? RadioGroup;
    }

    private readonly List<SelectionState> _selection = [];
    private readonly Dictionary<MenuItem, SelectionState> _generatedChecks = new();

    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
        if (oldValue is not null)
        {
            oldValue.ContainerPrepared -= OnContainerPrepared;
            oldValue.ContainerClearing -= OnContainerClearing;
        }

        foreach (var item in _generatedChecks.Keys.ToArray()) UntrackCheck(item);
        base.OnItemsControlChanged(oldValue);
        _selection.Clear();
        _selection.AddRange(Items.Select(_ => new SelectionState()));
        TrackVisibility();
        if (ItemsControl is { } owner)
        {
            owner.ContainerPrepared += OnContainerPrepared;
            owner.ContainerClearing += OnContainerClearing;
        }
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is MenuItem row) UpdateSpacing(row, e.Index);
        if (e.Container is not MenuItem item || Items[e.Index] is Control) return;
        item.ApplyStyling();
        if (item.ToggleType == MenuItemToggleType.None ||
            BindingOperations.GetBindingExpressionBase(item, MenuItem.IsCheckedProperty) is not null) return;
        var state = _selection[e.Index];
        _generatedChecks[item] = state;
        if (state.Checked is { } value) item.SetCurrentValue(MenuItem.IsCheckedProperty, value);
        item.PropertyChanged += OnCheckChanged;
        SaveCheck(item, state);
    }

    private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container is MenuItem item) UntrackCheck(item);
    }

    private void UntrackCheck(MenuItem item)
    {
        if (!_generatedChecks.Remove(item)) return;
        item.PropertyChanged -= OnCheckChanged;
        item.ClearValue(MenuItem.IsCheckedProperty);
    }

    private void OnCheckChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MenuItem.IsCheckedProperty && sender is MenuItem item &&
            _generatedChecks.TryGetValue(item, out var state)) SaveCheck(item, state);
    }

    private void SaveCheck(MenuItem item, SelectionState state)
    {
        state.Checked = item.IsChecked;
        state.RadioGroup = item.ToggleType == MenuItemToggleType.Radio ? item.GroupName ?? "" : null;
        if (!item.IsChecked || state.RadioGroup is null) return;
        foreach (var other in _selection)
            if (other != state && other.RadioGroup == state.RadioGroup)
                other.Checked = false;
        foreach (var other in _generatedChecks.ToArray())
            if (other.Key != item && other.Value.RadioGroup == state.RadioGroup && other.Key.IsChecked)
                other.Key.SetCurrentValue(MenuItem.IsCheckedProperty, false);
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        _resizeRequested = true;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _selection.InsertRange(e.NewStartingIndex,
                    e.NewItems!.Cast<object?>().Select(_ => new SelectionState()));
                break;
            case NotifyCollectionChangedAction.Remove:
                _selection.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                break;
            case NotifyCollectionChangedAction.Replace:
                _selection.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                _selection.InsertRange(e.NewStartingIndex,
                    e.NewItems!.Cast<object?>().Select(_ => new SelectionState()));
                break;
            case NotifyCollectionChangedAction.Move:
                var moved = _selection.GetRange(e.OldStartingIndex, e.OldItems!.Count);
                _selection.RemoveRange(e.OldStartingIndex, moved.Count);
                _selection.InsertRange(e.NewStartingIndex, moved);
                break;
            default:
                _selection.Clear();
                _selection.AddRange(items.Select(_ => new SelectionState()));
                break;
        }

        TrackVisibility();
        _virtualizing++;
        try
        {
            base.OnItemsChanged(items, e);
        }
        finally
        {
            _virtualizing--;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var control in _visibility.Keys.ToArray())
            if (!global::Avalonia.Diagnostics.AvaloniaObjectExtensions.GetDiagnostic(control, IsVisibleProperty)
                    .IsOverriddenCurrentValue)
                _visibility[control] = control.IsVisible;
        for (var index = 0; index < Items.Count; index++)
            if (Items[index] is MenuItem row)
                UpdateSpacing(row, index);
        foreach (var row in GetRealizedContainers()?.OfType<MenuItem>() ?? [])
            UpdateSpacing(row, IndexFromContainer(row));
        Size estimated;
        _virtualizing++;
        try
        {
            estimated = base.MeasureOverride(new Size(Math.Max(0, availableSize.Width - 2 * ItemInset),
                availableSize.Height));
        }
        finally
        {
            _virtualizing--;
        }

        var realized = GetRealizedContainers()?.ToList() ?? [];
        var allRealized = FirstRealizedIndex == 0 && LastRealizedIndex == Items.Count - 1;
        if (!allRealized && Items.Count > 0)
        {
            HasOverflow = true;
            FinishResize();
            _lastSize = default;
            _layoutSurfaces.Clear();
            _layoutRows.Clear();
            foreach (var gap in realized.OfType<MenuGap>()) gap.Classes.Set("m3-menu-gap", false);
            return new Size(estimated.Width + 2 * ItemInset, estimated.Height + 2 * GroupPadding);
        }

        _groups = [[]];
        _groupGaps.Clear();
        foreach (var child in realized)
        {
            if (!child.IsVisible) continue;
            if (child is MenuGap)
            {
                if (_groups[^1].Count > 0)
                {
                    _groups.Add([]);
                    _groupGaps.Add((MenuGap)child);
                }
            }
            else _groups[^1].Add(child);

            child.Measure(new Size(
                Math.Max(0, availableSize.Width - (child is MenuItem { Header: not "-" } ? 2 * ItemInset : 0)),
                double.PositiveInfinity));
        }

        if (_groups[^1].Count == 0)
        {
            _groups.RemoveAt(_groups.Count - 1);
            if (_groupGaps.Count > 0) _groupGaps.RemoveAt(_groupGaps.Count - 1);
        }

        _width = _groups.SelectMany(g => g)
            .Select(c => c.DesiredSize.Width + (c is MenuItem { Header: not "-" } ? 2 * ItemInset : 0)).DefaultIfEmpty()
            .Max();
        var contentHeight = _groups.SelectMany(g => g).Sum(c => c.DesiredSize.Height);
        var candidate = contentHeight + _groups.Count * 2 * GroupPadding + Math.Max(0, _groups.Count - 1) * Gap;
        HasOverflow = candidate > AvailableHeight + 0.5;
        var height = HasOverflow
            ? contentHeight + 2 * GroupPadding + DividerHeights()
            : candidate;
        return ResizeDesired(new Size(_width, Math.Max(0, height)));
    }

    private void UpdateSpacing(MenuItem item, int index)
    {
        var next = index + 1;
        while (next < Items.Count && Items[next] is Control control &&
               !_visibility.GetValueOrDefault(control, control.IsVisible)) next++;
        var spacing = IsMenuRow(item) && next < Items.Count && IsMenuRow(Items[next]) ? ItemSpacing : 0;
        MenuItemPresentation.SetRowSpacing(item, new Thickness(0, 0, 0, spacing));
    }

    private double DividerHeights() => _groupGaps.Sum(gap => gap.DesiredSize.Height);

    private bool IsMenuRow(object? value) => value switch
    {
        MenuItem item => _visibility.GetValueOrDefault(item, item.IsVisible) && !Equals(item.Header, "-"),
        Control => false,
        _ => !Equals(value, "-")
    };

    private static void UpdatePositions(IEnumerable<Control> controls, bool firstGroup = true, bool lastGroup = true)
    {
        var items = controls.OfType<MenuItem>().Where(i => !Equals(i.Header, "-")).ToList();
        for (var i = 0; i < items.Count; i++)
            MenuItemPresentation.SetPosition(items[i], items.Count == 1 ? firstGroup && lastGroup
                    ? MenuItemPosition.Standalone
                    : firstGroup
                        ? MenuItemPosition.First
                        : lastGroup
                            ? MenuItemPosition.Last
                            : MenuItemPosition.Single :
                i == 0 ? MenuItemPosition.First :
                i == items.Count - 1 ? MenuItemPosition.Last : MenuItemPosition.Middle);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Surfaces.Clear();
        if (HasOverflow)
        {
            base.ArrangeOverride(new Size(finalSize.Width, Math.Max(0, finalSize.Height - 2 * GroupPadding)));
            var realized = GetRealizedContainers()?.ToList() ?? [];
            foreach (var child in realized)
            {
                var inset = child is MenuItem { Header: not "-" } ? ItemInset : 0;
                var rect = child.Bounds.Inflate(child.Margin);
                child.Arrange(new Rect(inset, rect.Y + GroupPadding,
                    Math.Max(0, finalSize.Width - 2 * inset), rect.Height));
                if (child is MenuGap gap) gap.Classes.Set("m3-menu-gap", false);
                if (child is MenuItem item)
                {
                    var index = IndexFromContainer(item);
                    var first = !Items.Take(index).Any(IsMenuRow);
                    var last = !Items.Skip(index + 1).Any(IsMenuRow);
                    MenuItemPresentation.SetPosition(item, first && last ? MenuItemPosition.Standalone :
                        first ? MenuItemPosition.First :
                        last ? MenuItemPosition.Last : MenuItemPosition.Middle);
                }
            }

            Surfaces.Add(new Rect(finalSize));
            _motionDirty = true;
            return finalSize;
        }

        var y = GroupPadding;
        foreach (var gap in Children.OfType<MenuGap>())
        {
            gap.Classes.Set("m3-menu-gap", !HasOverflow);
            gap.Arrange(new Rect(0, 0, 0, 0));
        }

        if (HasOverflow) UpdatePositions(_groups.SelectMany(g => g));
        for (var groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
        {
            var group = _groups[groupIndex];
            var top = y - (HasOverflow && groupIndex > 0 ? 0 : GroupPadding);
            if (!HasOverflow) UpdatePositions(group, groupIndex == 0, groupIndex == _groups.Count - 1);
            foreach (var child in group)
            {
                child.Arrange(new Rect(child is MenuItem { Header: not "-" } ? ItemInset : 0, y,
                    Math.Max(0, finalSize.Width - (child is MenuItem { Header: not "-" } ? 2 * ItemInset : 0)),
                    child.DesiredSize.Height));
                y += child.DesiredSize.Height;
            }

            if (!HasOverflow)
            {
                y += GroupPadding;
                Surfaces.Add(new Rect(0, top, finalSize.Width, y - top));
            }

            if (groupIndex < _groups.Count - 1)
            {
                var separator = _groupGaps[groupIndex];
                var space = HasOverflow ? separator.DesiredSize.Height : Gap;
                separator.Arrange(new Rect(0, y, finalSize.Width, space));
                y += space + (HasOverflow ? 0 : GroupPadding);
            }
        }

        if (HasOverflow && _groups.Count > 0)
            Surfaces.Add(new Rect(0, 0, finalSize.Width, y + GroupPadding));
        ArrangeResize();
        _motionDirty = true;
        return finalSize;
    }

    private readonly Stopwatch _resizeClock = new();
    private readonly Dictionary<object, Rect> _layoutSurfaces = new();
    private readonly Dictionary<Control, double> _layoutRows = new();
    private Dictionary<object, Rect> _fromSurfaces = new();
    private Dictionary<Control, double> _fromRows = new();
    private readonly Dictionary<Control, MotionRow> _resizeRows = new();
    private readonly Dictionary<Control, RevealingRow> _newRows = new();
    private Size _lastSize;
    private Size _resizeFromSize;
    private Size _resizeTargetSize;
    private double _resizeProgress = 1;
    private bool _resizing;
    private bool _resizeRequested;
    private int _resizeGeneration;

    private Size ResizeDesired(Size target)
    {
        var enabled = !HasOverflow && MenuAssist.GetIsAnimationEnabled(this) && !MotionSettings.ReduceMotion &&
                      _surface is { Progress: 1 };
        if (!enabled)
        {
            FinishResize();
            _lastSize = HasOverflow ? default : target;
            if (HasOverflow)
            {
                _layoutSurfaces.Clear();
                _layoutRows.Clear();
            }

            _resizeRequested = false;
            return target;
        }

        if (target != _resizeTargetSize &&
            (_resizeRequested || _resizing || Math.Abs(target.Height - _lastSize.Height) > .5) &&
            _lastSize.Height > 0 && _layoutSurfaces.Count > 0)
        {
            _resizeFromSize = _lastSize;
            _resizeTargetSize = target;
            _fromSurfaces = new Dictionary<object, Rect>(_layoutSurfaces);
            _fromRows = new Dictionary<Control, double>(_layoutRows);
            _resizeClock.Restart();
            _resizeProgress = 0;
            _resizing = true;
            RequestResizeFrame(++_resizeGeneration);
        }

        _resizeRequested = false;
        _lastSize = _resizing
            ? new Size(Math.Max(0, Lerp(_resizeFromSize.Width, target.Width)),
                Math.Max(0, Lerp(_resizeFromSize.Height, target.Height)))
            : target;
        if (!_resizing) _resizeTargetSize = target;
        return _lastSize;
    }

    private void RequestResizeFrame(int generation) => TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ =>
    {
        if (!_resizing || generation != _resizeGeneration) return;
        var token = MotionSettings.GlobalScheme.SpatialFast;
        var seconds = _resizeClock.Elapsed.TotalSeconds;
        if (MotionSettings.ReduceMotion || !MenuAssist.GetIsAnimationEnabled(this) ||
            seconds >= SpringDuration.ComputeSeconds(token.Stiffness, token.Damping, false))
            FinishResize();
        else
        {
            _resizeProgress = SpringAnalytic.Evaluate(seconds, token.Stiffness, token.Damping, 0);
            RequestResizeFrame(generation);
        }

        // Keep frames running when rounding leaves the native popup extent unchanged.
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    });

    private double Lerp(double from, double to) => from + (to - from) * _resizeProgress;

    private void ArrangeResize()
    {
        _layoutSurfaces.Clear();
        for (var i = 0; i < Surfaces.Count; i++)
        {
            object key = i == 0 ? this : _groupGaps[i - 1];
            var target = Surfaces[i];
            if (_resizing && _fromSurfaces.TryGetValue(key, out var from))
                Surfaces[i] = new Rect(target.X, Lerp(from.Y, target.Y), target.Width,
                    Math.Max(0, Lerp(from.Height, target.Height)));
            _layoutSurfaces[key] = Surfaces[i];
        }

        _layoutRows.Clear();
        foreach (var row in _groups.SelectMany(group => group))
        {
            var y = row.Bounds.Y;
            if (_resizing && _fromRows.TryGetValue(row, out var from))
            {
                y = Lerp(from, y);
                if (!_resizeRows.TryGetValue(row, out var transform))
                    _resizeRows[row] = transform = new MotionRow(row, row, 0, 0);
                transform.Apply(y - row.Bounds.Y);
            }

            if (_resizing && (!_fromRows.ContainsKey(row) || _newRows.ContainsKey(row)))
            {
                if (!_newRows.TryGetValue(row, out var reveal)) _newRows[row] = reveal = new RevealingRow(row);
                reveal.Apply(Math.Clamp(_resizeProgress, 0, 1));
            }

            _layoutRows[row] = y;
        }

        foreach (var removed in _resizeRows.Keys.Where(row => !_layoutRows.ContainsKey(row)).ToArray())
        {
            _resizeRows[removed].Dispose();
            _resizeRows.Remove(removed);
        }
    }

    private void FinishResize()
    {
        _resizeGeneration++;
        _resizing = false;
        _resizeProgress = 1;
        _resizeClock.Stop();
        foreach (var row in _resizeRows.Values) row.Dispose();
        _resizeRows.Clear();
        foreach (var row in _newRows.Values) row.Dispose();
        _newRows.Clear();
    }

    private sealed class RevealingRow : IDisposable
    {
        private readonly Control _row;
        private readonly RectangleGeometry _clip = new();
        private readonly IDisposable? _value;
        private double _height;

        internal RevealingRow(Control row)
        {
            _row = row;
            Geometry clip = row.Clip is { } original
                ? new CombinedGeometry
                    { GeometryCombineMode = GeometryCombineMode.Intersect, Geometry1 = original, Geometry2 = _clip }
                : _clip;
            _value = row.SetValue(Visual.ClipProperty, clip, BindingPriority.Animation);
        }

        internal void Apply(double progress)
        {
            _height = Math.Max(_height, _row.Bounds.Height * progress);
            _clip.Rect = new Rect(0, 0, _row.Bounds.Width, _height);
        }

        public void Dispose() => _value?.Dispose();
    }

    private readonly List<MotionRow> _motionRows = [];
    private double _compactHeight;
    private double _motionTop;
    private double _motionBottom;
    private double _progress = 1;
    private bool _fromBottom;
    internal double CollapsedDistance => Math.Max(0, _motionBottom - _compactHeight);

    private void BuildMotion()
    {
        ResetMotion();
        var origin = _surface is null ? default : this.TranslatePoint(default, _surface) ?? default;
        var viewport = _surface?.SurfaceBounds ?? new Rect(Bounds.Size);
        _motionTop = Math.Clamp(viewport.Top - origin.Y, 0, Bounds.Height);
        _motionBottom = Math.Min(Bounds.Height, _motionTop + viewport.Height);
        var controls = Children.Where(c => c.IsVisible && c.Bounds.Height > 0 &&
                                           c.Bounds.Bottom > _motionTop && c.Bounds.Top < _motionBottom &&
                                           (c is not MenuGap || HasOverflow)).OrderBy(c => c.Bounds.Y).ToList();
        var previousCenter = 0d;
        var previousCompact = 0d;
        var previousInk = 0d;
        foreach (var child in controls)
        {
            var target = child is MenuItem
                ? child.GetVisualChildren().OfType<Control>().FirstOrDefault()?.GetVisualChildren()
                    .OfType<Control>().FirstOrDefault(c => c.Name == "PART_Row") ?? child
                : child;
            var content = target.GetVisualChildren().OfType<Control>().FirstOrDefault(c => c.Name == "PART_Content");
            var ink = content is null
                ? child.Bounds.Height
                : content.GetVisualChildren().OfType<Control>().Where(c => c.IsVisible)
                    .Select(c => c.Bounds.Height).DefaultIfEmpty(child.Bounds.Height).Max();
            var center = target.TranslatePoint(new Point(0, target.Bounds.Height / 2), this)?.Y ??
                         child.Bounds.Center.Y;
            var compact = _motionRows.Count == 0
                ? _motionTop + Math.Max((center - _motionTop) * Compression, ink / 2 + GroupPadding)
                : previousCompact + Math.Max((center - previousCenter) * Compression,
                    (previousInk + ink) / 2 + MinimumContentGap);
            compact = Math.Min(center, compact);
            _motionRows.Add(new MotionRow(target, child, center, compact));
            previousCenter = center;
            previousCompact = compact;
            previousInk = ink;
        }

        var remaining = _motionBottom - previousCenter;
        _compactHeight = Math.Min(_motionBottom, previousCompact + Math.Max(remaining * Compression,
            previousInk / 2 + GroupPadding));
        ApplyMotion(_progress, _fromBottom);
    }

    internal void ApplyMotion(double progress, bool fromBottom)
    {
        _progress = progress;
        _fromBottom = fromBottom;
        foreach (var row in _motionRows)
        {
            var offset = (row.Compact - row.Center + (fromBottom ? CollapsedDistance : 0)) * (1 - progress);
            row.Apply(offset);
        }
    }

    internal double MapY(double y, double progress, bool fromBottom)
    {
        var beforeY = _motionTop;
        var beforeCompact = _motionTop;
        foreach (var row in _motionRows)
        {
            if (y <= row.Center)
                return Interpolate(row.Center, row.Compact);
            beforeY = row.Center;
            beforeCompact = row.Compact;
        }

        return Interpolate(_motionBottom, _compactHeight);

        double Interpolate(double afterY, double afterCompact)
        {
            var fraction = afterY == beforeY ? 0 : (y - beforeY) / (afterY - beforeY);
            var compact = beforeCompact + (afterCompact - beforeCompact) * fraction;
            return y + (compact - y + (fromBottom ? CollapsedDistance : 0)) * (1 - progress);
        }
    }

    internal void ResetMotion()
    {
        foreach (var row in _motionRows) row.Dispose();
        _motionRows.Clear();
    }

    private sealed class MotionRow(Control target, Control owner, double center, double compact) : IDisposable
    {
        internal double Center { get; } = center;
        internal double Compact { get; } = compact;
        private readonly TranslateTransform _translation = new();
        private IDisposable? _value;
        private IDisposable? _clip;

        internal void Apply(double offset)
        {
            if (Math.Abs(offset) < .00001)
            {
                _value?.Dispose();
                _value = null;
                _clip?.Dispose();
                _clip = null;
                return;
            }

            if (_value is null)
            {
                _clip = owner.SetValue(ClipToBoundsProperty, false, BindingPriority.Animation);
                ITransform transform = _translation;
                if (target.RenderTransform is { } original)
                    transform = new TransformGroup
                        { Children = { original as Transform ?? new MatrixTransform(original.Value), _translation } };
                _value = target.SetValue(Visual.RenderTransformProperty, transform, BindingPriority.Animation);
            }

            _translation.Y = offset;
        }

        public void Dispose()
        {
            _value?.Dispose();
            _clip?.Dispose();
        }
    }
}