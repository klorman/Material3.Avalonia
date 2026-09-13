using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Material3.Avalonia.Attached;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class MenuFlyoutRegistration
{
    private static readonly ConditionalWeakTable<MenuFlyout, MenuFlyoutRegistration> Registrations = new();
    private readonly MenuFlyout _flyout;
    private readonly List<IDisposable> _bindings = [];
    private Control? _presenter;
    private Button? _button;
    private AvaloniaObject? _colorsSource;
    private AvaloniaObject? _animationSource;
    private AvaloniaObject? _checkmarkSource;
    private Control? _target;

    internal static MenuFlyoutRegistration Get(MenuFlyout flyout) =>
        Registrations.GetValue(flyout, f => new MenuFlyoutRegistration(f));

    private MenuFlyoutRegistration(MenuFlyout flyout)
    {
        _flyout = flyout;
        flyout.Opening += (_, _) => Connect();
        flyout.Opened += (_, _) => Connect();
        flyout.Closing += OnClosing;
        flyout.Closed += (_, _) => Disconnect();
        flyout.PropertyChanged += (_, e) =>
        {
            if (e.Property == MenuAssist.CheckmarkPlacementProperty || e.Property == MenuAssist.ColorStyleProperty ||
                e.Property == MenuAssist.IsAnimationEnabledProperty) Connect();
        };
        if (flyout.IsOpen) Connect();
    }

    internal void Connect()
    {
        if (_flyout.Popup.Child is not Control presenter) return;
        var target = _flyout.Target;
        AvaloniaObject colors = _flyout.IsSet(MenuAssist.ColorStyleProperty) || target is null ? _flyout : target;
        AvaloniaObject animation =
            _flyout.IsSet(MenuAssist.IsAnimationEnabledProperty) || target is null ? _flyout : target;
        AvaloniaObject checkmark =
            _flyout.IsSet(MenuAssist.CheckmarkPlacementProperty) || target is null ? _flyout : target;
        if (_checkmarkSource == checkmark && _presenter == presenter && _target == target && _colorsSource == colors &&
            _animationSource == animation)
            return;
        foreach (var binding in _bindings) binding.Dispose();
        _bindings.Clear();
        _presenter = presenter;
        _target = target;
        _colorsSource = colors;
        _animationSource = animation;
        _checkmarkSource = checkmark;
        _bindings.Add(presenter.Bind(MenuAssist.CheckmarkPlacementProperty,
            checkmark.GetObservable(MenuAssist.CheckmarkPlacementProperty), BindingPriority.Style));
        _bindings.Add(presenter.Bind(MenuAssist.ColorStyleProperty, colors.GetObservable(MenuAssist.ColorStyleProperty),
            BindingPriority.Style));
        _bindings.Add(presenter.Bind(MenuAssist.IsAnimationEnabledProperty,
            animation.GetObservable(MenuAssist.IsAnimationEnabledProperty), BindingPriority.Style));
        if (target is not null)
            _bindings.Add(presenter.Bind(DensityAssist.DensityProperty,
                target.GetObservable(DensityAssist.DensityProperty), BindingPriority.Style));
        MenuPopupPresentation.Ensure(presenter);
        if (target is Button button && button.Flyout == _flyout)
        {
            _button = button;
            button.Classes.Add("m3-menu-open");
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_presenter is not null) MenuPopupPresentation.Find(_presenter)?.OnClosing(sender, e);
    }

    private void Disconnect()
    {
        _button?.Classes.Remove("m3-menu-open");
        _button = null;
        foreach (var binding in _bindings) binding.Dispose();
        _bindings.Clear();
        _presenter = null;
    }
}