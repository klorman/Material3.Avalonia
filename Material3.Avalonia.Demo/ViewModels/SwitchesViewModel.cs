using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>Demonstrates settings applied immediately through native switch bindings.</summary>
public sealed class SwitchesViewModel : INotifyPropertyChanged
{
    private bool? _notificationsEnabled = true;
    private int _activations;

    /// <summary>Creates the native activation counter.</summary>
    public SwitchesViewModel() => CountActivationCommand = new ActivationCommand(() =>
    {
        _activations++;
        Changed(nameof(Status));
    });

    /// <summary>Gets or sets whether notifications appear in the preview.</summary>
    public bool? NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (_notificationsEnabled == value) return;
            _notificationsEnabled = value;
            Changed();
            Changed(nameof(Status));
        }
    }

    /// <summary>Gets the current setting and native click count.</summary>
    public string Status =>
        $"Notifications {(NotificationsEnabled == true ? "on" : "off")} · Native click commands: {_activations}";

    /// <summary>Records native click command activation.</summary>
    public ICommand CountActivationCommand { get; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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