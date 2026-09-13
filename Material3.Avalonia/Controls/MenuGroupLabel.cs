using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;

namespace Material3.Avalonia.Controls;

/// <summary>Displays a noninteractive label inside a Material menu group.</summary>
public class MenuGroupLabel : Separator
{
    /// <summary>Defines the group label text.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MenuGroupLabel, string?>(nameof(Text));

    /// <summary>Gets or sets the group label text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new LabelPeer(this);

    private sealed class LabelPeer(MenuGroupLabel owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.Text;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
    }
}