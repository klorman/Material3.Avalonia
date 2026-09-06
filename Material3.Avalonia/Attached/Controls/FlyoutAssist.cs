using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Material3.Avalonia.Attached.Controls.Internal;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>
/// Defines the presentation of a native Avalonia flyout.
/// </summary>
public enum FlyoutVariant
{
    /// <summary>General-purpose flyout content.</summary>
    Standard,

    /// <summary>Material rich tooltip with optional subhead and actions.</summary>
    RichToolTip
}

/// <summary>
/// Provides Material presentation slots for <see cref="Flyout" /> and <see cref="FlyoutPresenter" />.
/// </summary>
public static class FlyoutAssist
{
    static FlyoutAssist()
    {
        VariantProperty.Changed.AddClassHandler<AvaloniaObject>(OnPresentationChanged);
        SubheadProperty.Changed.AddClassHandler<AvaloniaObject>(OnPresentationChanged);
        SubheadTemplateProperty.Changed.AddClassHandler<AvaloniaObject>(OnPresentationChanged);
        ActionsProperty.Changed.AddClassHandler<AvaloniaObject>(OnPresentationChanged);
        ActionsTemplateProperty.Changed.AddClassHandler<AvaloniaObject>(OnPresentationChanged);
    }

    private static void OnPresentationChanged(AvaloniaObject target, AvaloniaPropertyChangedEventArgs change)
    {
        if (target is Flyout flyout)
            FlyoutRegistration.Get(flyout).Update();
    }

    /// <summary>Defines the flyout presentation variant.</summary>
    public static readonly AttachedProperty<FlyoutVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, FlyoutVariant>(
            "Variant", typeof(FlyoutAssist), defaultValue: FlyoutVariant.Standard);

    /// <summary>Gets the flyout presentation variant.</summary>
    public static FlyoutVariant GetVariant(AvaloniaObject target) => target.GetValue(VariantProperty);

    /// <summary>Sets the flyout presentation variant.</summary>
    public static void SetVariant(AvaloniaObject target, FlyoutVariant value) =>
        target.SetValue(VariantProperty, value);

    /// <summary>Defines the optional rich tooltip subhead.</summary>
    public static readonly AttachedProperty<object?> SubheadProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>(
            "Subhead", typeof(FlyoutAssist), defaultValue: null);

    /// <summary>Gets the optional rich tooltip subhead.</summary>
    public static object? GetSubhead(AvaloniaObject target) => target.GetValue(SubheadProperty);

    /// <summary>Sets the optional rich tooltip subhead.</summary>
    public static void SetSubhead(AvaloniaObject target, object? value) => target.SetValue(SubheadProperty, value);

    /// <summary>Defines the data template for the subhead.</summary>
    public static readonly AttachedProperty<IDataTemplate?> SubheadTemplateProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IDataTemplate?>(
            "SubheadTemplate", typeof(FlyoutAssist), defaultValue: null);

    /// <summary>Gets the data template for the subhead.</summary>
    public static IDataTemplate? GetSubheadTemplate(AvaloniaObject target) => target.GetValue(SubheadTemplateProperty);

    /// <summary>Sets the data template for the subhead.</summary>
    public static void SetSubheadTemplate(AvaloniaObject target, IDataTemplate? value) =>
        target.SetValue(SubheadTemplateProperty, value);

    /// <summary>Defines the optional rich tooltip actions, using native buttons.</summary>
    public static readonly AttachedProperty<object?> ActionsProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>(
            "Actions", typeof(FlyoutAssist), defaultValue: null);

    /// <summary>Gets the optional rich tooltip actions, using native buttons.</summary>
    public static object? GetActions(AvaloniaObject target) => target.GetValue(ActionsProperty);

    /// <summary>Sets the optional rich tooltip actions, using native buttons.</summary>
    public static void SetActions(AvaloniaObject target, object? value) => target.SetValue(ActionsProperty, value);

    /// <summary>Defines the data template for the actions.</summary>
    public static readonly AttachedProperty<IDataTemplate?> ActionsTemplateProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IDataTemplate?>(
            "ActionsTemplate", typeof(FlyoutAssist), defaultValue: null);

    /// <summary>Gets the data template for the actions.</summary>
    public static IDataTemplate? GetActionsTemplate(AvaloniaObject target) => target.GetValue(ActionsTemplateProperty);

    /// <summary>Sets the data template for the actions.</summary>
    public static void SetActionsTemplate(AvaloniaObject target, IDataTemplate? value) =>
        target.SetValue(ActionsTemplateProperty, value);
}