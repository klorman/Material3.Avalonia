using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Motion;

namespace Material3.Avalonia.Controls;

/// <summary>Material floating action button sizes.</summary>
public enum FloatingActionButtonSize
{
    /// <summary>A 56 dp FAB or small extended FAB.</summary>
    Small,

    /// <summary>An 80 dp FAB or medium extended FAB.</summary>
    Medium,

    /// <summary>A 96 dp FAB or large extended FAB.</summary>
    Large
}

/// <summary>Material floating action button color variants.</summary>
public enum FloatingActionButtonVariant
{
    /// <summary>Primary container and on-primary-container colors.</summary>
    PrimaryContainer,

    /// <summary>Secondary container and on-secondary-container colors.</summary>
    SecondaryContainer,

    /// <summary>Tertiary container and on-tertiary-container colors.</summary>
    TertiaryContainer,

    /// <summary>Primary and on-primary colors.</summary>
    Primary,

    /// <summary>Secondary and on-secondary colors.</summary>
    Secondary,

    /// <summary>Tertiary and on-tertiary colors.</summary>
    Tertiary
}

/// <summary>A Material floating action button with an optional, collapsible content label.</summary>
[PseudoClasses(":extended", ":presented")]
[TemplatePart("PART_Reveal", typeof(FabReveal))]
public class FloatingActionButton : Button
{
    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<FloatingActionButton, object?>(nameof(Icon));

    /// <summary>Identifies the <see cref="Size"/> property.</summary>
    public static readonly StyledProperty<FloatingActionButtonSize> SizeProperty =
        AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonSize>(nameof(Size),
            validate: value => Enum.IsDefined(value));

    /// <summary>Identifies the <see cref="Variant"/> property.</summary>
    public static readonly StyledProperty<FloatingActionButtonVariant> VariantProperty =
        AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonVariant>(nameof(Variant),
            validate: value => Enum.IsDefined(value));

    /// <summary>Identifies the <see cref="IsExtended"/> property.</summary>
    public static readonly StyledProperty<bool> IsExtendedProperty =
        AvaloniaProperty.Register<FloatingActionButton, bool>(nameof(IsExtended), true);

    /// <summary>Identifies the <see cref="IsPresented"/> property.</summary>
    public static readonly StyledProperty<bool> IsPresentedProperty =
        AvaloniaProperty.Register<FloatingActionButton, bool>(nameof(IsPresented), true);

    /// <summary>Identifies the <see cref="ScrollViewer"/> property.</summary>
    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<FloatingActionButton, ScrollViewer?>(nameof(ScrollViewer));

    private FabReveal? _reveal;
    private ScrollViewer? _scrollViewer;
    private IDisposable? _automaticTip;
    private string? _automaticTipText;
    private bool _isExiting;
    private bool _scrollExtended = true;
    private double _scrollDelta;
    private double _lastOffset;
    private bool _animateOnApplyTemplate;

    static FloatingActionButton()
    {
        IsVisibleProperty.OverrideMetadata<FloatingActionButton>(new StyledPropertyMetadata<bool>(
            coerce: (owner, visible) => visible && (((FloatingActionButton)owner).ShouldPresent ||
                                                    ((FloatingActionButton)owner)._isExiting)));
        IsHitTestVisibleProperty.OverrideMetadata<FloatingActionButton>(new StyledPropertyMetadata<bool>(
            coerce: (owner, value) => value && ((FloatingActionButton)owner).ShouldPresent));
        FocusableProperty.OverrideMetadata<FloatingActionButton>(new StyledPropertyMetadata<bool>(
            coerce: (owner, value) => value && ((FloatingActionButton)owner).ShouldPresent));
    }

    /// <summary>Gets or sets the icon displayed by the button.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the Material size; Small is 56 dp.</summary>
    public FloatingActionButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Gets or sets the Material color variant.</summary>
    public FloatingActionButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Gets or sets whether the label may expand; iconless buttons always retain their label.</summary>
    public bool IsExtended
    {
        get => GetValue(IsExtendedProperty);
        set => SetValue(IsExtendedProperty, value);
    }

    /// <summary>Gets or sets animated visibility; unavailable buttons are also hidden.</summary>
    public bool IsPresented
    {
        get => GetValue(IsPresentedProperty);
        set => SetValue(IsPresentedProperty, value);
    }

    /// <summary>Gets or sets the optional scroll source that collapses the label on downward scrolling.</summary>
    public ScrollViewer? ScrollViewer
    {
        get => GetValue(ScrollViewerProperty);
        set => SetValue(ScrollViewerProperty, value);
    }

    internal bool IsEffectivelyExtended => (Content is not null and not "" || ContentTemplate is not null) &&
                                           (Icon is null || IsExtended && _scrollExtended);

    private bool ShouldPresent => IsPresented && IsEffectivelyEnabled;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_reveal is not null)
            _reveal.Hidden -= OnHidden;
        base.OnApplyTemplate(e);
        _reveal = e.NameScope.Find<FabReveal>("PART_Reveal");
        _isExiting = false;
        UpdatePresentation(false);
        if (_reveal is not null)
        {
            var animate = _animateOnApplyTemplate && ShouldPresent && !MotionSettings.ReduceMotion;
            _reveal.SetPresented(ShouldPresent && !animate, false);
            _reveal.Hidden += OnHidden;
            if (animate)
                QueueEntrance();
        }

        _animateOnApplyTemplate = false;
        UpdateExtended();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ConnectScrollViewer();
        if (_reveal is null)
            _animateOnApplyTemplate = ShouldPresent;
        UpdateExtended();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DisconnectScrollViewer();
        _automaticTip?.Dispose();
        _automaticTip = null;
        _isExiting = false;
        _animateOnApplyTemplate = false;
        _reveal?.SetPresented(ShouldPresent, false);
        CoerceValue(IsVisibleProperty);
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPresentedProperty || change.Property == IsEffectivelyEnabledProperty)
            UpdatePresentation(true);
        if (change.Property == ContentProperty || change.Property == ContentTemplateProperty ||
            change.Property == IconProperty || change.Property == IsExtendedProperty)
            UpdateExtended();
        if (change.Property == ScrollViewerProperty)
        {
            DisconnectScrollViewer();
            if (this.IsAttachedToVisualTree())
                ConnectScrollViewer();
            UpdateExtended();
        }
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        if (ShouldPresent)
            base.OnClick();
    }

    private void UpdatePresentation(bool animate)
    {
        var presented = ShouldPresent;
        animate &= _reveal is not null && this.IsAttachedToVisualTree() && !MotionSettings.ReduceMotion;
        _isExiting = !presented && animate && IsEffectivelyVisible && _reveal!.Alpha > 0;
        if (_reveal is null && presented && this.IsAttachedToVisualTree())
            _animateOnApplyTemplate = true;
        PseudoClasses.Set(":presented", presented);
        CoerceValue(IsHitTestVisibleProperty);
        CoerceValue(FocusableProperty);
        if (!presented && IsKeyboardFocusWithin)
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
        CoerceValue(IsVisibleProperty);
        _reveal?.SetPresented(presented, animate);
    }

    private void QueueEntrance()
    {
        var reveal = _reveal;
        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(reveal, _reveal) && this.IsAttachedToVisualTree())
                reveal?.SetPresented(ShouldPresent, true);
        }, DispatcherPriority.Render);
    }

    private void OnHidden(object? sender, EventArgs e)
    {
        if (ShouldPresent)
            return;
        _isExiting = false;
        CoerceValue(IsVisibleProperty);
    }

    private void UpdateExtended()
    {
        PseudoClasses.Set(":extended", IsEffectivelyExtended);
        var tip = !IsEffectivelyExtended && Content is string text && text.Length > 0 ? text : null;
        if (_automaticTip is not null && _automaticTipText == tip)
            return;
        _automaticTipText = tip;
        // A style-priority fallback also lets an explicit null tip suppress the automatic label.
        var replacement = SetValue(ToolTip.TipProperty, tip, BindingPriority.Style);
        _automaticTip?.Dispose();
        _automaticTip = replacement;
    }

    private void ConnectScrollViewer()
    {
        _scrollViewer = ScrollViewer;
        if (_scrollViewer is null)
            return;
        _lastOffset = _scrollViewer.Offset.Y;
        _scrollViewer.ScrollChanged += OnScrollChanged;
        UpdateScrollState();
    }

    private void DisconnectScrollViewer()
    {
        if (_scrollViewer is not null)
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        _scrollViewer = null;
        _scrollExtended = true;
        _scrollDelta = 0;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => UpdateScrollState();

    private void UpdateScrollState()
    {
        if (_scrollViewer is not { } viewer)
            return;
        var offset = viewer.Offset.Y;
        var delta = offset - _lastOffset;
        _lastOffset = offset;
        var maximum = Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height);
        if (offset <= .5 || offset >= maximum - .5)
        {
            _scrollExtended = true;
            _scrollDelta = 0;
        }
        else
        {
            if (Math.Sign(delta) != Math.Sign(_scrollDelta))
                _scrollDelta = 0;
            _scrollDelta += delta;
            // Accumulating a small distance avoids oscillation from fractional scroll offsets.
            if (Math.Abs(_scrollDelta) >= 2)
            {
                _scrollExtended = _scrollDelta < 0;
                _scrollDelta = 0;
            }
        }

        UpdateExtended();
    }
}