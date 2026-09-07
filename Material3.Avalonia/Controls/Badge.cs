using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Motion;

namespace Material3.Avalonia.Controls;

/// <summary>
/// Defines how a Material badge displays its label.
/// </summary>
public enum BadgeVariant
{
    /// <summary>Uses a large badge when a count or nonempty content is supplied.</summary>
    Auto,

    /// <summary>Displays a notification dot without a label.</summary>
    Small,

    /// <summary>Displays a label in a large badge container.</summary>
    Large
}

/// <summary>
/// Displays a Material notification dot, count, or short status label.
/// </summary>
[PseudoClasses(":large")]
[TemplatePart("PART_Reveal", typeof(BadgeReveal))]
public class Badge : ContentControl
{
    /// <summary>Defines whether the badge is active, with animated entry and exit.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<Badge, bool>(nameof(IsActive), true);

    /// <summary>Defines the badge variant.</summary>
    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<Badge, BadgeVariant>(nameof(Variant), validate: value => Enum.IsDefined(value));

    /// <summary>Defines the nonnegative count, which takes precedence over content.</summary>
    public static readonly StyledProperty<int?> CountProperty =
        AvaloniaProperty.Register<Badge, int?>(nameof(Count), validate: value => value is null or >= 0);

    /// <summary>Defines the largest count displayed without a plus suffix.</summary>
    public static readonly StyledProperty<int> MaxCountProperty =
        AvaloniaProperty.Register<Badge, int>(nameof(MaxCount), 999, validate: value => value is >= 1 and <= 999);

    /// <summary>Defines whether a zero count remains visible.</summary>
    public static readonly StyledProperty<bool> ShowZeroProperty =
        AvaloniaProperty.Register<Badge, bool>(nameof(ShowZero));

    /// <summary>Defines the formatted content supplied to the badge template.</summary>
    public static readonly DirectProperty<Badge, object?> DisplayContentProperty =
        AvaloniaProperty.RegisterDirect<Badge, object?>(nameof(DisplayContent), badge => badge.DisplayContent);

    private object? _displayContent;
    private BadgeReveal? _reveal;
    private bool _isExiting;
    private bool _animateOnApplyTemplate;

    static Badge()
    {
        IsVisibleProperty.OverrideMetadata<Badge>(new StyledPropertyMetadata<bool>(
            coerce: (owner, visible) => visible && (((Badge)owner).ShouldPresent || ((Badge)owner)._isExiting)));
        FocusableProperty.OverrideDefaultValue<Badge>(false);
        IsHitTestVisibleProperty.OverrideDefaultValue<Badge>(false);
    }

    /// <summary>Gets or sets whether the badge is active; a zero count also hides it unless ShowZero is enabled.</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Gets or sets the badge variant.</summary>
    public BadgeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Gets or sets the nonnegative count, or null to display content instead.</summary>
    public int? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <summary>Gets or sets the count limit from 1 to 999; larger counts display this limit followed by a plus.</summary>
    public int MaxCount
    {
        get => GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    /// <summary>Gets or sets whether a zero count remains visible.</summary>
    public bool ShowZero
    {
        get => GetValue(ShowZeroProperty);
        set => SetValue(ShowZeroProperty, value);
    }

    /// <summary>Gets the formatted count or original content for use in control templates.</summary>
    public object? DisplayContent
    {
        get => _displayContent;
        private set => SetAndRaise(DisplayContentProperty, ref _displayContent, value);
    }

    internal bool IsLarge => Variant == BadgeVariant.Large ||
                             (Variant == BadgeVariant.Auto && (Count.HasValue || Content is not null and not ""));

    private bool ShouldPresent => IsActive && (Count != 0 || ShowZero);

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_reveal is not null)
            _reveal.Hidden -= OnHidden;
        base.OnApplyTemplate(e);
        _reveal = e.NameScope.Find<BadgeReveal>("PART_Reveal");
        _isExiting = false;
        if (_reveal is not null)
        {
            var animate = _animateOnApplyTemplate && ShouldPresent && !MotionSettings.ReduceMotion;
            _reveal.SetPresented(ShouldPresent && !animate, false);
            _reveal.Hidden += OnHidden;
            if (animate)
            {
                var reveal = _reveal;
                // An initially hidden control only creates its template when first shown.
                Dispatcher.UIThread.Post(() =>
                {
                    if (ReferenceEquals(_reveal, reveal) && this.IsAttachedToVisualTree())
                        reveal.SetPresented(ShouldPresent, true);
                }, DispatcherPriority.Render);
            }
        }

        _animateOnApplyTemplate = false;
        CoerceValue(IsVisibleProperty);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _reveal?.SetPresented(ShouldPresent, false);
        _isExiting = false;
        _animateOnApplyTemplate = false;
        CoerceValue(IsVisibleProperty);
    }

    private void OnHidden(object? sender, EventArgs e)
    {
        if (ShouldPresent)
            return;
        _isExiting = false;
        CoerceValue(IsVisibleProperty);
        UpdateDisplayContent();
    }

    private void UpdateDisplayContent()
    {
        DisplayContent = Count is { } count
            ? count > MaxCount
                ? $"{MaxCount.ToString(CultureInfo.CurrentCulture)}+"
                : count.ToString(CultureInfo.CurrentCulture)
            : Content;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CountProperty || change.Property == MaxCountProperty ||
            change.Property == ContentProperty || change.Property == VariantProperty ||
            change.Property == ShowZeroProperty || change.Property == IsActiveProperty)
        {
            if (_reveal is null && ShouldPresent && this.IsAttachedToVisualTree())
                _animateOnApplyTemplate = true;
            var animate = _reveal is not null && this.IsAttachedToVisualTree() && !MotionSettings.ReduceMotion;
            _isExiting = !ShouldPresent && animate && IsEffectivelyVisible && _reveal!.Progress > 0;
            if (!_isExiting)
                UpdateDisplayContent();
            PseudoClasses.Set(":large", IsLarge);
            CoerceValue(IsVisibleProperty);
            _reveal?.SetPresented(ShouldPresent, animate);
            InvalidateMeasure();
            if (ControlAutomationPeer.FromElement(this) is { } peer)
                peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, null, peer.GetName());
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new BadgeAutomationPeer(this);

    private sealed class BadgeAutomationPeer(Badge owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string? GetNameCore()
        {
            if (base.GetNameCore() is { } name)
                return name;
            if (owner.IsLarge)
                return owner.Count?.ToString(CultureInfo.CurrentCulture) ??
                       (owner.Presenter?.Child is { } content
                           ? CreatePeerForElement(content).GetName()
                           : owner.Content?.ToString());
            return owner.TryFindResource("StringBadgeNewNotificationText", out var notification)
                ? notification as string
                : null;
        }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
    }
}