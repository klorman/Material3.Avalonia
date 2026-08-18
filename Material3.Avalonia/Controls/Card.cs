using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3.Avalonia.Controls;

public enum CardVariant
{
    Elevated,
    Filled,
    Outlined
}

[PseudoClasses(pcPressed)]
public sealed class Card : ContentControl
{
    private const string pcPressed = ":pressed";
    
    public static readonly StyledProperty<CardVariant> VariantProperty =
        AvaloniaProperty.Register<Card, CardVariant>(nameof(Variant), CardVariant.Filled);
    
    public CardVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<Card, bool>(nameof(IsInteractive), true);
    
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    private bool _isPressed;
    private IPointer? _capturedPointer;

    public Card()
    {
        LostFocus += OnLostFocus;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsInteractive || !IsEffectivelyEnabled) return;

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isPressed = true;
            PseudoClasses.Set(pcPressed, true);
            
            _capturedPointer = e.Pointer;
            _capturedPointer.Capture(this);
            
            e.Handled = true;
        }
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isPressed) return;

        _isPressed = false;
        PseudoClasses.Set(pcPressed, false);

        _capturedPointer?.Capture(null);
        _capturedPointer = null;

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_isPressed)
        {
            _isPressed = false;
            PseudoClasses.Set(pcPressed, false);
        }
        _capturedPointer = null;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            PseudoClasses.Set(pcPressed, false);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsInteractive || !IsEffectivelyEnabled) return;

        if (IsFocused && (e.Key is Key.Space or Key.Enter))
        {
            _isPressed = true;
            PseudoClasses.Set(pcPressed, true);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!IsInteractive || !IsEffectivelyEnabled) return;

        if (IsFocused && (e.Key is Key.Space or Key.Enter))
        {
            _isPressed = false;
            PseudoClasses.Set(pcPressed, false);
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsInteractiveProperty && !change.GetNewValue<bool>())
        {
            if (_isPressed)
            {
                _isPressed = false;
                PseudoClasses.Set(pcPressed, false);
                _capturedPointer?.Capture(null);
                _capturedPointer = null;
            }
        }
        else if (change.Property == IsEnabledProperty && !IsEffectivelyEnabled)
        {
            if (_isPressed)
            {
                _isPressed = false;
                PseudoClasses.Set(pcPressed, false);
                _capturedPointer?.Capture(null);
                _capturedPointer = null;
            }
        }
    }
}
