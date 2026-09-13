using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3.Avalonia.Demo.Views;

/// <summary>Exercises menu placement at each corner of a window.</summary>
public partial class MenuPlacementWindow : Window
{
    private readonly HashSet<MenuFlyout> _menus = [];

    private bool _useOverlay;

    internal bool UseOverlay
    {
        get => _useOverlay;
        set
        {
            _useOverlay = value;
            ConfigureMenuHost();
        }
    }

    /// <summary>Creates the placement examples.</summary>
    public MenuPlacementWindow()
    {
        InitializeComponent();
        foreach (var item in EdgeMenu.Items.OfType<MenuItem>())
            item.TemplateApplied += (_, _) => ConfigureMenuHost();
        Closed += (_, _) =>
        {
            foreach (var menu in _menus) menu.Hide();
        };
    }

    private void ConfigureMenuHost()
    {
        foreach (var item in EdgeMenu.Items.OfType<MenuItem>())
            if (item.GetVisualDescendants().OfType<Popup>().FirstOrDefault() is { } popup)
                popup.ShouldUseOverlayLayer = UseOverlay;
    }

    private void ToggleMenuEdge(object? sender, RoutedEventArgs e)
    {
        EdgeMenu.Close();
        Grid.SetRow(EdgeMenu, Grid.GetRow(EdgeMenu) == 0 ? 2 : 0);
    }

    private void ToggleMenuDirection(object? sender, RoutedEventArgs e)
    {
        EdgeMenu.Close();
        EdgeMenu.FlowDirection = EdgeMenu.FlowDirection == FlowDirection.LeftToRight
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    private void OnMenuOpening(object? sender, EventArgs e)
    {
        var menu = (MenuFlyout)sender!;
        _menus.Add(menu);
        menu.Popup.ShouldUseOverlayLayer = UseOverlay;
    }
}