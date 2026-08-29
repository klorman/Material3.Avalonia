using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>
/// Demo view model for native TextBox validation.
/// </summary>
public sealed class TextBoxesViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private string _nativeValidationEmail = "material3.example.com";
    private MaterialDensity _selectedDensity = MaterialDensity.Default;

    public TextBoxesViewModel()
    {
        SetDensityCommand = new SetDensityCommandImpl(this);
    }

    public string NativeValidationEmail
    {
        get => _nativeValidationEmail;
        set
        {
            if (_nativeValidationEmail == value)
                return;

            _nativeValidationEmail = value;
            OnPropertyChanged(nameof(NativeValidationEmail));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(NativeValidationEmail)));
            OnPropertyChanged(nameof(HasErrors));
        }
    }

    public MaterialDensity SelectedDensity
    {
        get => _selectedDensity;
        set
        {
            if (_selectedDensity == value)
                return;

            _selectedDensity = value;
            OnPropertyChanged(nameof(SelectedDensity));
        }
    }

    public ICommand SetDensityCommand { get; }

    public bool HasErrors => GetEmailError() is not null;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName) && propertyName != nameof(NativeValidationEmail))
            return Array.Empty<string>();

        var error = GetEmailError();
        return error is null ? Array.Empty<string>() : new[] { error };
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string? GetEmailError()
    {
        var value = _nativeValidationEmail.Trim();

        if (value.Length == 0)
            return "Email is required";

        var at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
            return "Enter a valid email address";

        var domain = value[(at + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            return "Enter a valid email address";

        return null;
    }

    private sealed class SetDensityCommandImpl : ICommand
    {
        private readonly TextBoxesViewModel _owner;

        public SetDensityCommandImpl(TextBoxesViewModel owner)
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
