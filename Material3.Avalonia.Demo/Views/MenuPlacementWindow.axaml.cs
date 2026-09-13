using Avalonia.Controls;

namespace Material3.Avalonia.Demo.Views;

/// <summary>Exercises menu placement at each corner of a window.</summary>
public partial class MenuPlacementWindow : Window
{
    private readonly HashSet<MenuFlyout> _menus = [];

    internal bool UseOverlay { get; set; }

    /// <summary>Creates the placement examples.</summary>
    public MenuPlacementWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            foreach (var menu in _menus) menu.Hide();
        };
    }

    private void OnMenuOpening(object? sender, EventArgs e)
    {
        var menu = (MenuFlyout)sender!;
        _menus.Add(menu);
        menu.Popup.ShouldUseOverlayLayer = UseOverlay;
    }
}