using Avalonia.Media;
using Material3.Avalonia.Controls;
using ReactiveUI;

namespace Material3.Avalonia.Demo.ViewModels;

public sealed class BadgesViewModel : ReactiveObject
{
    private int _unreadCount = 7;
    private bool _showZero;
    private bool _isBadgeVisible = true;
    private bool _isOwnerEnabled = true;
    private BadgeVariant _variant;
    private FlowDirection _flowDirection;

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            this.RaiseAndSetIfChanged(ref _unreadCount, value);
            this.RaisePropertyChanged(nameof(InboxAccessibleName));
        }
    }

    public bool ShowZero
    {
        get => _showZero;
        set
        {
            this.RaiseAndSetIfChanged(ref _showZero, value);
            this.RaisePropertyChanged(nameof(InboxAccessibleName));
        }
    }

    public bool IsBadgeVisible
    {
        get => _isBadgeVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isBadgeVisible, value);
            this.RaisePropertyChanged(nameof(InboxAccessibleName));
        }
    }

    public bool IsOwnerEnabled
    {
        get => _isOwnerEnabled;
        set => this.RaiseAndSetIfChanged(ref _isOwnerEnabled, value);
    }

    public BadgeVariant Variant
    {
        get => _variant;
        set
        {
            this.RaiseAndSetIfChanged(ref _variant, value);
            this.RaisePropertyChanged(nameof(InboxAccessibleName));
        }
    }

    public FlowDirection FlowDirection
    {
        get => _flowDirection;
        set => this.RaiseAndSetIfChanged(ref _flowDirection, value);
    }

    public string InboxAccessibleName => !IsBadgeVisible || (UnreadCount == 0 && !ShowZero)
        ? "Inbox"
        : Variant == BadgeVariant.Small
            ? "Inbox, new notification"
            : $"Inbox, {UnreadCount} unread messages";
}