using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Material3.Avalonia.Attached.Controls;

namespace Material3.Avalonia.Demo.Views;

/// <summary>Exercises native plain and rich tooltip interaction.</summary>
public partial class ToolTips : UserControl
{
    /// <summary>Initializes the tooltip gallery.</summary>
    public ToolTips()
    {
        InitializeComponent();
        foreach (var control in this.GetLogicalDescendants().OfType<Control>())
            ConfigureHost(control);
    }

    private void DismissToolTip(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.FindLogicalAncestorOfType<Popup>() is { } popup)
            popup.IsOpen = false;
    }

    private void ShowActionResult(object? sender, RoutedEventArgs e)
    {
        ActionResult.Text = "Learn more action selected";
        DismissToolTip(sender, e);
    }

    private bool _useOverlay;
    private readonly List<Control> _anchors = [];

    private void ConfigureHost(Control control)
    {
        if (ToolTip.GetTip(control) is { } content)
        {
            var tip = content as ToolTip ?? new ToolTip { Content = content };
            ToolTip.SetTip(control, tip);
            ToolTip.AddToolTipOpeningHandler(control, (_, _) => ToolTip.SetShouldUseOverlayLayer(control, _useOverlay));
            tip.AttachedToVisualTree += (_, _) =>
                ReportHost(TopLevel.GetTopLevel(tip));
            _anchors.Add(control);
        }

        if ((control as Button)?.Flyout is Flyout flyout)
        {
            flyout.Opening += (_, _) => flyout.Popup.ShouldUseOverlayLayer = _useOverlay;
            flyout.Opened += (_, _) =>
                ActualPopupHost.Text =
                    flyout.Popup.IsUsingOverlayLayer ? "Active: inside window" : "Active: separate popup";
            if (!_anchors.Contains(control))
                _anchors.Add(control);
        }
    }

    private void ReportHost(object? host)
    {
        if (host is not null)
            ActualPopupHost.Text = host is not PopupRoot ? "Active: inside window" : "Active: separate popup";
    }

    private void TogglePopupHost(object? sender, RoutedEventArgs e)
    {
        foreach (var anchor in _anchors)
        {
            ToolTip.SetIsOpen(anchor, false);
            (anchor as Button)?.Flyout?.Hide();
        }

        _useOverlay = !_useOverlay;
        PopupHostButton.Content = _useOverlay ? "Popup host: inside window" : "Popup host: platform";
        ActualPopupHost.Text = "Applies to the next tooltip";
    }

    private void ShowEdgeWindow(object? sender, RoutedEventArgs e)
    {
        var grid = new Grid();
        foreach (var (horizontal, vertical) in new[]
                 {
                     (HorizontalAlignment.Left, VerticalAlignment.Top),
                     (HorizontalAlignment.Right, VerticalAlignment.Top),
                     (HorizontalAlignment.Left, VerticalAlignment.Bottom),
                     (HorizontalAlignment.Right, VerticalAlignment.Bottom)
                 })
        {
            var flyout = new Flyout { Content = "The popup should remain visible, including its shadow and actions." };
            FlyoutAssist.SetVariant(flyout, FlyoutVariant.RichToolTip);
            FlyoutAssist.SetSubhead(flyout, "Screen boundary");
            var button = new Button
            {
                Content = "Hover or Tab",
                Flyout = flyout,
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,
                Margin = new Thickness(4)
            };
            ConfigureHost(button);
            grid.Children.Add(button);
        }

        var window = new Window { Title = "ToolTip placement", Width = 500, Height = 300, Content = grid };
        window.Closed += (_, _) =>
        {
            foreach (var anchor in grid.Children)
                _anchors.Remove(anchor);
        };
        window.Show((Window)TopLevel.GetTopLevel(this)!);
    }
}