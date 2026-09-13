using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;

namespace Material3.Avalonia.Controls;

/// <summary>Separates the surfaces of adjacent Material menu groups.</summary>
public class MenuGap : Separator
{
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new GapPeer(this);

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Classes.Remove("m3-menu-gap");
        base.OnDetachedFromVisualTree(e);
    }

    private sealed class GapPeer(MenuGap owner) : ControlAutomationPeer(owner)
    {
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}