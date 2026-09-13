using System.Windows.Input;
using ReactiveUI;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>Holds the independent state and project data of the menu examples.</summary>
public sealed class MenusViewModel
{
    /// <summary>Gets the state of the file example.</summary>
    public MenuScenarioViewModel File { get; } = new();

    /// <summary>Gets the state of the export example.</summary>
    public MenuScenarioViewModel Export { get; } = new();

    /// <summary>Gets the state of the sort example.</summary>
    public MenuScenarioViewModel Sort { get; } = new();

    /// <summary>Gets the state of the editor example.</summary>
    public MenuScenarioViewModel Editor { get; } = new();

    /// <summary>Gets the state of the availability example.</summary>
    public MenuScenarioViewModel Availability { get; } = new();

    /// <summary>Gets the state of the destinations example.</summary>
    public MenuScenarioViewModel Destinations { get; } = new();

    /// <summary>Gets the state of the calendars example.</summary>
    public MenuScenarioViewModel Calendars { get; } = new();

    /// <summary>Gets the state of the mail example.</summary>
    public MenuScenarioViewModel Mail { get; } = new();

    /// <summary>Gets the state of the priority example.</summary>
    public MenuScenarioViewModel Priority { get; } = new();

    /// <summary>Gets the state of the devices example.</summary>
    public MenuScenarioViewModel Devices { get; } = new();

    /// <summary>Gets the state of the workspace example.</summary>
    public MenuScenarioViewModel Workspace { get; } = new();

    /// <summary>Gets the state of the text example.</summary>
    public MenuScenarioViewModel Text { get; } = new();

    /// <summary>Gets the state of the archive example.</summary>
    public MenuScenarioViewModel Archive { get; } = new();

    /// <summary>Gets the state of the density example.</summary>
    public MenuScenarioViewModel Density { get; } = new();

    /// <summary>Gets the state of the touch example.</summary>
    public MenuScenarioViewModel Touch { get; } = new();

    /// <summary>Gets the state of the motion example.</summary>
    public MenuScenarioViewModel Motion { get; } = new();

    /// <summary>Gets the state of the placement example.</summary>
    public MenuScenarioViewModel Placement { get; } = new();

    /// <summary>Gets the state of the tokens example.</summary>
    public MenuScenarioViewModel Tokens { get; } = new();

    /// <summary>Gets the archive entries shown by the project item template.</summary>
    public IReadOnlyList<MenuProjectViewModel> Projects { get; }

    /// <summary>Creates the sample archive data.</summary>
    public MenusViewModel() => Projects = Enumerable.Range(1, 35)
        .Select(i => new MenuProjectViewModel($"Project {i:00} · design review", Archive.SelectCommand)).ToArray();
}

/// <summary>Reports actions within one menu card.</summary>
public sealed class MenuScenarioViewModel : ReactiveObject
{
    private string _status = "Ready";
    private double _historyMaxHeight = double.PositiveInfinity;

    /// <summary>Gets or sets the last action shown on the card.</summary>
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    /// <summary>Gets or sets the available height of the adaptive history menu.</summary>
    public double HistoryMaxHeight
    {
        get => _historyMaxHeight;
        set => this.RaiseAndSetIfChanged(ref _historyMaxHeight, value);
    }

    /// <summary>Gets the command that reports the selected action.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Gets the disabled command for the disconnected device.</summary>
    public ICommand UnavailableCommand { get; }

    /// <summary>Creates commands for the example.</summary>
    public MenuScenarioViewModel()
    {
        SelectCommand = new MenuCommand(this, true);
        UnavailableCommand = new MenuCommand(this, false);
    }

    private sealed class MenuCommand(MenuScenarioViewModel owner, bool enabled) : ICommand
    {
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) => owner.Status = parameter?.ToString() ?? "Ready";

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}

/// <summary>Supplies data and the action for an AXAML-defined project menu item.</summary>
public sealed record MenuProjectViewModel(string Title, ICommand OpenCommand);