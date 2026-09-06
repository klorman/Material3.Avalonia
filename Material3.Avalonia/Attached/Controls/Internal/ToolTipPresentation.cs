using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3.Avalonia.Controls.Primitives;

namespace Material3.Avalonia.Attached.Controls.Internal;

internal sealed class ToolTipPresentation : IDisposable
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ToolTipPresentation));

    public static readonly AttachedProperty<bool> IsPresentedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsPresented", typeof(ToolTipPresentation), true);

    private static readonly ConditionalWeakTable<Control, ToolTipPresentation> Presentations = new();

    static ToolTipPresentation()
    {
        IsPresentedProperty.Changed.AddClassHandler<Control>((control, change) =>
        {
            control.ApplyTemplate();
            foreach (var reveal in control.GetVisualDescendants().OfType<ToolTipReveal>())
                reveal.SetPresented(change.GetNewValue<bool>());
        });
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, change) =>
        {
            if (change.GetNewValue<bool>())
                Presentations.GetValue(control, target => new ToolTipPresentation(target));
            else if (Presentations.TryGetValue(control, out var presentation))
            {
                presentation.Dispose();
                Presentations.Remove(control);
            }
        });
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
    public static bool GetIsPresented(Control control) => control.GetValue(IsPresentedProperty);
    public static void SetIsPresented(Control control, bool value) => control.SetValue(IsPresentedProperty, value);

    private readonly Control _control;
    private Popup? _popup;
    private Control? _target;
    private IDisposable? _classVariant;
    private IDisposable? _placement;
    private PopupRoot? _nativeHost;

    public ToolTipPresentation(Control control)
    {
        _control = control;
        control.AttachedToVisualTree += OnAttached;
        control.DetachedFromVisualTree += OnDetached;
        if (control is FlyoutPresenter)
        {
            control.Classes.CollectionChanged += OnClassesChanged;
            UpdateClass();
        }

        if (control.IsAttachedToVisualTree())
            Connect();
    }

    public void Dispose()
    {
        Disconnect();
        _control.AttachedToVisualTree -= OnAttached;
        _control.DetachedFromVisualTree -= OnDetached;
        _control.Classes.CollectionChanged -= OnClassesChanged;
        _classVariant?.Dispose();
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Connect();
    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Disconnect();
    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateClass();

    private void UpdateClass()
    {
        if (_control.Classes.Contains("rich-tooltip") && _classVariant is null)
            _classVariant = _control.SetValue(FlyoutAssist.VariantProperty, FlyoutVariant.RichToolTip,
                BindingPriority.Style);
        else if (!_control.Classes.Contains("rich-tooltip"))
        {
            _classVariant?.Dispose();
            _classVariant = null;
        }
    }

    private void Connect()
    {
        Disconnect();
        _popup = _control.FindLogicalAncestorOfType<Popup>();
        if (_popup is null)
            return;

        _target = _popup.PlacementTarget;
        if (_target is not null)
        {
            // Native popup parents are not logical children of the anchor and miss its resource notifications.
            _target.ResourcesChanged += OnTargetResourcesChanged;
            ToolTipAssist.GetOwner(_target)?.AttachPresentation(_popup, _control);
        }

        if (_control is ToolTip && _target is not null)
            _placement = ToolTipPlacement.ConfigurePlain(_popup, _target);
        SetIsPresented(_control, false);
        foreach (var reveal in _control.GetVisualDescendants().OfType<ToolTipReveal>())
            reveal.Reset();
        _nativeHost = TopLevel.GetTopLevel(_control) as PopupRoot;
        if (_nativeHost is not null)
            _nativeHost.PositionChanged += OnPositionChanged;
        _popup.Opened += OnOpened;
        _popup.Closed += OnClosed;
        if (_popup.IsOpen)
            ShowSurface();
    }

    private void Disconnect()
    {
        if (_nativeHost is not null)
            _nativeHost.PositionChanged -= OnPositionChanged;
        _nativeHost = null;
        _placement?.Dispose();
        _placement = null;
        if (_target is not null)
            _target.ResourcesChanged -= OnTargetResourcesChanged;
        _target = null;
        if (_popup is not null)
        {
            _popup.Opened -= OnOpened;
            _popup.Closed -= OnClosed;
            _popup = null;
        }
    }

    private void OnTargetResourcesChanged(object? sender, ResourcesChangedEventArgs e) =>
        ((ILogical?)_popup)?.NotifyResourcesChanged(e);

    private void OnPositionChanged(object? sender, PixelPointEventArgs e) => UpdateDirection();

    private void UpdateDirection()
    {
        if (_popup is null)
            return;
        foreach (var reveal in _control.GetVisualDescendants().OfType<ToolTipReveal>())
            reveal.UpdateDirection(_target, _popup);
    }

    private void OnOpened(object? sender, EventArgs e) => ShowSurface();
    private void OnClosed(object? sender, EventArgs e) => SetIsPresented(_control, false);

    private void ShowSurface()
    {
        var popup = _popup;
        Dispatcher.UIThread.Post(() =>
        {
            if (_popup == popup && popup?.IsOpen == true)
            {
                UpdateDirection();
                SetIsPresented(_control, true);
            }
        }, DispatcherPriority.Render);
    }
}