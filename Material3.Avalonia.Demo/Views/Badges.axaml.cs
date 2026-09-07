using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

public partial class Badges : UserControl
{
    private bool _alternateTokens;
    private BadgesViewModel Model => (BadgesViewModel)DataContext!;

    public Badges() => InitializeComponent();

    private void Increment(object? sender, RoutedEventArgs e) => Model.UnreadCount++;

    private void NextBoundary(object? sender, RoutedEventArgs e) => Model.UnreadCount = Model.UnreadCount switch
    {
        < 9 => 9,
        < 10 => 10,
        < 999 => 999,
        < 1000 => 1000,
        _ => 9
    };

    private void ClearCount(object? sender, RoutedEventArgs e) => Model.UnreadCount = 0;
    private void ToggleZero(object? sender, RoutedEventArgs e) => Model.ShowZero = !Model.ShowZero;
    private void ToggleBadge(object? sender, RoutedEventArgs e) => Model.IsBadgeVisible = !Model.IsBadgeVisible;
    private void ToggleOwner(object? sender, RoutedEventArgs e) => Model.IsOwnerEnabled = !Model.IsOwnerEnabled;

    private void NextVariant(object? sender, RoutedEventArgs e) => Model.Variant = Model.Variant switch
    {
        BadgeVariant.Auto => BadgeVariant.Small,
        BadgeVariant.Small => BadgeVariant.Large,
        _ => BadgeVariant.Auto
    };

    private void ToggleDirection(object? sender, RoutedEventArgs e) => Model.FlowDirection =
        Model.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private void OpenInbox(object? sender, RoutedEventArgs e)
    {
        Model.UnreadCount = 0;
        ActionStatus.Text = "Inbox opened. The notification was cleared by the application.";
    }

    private void RemapTokens(object? sender, RoutedEventArgs e)
    {
        _alternateTokens = !_alternateTokens;
        TokenScope.Resources["MdCompBadgeLargeShape"] = new TokenAlias(
            _alternateTokens ? "MdSysShapeCornerExtraSmall" : "MdSysShapeCornerFull");
        TokenScope.Resources["MdCompBadgeLargeSize"] = _alternateTokens ? 24d : 16d;
    }
}