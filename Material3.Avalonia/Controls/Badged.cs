using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;

namespace Material3.Avalonia.Controls;

/// <summary>
/// Anchors a badge to the upper trailing corner of its content without enlarging the content's layout slot.
/// </summary>
[TemplatePart("PART_BadgePresenter", typeof(ContentPresenter))]
public class Badged : ContentControl
{
    /// <summary>Defines the badge displayed over the content.</summary>
    public static readonly StyledProperty<Badge?> BadgeProperty =
        AvaloniaProperty.Register<Badged, Badge?>(nameof(Badge));

    /// <summary>Gets or sets the badge displayed over the content.</summary>
    public Badge? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <inheritdoc />
    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        presenter.Name == "PART_BadgePresenter" || base.RegisterContentPresenter(presenter);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BadgeProperty)
        {
            if (change.OldValue is Badge oldBadge)
                LogicalChildren.Remove(oldBadge);
            if (change.NewValue is Badge newBadge)
                LogicalChildren.Add(newBadge);
        }
    }
}