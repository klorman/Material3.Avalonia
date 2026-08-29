using System.ComponentModel;
using System.Windows.Input;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>
/// Demo view model for button density.
/// </summary>
public sealed class ButtonsViewModel : INotifyPropertyChanged
{
    private MaterialDensity _selectedDensity = MaterialDensity.Default;

    public ButtonsViewModel()
    {
        SetDensityCommand = new SetDensityCommandImpl(this);
    }

    public MaterialDensity SelectedDensity
    {
        get => _selectedDensity;
        set
        {
            if (_selectedDensity == value)
                return;

            _selectedDensity = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDensity)));
        }
    }

    public ICommand SetDensityCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private sealed class SetDensityCommandImpl : ICommand
    {
        private readonly ButtonsViewModel _owner;

        public SetDensityCommandImpl(ButtonsViewModel owner)
        {
            _owner = owner;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return parameter is MaterialDensity;
        }

        public void Execute(object? parameter)
        {
            _owner.SelectedDensity = parameter is MaterialDensity density
                ? density
                : throw new ArgumentException("Density command parameter must be a MaterialDensity.",
                    nameof(parameter));
        }
    }
}
