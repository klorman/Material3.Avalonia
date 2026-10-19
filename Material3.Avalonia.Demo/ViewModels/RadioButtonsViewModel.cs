using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>Demonstrates native radio bindings and command activation.</summary>
public sealed class RadioButtonsViewModel : INotifyPropertyChanged
{
    private bool? _home = true;
    private bool? _office = false;
    private int _activations;

    /// <summary>Creates the gallery command.</summary>
    public RadioButtonsViewModel() => CountActivationCommand = new ActivationCommand(() =>
    {
        _activations++;
        Changed(nameof(SelectionText));
    });

    /// <summary>Gets or sets the home address selection.</summary>
    public bool? Home
    {
        get => _home;
        set => SetOption(ref _home, value);
    }

    /// <summary>Gets or sets the office address selection.</summary>
    public bool? Office
    {
        get => _office;
        set => SetOption(ref _office, value);
    }

    /// <summary>Gets the current selection and command count.</summary>
    public string SelectionText =>
        $"Selected: {(Home == true ? "Home" : Office == true ? "Office" : "None")} · Command activations: {_activations}";

    /// <summary>Records native click commands independently of selection.</summary>
    public ICommand CountActivationCommand { get; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetOption(ref bool? field, bool? value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        Changed(name);
        Changed(nameof(SelectionText));
    }

    private void Changed(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class ActivationCommand(Action action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}