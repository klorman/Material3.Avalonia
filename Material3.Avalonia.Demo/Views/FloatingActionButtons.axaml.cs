using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Material3.Avalonia.Controls;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Symbols;

namespace Material3.Avalonia.Demo.Views;

public partial class FloatingActionButtons : UserControl
{
    private readonly SampleCommand _command;
    private string _destination = "Inbox";
    private bool _navigationPending;
    private int _actions;

    public FloatingActionButtons()
    {
        InitializeComponent();
        _command = new SampleCommand(() => ActionStatus.Text = $"Primary action activated {++_actions} times.");
        InteractiveSample.Command = _command;
        Messages.ItemsSource = Enumerable.Range(1, 30).Select(i => $"Message {i:00} — project update").ToArray();
        NavigationSample.PropertyChanged += OnNavigationPresentationChanged;
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _navigationPending = false;
        PresentNavigationAction();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnAvailabilityChanged(object? sender, RoutedEventArgs e) =>
        _command.SetAvailable(AvailableToggle.IsChecked == true);

    private void OnChangeLabel(object? sender, RoutedEventArgs e) => InteractiveSample.Content =
        Equals(InteractiveSample.Content, "Compose") ? "Compose a new message" : "Compose";

    private void OnChangeIcon(object? sender, RoutedEventArgs e) => InteractiveSample.Icon =
        Equals(InteractiveSample.Icon, MaterialSymbol.Edit) ? MaterialSymbol.Add : MaterialSymbol.Edit;

    private void OnChangeSize(object? sender, RoutedEventArgs e) => InteractiveSample.Size =
        InteractiveSample.Size switch
        {
            FloatingActionButtonSize.Small => FloatingActionButtonSize.Medium,
            FloatingActionButtonSize.Medium => FloatingActionButtonSize.Large,
            _ => FloatingActionButtonSize.Small
        };

    private void OnMirror(object? sender, RoutedEventArgs e) => InteractionArea.FlowDirection =
        InteractionArea.FlowDirection == FlowDirection.LeftToRight
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    private void OnAction(object? sender, RoutedEventArgs e) => ActionStatus.Text = "Primary action activated.";

    private void OnStandardMotion(object? sender, RoutedEventArgs e) =>
        MotionSettings.GlobalScheme = MotionScheme.Standard;

    private void OnExpressiveMotion(object? sender, RoutedEventArgs e) =>
        MotionSettings.GlobalScheme = MotionScheme.Expressive;

    private void OnReducedMotion(object? sender, RoutedEventArgs e) => MotionSettings.ReduceMotion =
        sender is CheckBox { IsChecked: true };

    private void OnInbox(object? sender, RoutedEventArgs e) => Navigate("Inbox");
    private void OnProjects(object? sender, RoutedEventArgs e) => Navigate("Projects");

    private void OnSettings(object? sender, RoutedEventArgs e) => Navigate("Settings");

    private void Navigate(string destination)
    {
        if (_destination == destination)
            return;
        _destination = destination;
        PageTitle.Text = destination;
        PageDescription.Text = destination switch
        {
            "Inbox" => "Compose a message in this inbox.",
            "Projects" => "Create a project in this workspace.",
            _ => "This settings page has no primary action, so its FAB stays hidden."
        };
        _navigationPending = true;
        NavigationSample.IsPresented = false;
        if (!NavigationSample.IsVisible)
            CompleteNavigation();
    }

    private void OnNavigationPresentationChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && !NavigationSample.IsVisible && _navigationPending)
            Dispatcher.UIThread.Post(CompleteNavigation, DispatcherPriority.Render);
    }

    private void CompleteNavigation()
    {
        if (!_navigationPending || NavigationSample.IsVisible)
            return;
        _navigationPending = false;
        PresentNavigationAction();
    }

    private void PresentNavigationAction()
    {
        if (_destination == "Settings")
        {
            NavigationSample.IsPresented = false;
            return;
        }

        NavigationSample.Content = _destination == "Inbox" ? "Compose" : "Create";
        NavigationSample.Icon = _destination == "Inbox" ? MaterialSymbol.Edit : MaterialSymbol.Add;
        NavigationSample.IsPresented = true;
    }

    private sealed class SampleCommand(Action execute) : ICommand
    {
        private bool _available = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _available;
        public void Execute(object? parameter) => execute();

        public void SetAvailable(bool value)
        {
            _available = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}