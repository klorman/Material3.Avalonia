using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class FlyoutRegistration
{
    private static readonly ConditionalWeakTable<Flyout, FlyoutRegistration> Registrations = new();
    private readonly Flyout _flyout;
    private readonly CustomPopupPlacementCallback _defaultPlacementCallback;
    private IDisposable? _classVariant;
    private List<IDisposable>? _defaults;
    private List<IDisposable>? _presenterBindings;
    private FlyoutPresenter? _presenter;
    private bool _updating;
    private bool _materialEnabled;
    private readonly HashSet<ToolTipOwner> _owners = [];

    private FlyoutRegistration(Flyout flyout)
    {
        _flyout = flyout;
        _defaultPlacementCallback = placement => ToolTipPlacement.PlaceRich(_flyout, placement);
        flyout.FlyoutPresenterClasses.CollectionChanged += OnClassesChanged;
        flyout.Opening += OnOpening;
        flyout.PropertyChanged += OnPropertyChanged;
    }

    public event Action? Changed;

    public static FlyoutRegistration Get(Flyout flyout) =>
        Registrations.GetValue(flyout, value => new FlyoutRegistration(value));

    public bool IsRich => FlyoutAssist.GetVariant(_flyout) == FlyoutVariant.RichToolTip;

    public void Configure(ToolTipOwner owner, bool enabled)
    {
        if (enabled)
            _owners.Add(owner);
        else
            _owners.Remove(owner);
        _materialEnabled = _owners.Count > 0;
        Update();
    }

    public void Update()
    {
        if (_updating)
            return;

        _updating = true;
        try
        {
            var hasClass = _flyout.FlyoutPresenterClasses.Contains("rich-tooltip");
            if (hasClass && _classVariant is null)
                _classVariant = _flyout.SetValue(FlyoutAssist.VariantProperty, FlyoutVariant.RichToolTip,
                    BindingPriority.Style);
            else if (!hasClass)
            {
                _classVariant?.Dispose();
                _classVariant = null;
            }

            if (IsRich && _materialEnabled && _defaults is null)
            {
                _defaults = new List<IDisposable>
                {
                    _flyout.SetValue(PopupFlyoutBase.ShowModeProperty, FlyoutShowMode.Transient,
                        BindingPriority.Style)!,
                    _flyout.SetValue(PopupFlyoutBase.PlacementProperty, PlacementMode.Custom, BindingPriority.Style)!,
                    _flyout.SetValue(PopupFlyoutBase.CustomPopupPlacementCallbackProperty,
                        _defaultPlacementCallback, BindingPriority.Style)!
                };
            }
            else if (!IsRich || !_materialEnabled)
            {
                if (_defaults is not null)
                    foreach (var subscription in _defaults)
                        subscription.Dispose();
                _defaults = null;
            }
        }
        finally
        {
            _updating = false;
        }

        Changed?.Invoke();
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Update();

    public void RefreshPlacement(Popup popup)
    {
        if (!IsRich || !_materialEnabled || popup.Placement != PlacementMode.Custom ||
            _flyout.CustomPopupPlacementCallback != _defaultPlacementCallback ||
            popup.CustomPopupPlacementCallback != _defaultPlacementCallback)
            return;

        // Avalonia 12 has no public reposition method. Our callback always replaces Anchor,
        // so this temporary value requests positioning without changing the resulting geometry or bindings.
        var anchor = popup.PlacementAnchor == PopupAnchor.Top ? PopupAnchor.Bottom : PopupAnchor.Top;
        using (popup.SetValue(Popup.PlacementAnchorProperty, anchor, BindingPriority.Animation))
        {
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_updating && e.Property == PopupFlyoutBase.ShowModeProperty)
            Changed?.Invoke();
    }

    private void OnOpening(object? sender, EventArgs e)
    {
        _materialEnabled = _flyout.Target is { } target && ToolTipAssist.GetIsMaterialBehaviorEnabled(target);
        Update();

        if (_flyout.Popup.Child is FlyoutPresenter presenter && presenter != _presenter)
        {
            if (_presenterBindings is not null)
                foreach (var subscription in _presenterBindings)
                    subscription.Dispose();
            _presenter = presenter;
            _presenterBindings = new List<IDisposable>
            {
                presenter.Bind(FlyoutAssist.VariantProperty, _flyout.GetObservable(FlyoutAssist.VariantProperty),
                    BindingPriority.Template),
                presenter.Bind(FlyoutAssist.SubheadProperty, _flyout.GetObservable(FlyoutAssist.SubheadProperty),
                    BindingPriority.Template),
                presenter.Bind(FlyoutAssist.SubheadTemplateProperty,
                    _flyout.GetObservable(FlyoutAssist.SubheadTemplateProperty), BindingPriority.Template),
                presenter.Bind(FlyoutAssist.ActionsProperty, _flyout.GetObservable(FlyoutAssist.ActionsProperty),
                    BindingPriority.Template),
                presenter.Bind(FlyoutAssist.ActionsTemplateProperty,
                    _flyout.GetObservable(FlyoutAssist.ActionsTemplateProperty), BindingPriority.Template)
            };
        }

        if (IsRich && _materialEnabled && _flyout.Target is { } owner)
        {
            if (ToolTipAssist.GetOwner(owner, create: true)?.PrepareFlyout(_flyout) == false &&
                e is CancelEventArgs cancel)
                cancel.Cancel = true;
        }
    }
}