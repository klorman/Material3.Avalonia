using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material3.Avalonia.Attached.Controls;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

public partial class Dividers : UserControl
{
    private bool _alternateTokens;

    public Dividers()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => UpdateStatus();
    }

    private void ChangeVariant(object? sender, RoutedEventArgs e)
    {
        DividerAssist.SetVariant(PreviewDivider, DividerAssist.GetVariant(PreviewDivider) switch
        {
            DividerVariant.FullWidth => DividerVariant.Inset,
            DividerVariant.Inset => DividerVariant.MiddleInset,
            _ => DividerVariant.FullWidth
        });
        ResetInsets(sender, e);
    }

    private void ChangeOrientation(object? sender, RoutedEventArgs e)
    {
        DividerAssist.SetOrientation(PreviewDivider,
            DividerAssist.GetOrientation(PreviewDivider) == Orientation.Horizontal
                ? Orientation.Vertical
                : Orientation.Horizontal);
        UpdateStatus();
    }

    private void ChangeFlowDirection(object? sender, RoutedEventArgs e)
    {
        PreviewHost.FlowDirection = PreviewHost.FlowDirection == FlowDirection.LeftToRight
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
        UpdateStatus();
    }

    private void IncreaseStart(object? sender, RoutedEventArgs e)
    {
        DividerAssist.SetInsetStart(PreviewDivider, DividerAssist.GetInsetStart(PreviewDivider) + 8);
        UpdateStatus();
    }

    private void IncreaseEnd(object? sender, RoutedEventArgs e)
    {
        DividerAssist.SetInsetEnd(PreviewDivider, DividerAssist.GetInsetEnd(PreviewDivider) + 8);
        UpdateStatus();
    }

    private void ResetInsets(object? sender, RoutedEventArgs e)
    {
        PreviewDivider.ClearValue(DividerAssist.InsetStartProperty);
        PreviewDivider.ClearValue(DividerAssist.InsetEndProperty);
        UpdateStatus();
    }

    private void RemapTokens(object? sender, RoutedEventArgs e)
    {
        _alternateTokens = !_alternateTokens;
        TokenScope.Resources["MdCompDividerBrush"] = new TokenAlias(
            _alternateTokens ? "MdSysTertiaryBrush" : "MdSysPrimaryBrush");
        TokenScope.Resources["MdCompDividerThickness"] = _alternateTokens ? 4d : 2d;
    }

    private void UpdateStatus()
    {
        PreviewDivider.ApplyStyling();
        PreviewStatus.Text = $"{DividerAssist.GetVariant(PreviewDivider)} · " +
                             $"{DividerAssist.GetOrientation(PreviewDivider)} · {PreviewHost.FlowDirection} · " +
                             $"Start: {DividerAssist.GetInsetStart(PreviewDivider):0} · " +
                             $"End: {DividerAssist.GetInsetEnd(PreviewDivider):0}";
    }
}