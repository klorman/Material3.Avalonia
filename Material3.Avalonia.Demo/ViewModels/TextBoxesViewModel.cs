using System.Collections;
using System.ComponentModel;

namespace Material3.Avalonia.Demo.ViewModels;

/// <summary>
/// Demo view model for native TextBox validation.
/// </summary>
public sealed class TextBoxesViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private string _nativeValidationEmail = "material3.example.com";

    public string NativeValidationEmail
    {
        get => _nativeValidationEmail;
        set
        {
            if (_nativeValidationEmail == value)
                return;

            _nativeValidationEmail = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NativeValidationEmail)));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(NativeValidationEmail)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
        }
    }

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
}
