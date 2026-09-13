using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Material3.Avalonia.Demo.ViewModels;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Theme;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Demo.Views;

/// <summary>Shows focused examples of native menus with Material presentation.</summary>
public partial class Menus : UserControl
{
    private readonly HashSet<MenuFlyout> _flyouts = [];
    private readonly HashSet<ContextMenu> _contextMenus = [];
    private readonly List<MenuPlacementWindow> _edgeWindows = [];
    private readonly MaterialTheme _pageTheme;
    private bool _overlay;
    private bool _remapped;
    private Action? _restoreMotion;
    private MenuFlyout ArchiveMenu => (MenuFlyout)ArchiveButton.Flyout!;
    private MenuFlyout WorkspaceMenu => (MenuFlyout)WorkspaceButton.Flyout!;
    private MenuFlyout TokenMenu => (MenuFlyout)TokenButton.Flyout!;
    private MenusViewModel Model => (MenusViewModel)DataContext!;

    /// <summary>Creates the menu gallery.</summary>
    public Menus()
    {
        InitializeComponent();
        var applicationTheme = Application.Current!.Styles.OfType<MaterialTheme>().FirstOrDefault();
        _pageTheme = new MaterialTheme { MotionScheme = null };
        if (applicationTheme is not null)
            _pageTheme.Options = applicationTheme.Options with { MotionScheme = null };
        Scenarios.Styles.Add(_pageTheme);
        foreach (var project in Model.Projects) ArchiveMenu.Items.Add(CreateProjectItem(project));
        ConnectMotion(StandardMotionButton, MotionScheme.Standard, false);
        ConnectMotion(ExpressiveMotionButton, MotionScheme.Expressive, false);
        ConnectMotion(ReducedMotionButton, MotionScheme.Expressive, true);
        ConnectMotion(NoMotionButton, MotionScheme.Expressive, false);
        AddHandler(ContextRequestedEvent, (_, e) =>
        {
            var target = e.Source as Control;
            var flyout = target?.ContextFlyout as MenuFlyout ?? target?.GetLogicalAncestors()
                .OfType<Control>().Select(c => c.ContextFlyout).OfType<MenuFlyout>().FirstOrDefault();
            if (flyout is not null) flyout.Popup.ShouldUseOverlayLayer = _overlay;
        }, RoutingStrategies.Tunnel);
        DetachedFromVisualTree += (_, _) =>
        {
            CloseMenus();
            foreach (var window in _edgeWindows.ToArray()) window.Close();
            _restoreMotion?.Invoke();
        };
    }

    private Control CreateProjectItem(MenuProjectViewModel project)
    {
        var template = (IDataTemplate)this.FindResource("ProjectMenuItem")!;
        var item = template.Build(project)!;
        item.DataContext = project;
        return item;
    }

    private void OnMenuOpening(object? sender, EventArgs e)
    {
        var menu = (MenuFlyout)sender!;
        _flyouts.Add(menu);
        menu.Popup.ShouldUseOverlayLayer = _overlay;
    }

    private void OnMenuOpened(object? sender, EventArgs e) =>
        HostStatus.Text = ((MenuFlyout)sender!).Popup.IsUsingOverlayLayer
            ? "Current popup: overlay"
            : "Current popup: platform";

    private void OnContextAttached(object? sender, LogicalTreeAttachmentEventArgs e) =>
        ConfigureContext((ContextMenu)sender!);

    private void OnContextOpening(object? sender, CancelEventArgs e) => ConfigureContext((ContextMenu)sender!);

    private void ConfigureContext(ContextMenu menu)
    {
        _contextMenus.Add(menu);
        if (menu.FindLogicalAncestorOfType<Popup>() is { } popup) popup.ShouldUseOverlayLayer = _overlay;
    }

    private void CloseMenus()
    {
        foreach (var flyout in _flyouts) flyout.Hide();
        foreach (var context in _contextMenus) context.Close();
    }

    private void ToggleHost(object? sender, RoutedEventArgs e)
    {
        CloseMenus();
        _overlay = !_overlay;
        foreach (var context in _contextMenus) ConfigureContext(context);
        foreach (var window in _edgeWindows) window.UseOverlay = _overlay;
        HostButton.Content = _overlay ? "Host: overlay" : "Host: platform";
    }

    private void ToggleTheme(object? sender, RoutedEventArgs e) =>
        _pageTheme.Mode = _pageTheme.Mode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;

    private void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || e.Source != item ||
            item.DataContext is not MenuScenarioViewModel model) return;
        Dispatcher.UIThread.Post(() => model.Status = item.ToggleType == MenuItemToggleType.Radio
            ? "Sorted by " + item.Header
            : $"{item.Header}: {(item.IsChecked ? "on" : "off")}");
    }

    private void AddRecentProject(object? sender, RoutedEventArgs e)
    {
        WorkspaceMenu.Items.Insert(WorkspaceMenu.Items.IndexOf(WorkspaceSettingsGap),
            CreateProjectItem(new MenuProjectViewModel("Untitled project", Model.Workspace.SelectCommand)));
        Model.Workspace.Status = "Added a recent project";
    }

    private void ToggleRecentProject(object? sender, RoutedEventArgs e)
    {
        RecentProject.IsVisible = !RecentProject.IsVisible;
        Model.Workspace.Status = RecentProject.IsVisible ? "Recent project shown" : "Recent project hidden";
    }

    private void ToggleHistoryHeight(object? sender, RoutedEventArgs e)
    {
        var limited = double.IsPositiveInfinity(Model.Archive.HistoryMaxHeight);
        Model.Archive.HistoryMaxHeight = limited ? 120 : double.PositiveInfinity;
        HistoryButton.Content = limited ? "History · limited height" : "Adaptive history";
    }

    private void RemapTokens(object? sender, RoutedEventArgs e)
    {
        if (TokenMenu.Popup.Child is not { } presenter) return;
        _remapped = !_remapped;
        presenter.Resources["MdCompMenusStandardItemLabelTextBrush"] = new TokenAlias
            { ResourceKey = _remapped ? "MdSysTertiaryBrush" : "MdSysOnSurfaceBrush" };
        Model.Tokens.Status = "Menu accent changed";
    }

    private void ConnectMotion(Button launcher, MotionScheme scheme, bool reduceMotion)
    {
        var menu = (MenuFlyout)launcher.Flyout!;
        var previous = MotionSettings.GlobalScheme;
        var reduced = MotionSettings.ReduceMotion;
        var active = false;

        void Prepare()
        {
            if (active) return;
            _restoreMotion?.Invoke();
            active = true;
            _restoreMotion = Restore;
            previous = MotionSettings.GlobalScheme;
            reduced = MotionSettings.ReduceMotion;
            MotionSettings.GlobalScheme = scheme;
            MotionSettings.ReduceMotion = reduceMotion;
        }

        void Restore()
        {
            if (!active) return;
            active = false;
            _restoreMotion = null;
            MotionSettings.GlobalScheme = previous;
            MotionSettings.ReduceMotion = reduced;
        }

        menu.Opening += (_, _) => Prepare();
        menu.Closed += (_, _) => Restore();
        launcher.AddHandler(PointerPressedEvent, (_, _) => Prepare(), RoutingStrategies.Tunnel);
        launcher.PointerEntered += (_, _) => Prepare();
        launcher.GotFocus += (_, _) => Prepare();
        launcher.PointerExited += (_, _) =>
        {
            if (!menu.IsOpen && !launcher.IsFocused) Restore();
        };
        launcher.LostFocus += (_, _) =>
        {
            if (!menu.IsOpen && !launcher.IsPointerOver) Restore();
        };
        launcher.DetachedFromVisualTree += (_, _) => Restore();
    }

    private void ShowEdges(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var window = new MenuPlacementWindow { DataContext = Model.Placement, UseOverlay = _overlay };
        _edgeWindows.Add(window);
        window.Closed += (_, _) => _edgeWindows.Remove(window);
        window.Show(owner);
    }
}