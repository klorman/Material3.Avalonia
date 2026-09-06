using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material3.Avalonia.Attached.Controls.Internal;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Defines automatic tooltip activation sources.</summary>
[Flags]
public enum ToolTipTriggers
{
    /// <summary>Only explicit opening is enabled.</summary>
    None = 0,

    /// <summary>A hovering pointer opens the tooltip.</summary>
    Hover = 1,

    /// <summary>Keyboard navigation opens the tooltip.</summary>
    Focus = 2,

    /// <summary>A native holding gesture opens the tooltip.</summary>
    LongPress = 4
}

/// <summary>Provides Material interaction for native tooltips and rich tooltip flyouts.</summary>
public static class ToolTipAssist
{
    private static readonly ConditionalWeakTable<Control, ToolTipOwner> Owners = new();

    /// <summary>Defines whether Material manages tooltip interaction in this scope.</summary>
    public static readonly AttachedProperty<bool> IsMaterialBehaviorEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsMaterialBehaviorEnabled", typeof(ToolTipAssist), inherits: true);

    /// <summary>Defines the delay in milliseconds after leaving a transient tooltip.</summary>
    public static readonly AttachedProperty<int> HideDelayProperty =
        AvaloniaProperty.RegisterAttached<Control, int>(
            "HideDelay", typeof(ToolTipAssist), defaultValue: 1500, inherits: true,
            validate: value => value >= 0);

    /// <summary>Defines which input sources automatically open transient tooltips.</summary>
    public static readonly AttachedProperty<ToolTipTriggers> ShowTriggersProperty =
        AvaloniaProperty.RegisterAttached<Control, ToolTipTriggers>(
            "ShowTriggers", typeof(ToolTipAssist),
            ToolTipTriggers.Hover | ToolTipTriggers.Focus | ToolTipTriggers.LongPress, inherits: true);

    /// <summary>Defines whether a pointer-triggered tooltip retains its anchor's hover appearance.</summary>
    public static readonly AttachedProperty<bool> KeepAnchorHoveredProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "KeepAnchorHovered", typeof(ToolTipAssist), true, inherits: true);

    /// <summary>Gets the automatic tooltip activation sources.</summary>
    public static ToolTipTriggers GetShowTriggers(Control control) => control.GetValue(ShowTriggersProperty);

    /// <summary>Sets the automatic tooltip activation sources.</summary>
    public static void SetShowTriggers(Control control, ToolTipTriggers value) =>
        control.SetValue(ShowTriggersProperty, value);

    /// <summary>Gets whether a tooltip retains its anchor's hover appearance.</summary>
    public static bool GetKeepAnchorHovered(Control control) => control.GetValue(KeepAnchorHoveredProperty);

    /// <summary>Sets whether a tooltip retains its anchor's hover appearance.</summary>
    public static void SetKeepAnchorHovered(Control control, bool value) =>
        control.SetValue(KeepAnchorHoveredProperty, value);

    static ToolTipAssist()
    {
        IsMaterialBehaviorEnabledProperty.Changed.AddClassHandler<Control>((control, _) => Refresh(control));
        ToolTip.TipProperty.Changed.AddClassHandler<Control>((control, _) => Refresh(control));
        Button.FlyoutProperty.Changed.AddClassHandler<Control>((control, _) => Refresh(control));
        FlyoutBase.AttachedFlyoutProperty.Changed.AddClassHandler<Control>((control, _) => Refresh(control));
    }

    /// <summary>Gets whether Material manages tooltip interaction in this scope.</summary>
    public static bool GetIsMaterialBehaviorEnabled(Control control) =>
        control.GetValue(IsMaterialBehaviorEnabledProperty);

    /// <summary>Sets whether Material manages tooltip interaction in this scope.</summary>
    public static void SetIsMaterialBehaviorEnabled(Control control, bool value) =>
        control.SetValue(IsMaterialBehaviorEnabledProperty, value);

    /// <summary>Gets the transient tooltip hide delay in milliseconds.</summary>
    public static int GetHideDelay(Control control) => control.GetValue(HideDelayProperty);

    /// <summary>Sets the transient tooltip hide delay in milliseconds.</summary>
    public static void SetHideDelay(Control control, int value) => control.SetValue(HideDelayProperty, value);

    internal static void Refresh(Control control)
    {
        if (Owners.TryGetValue(control, out var owner))
            owner.Refresh();
        else if (GetIsMaterialBehaviorEnabled(control) &&
                 (ToolTip.GetTip(control) is not null || GetFlyout(control) is not null))
        {
            owner = new ToolTipOwner(control);
            Owners.Add(control, owner);
            owner.Refresh();
        }
    }

    internal static Flyout? GetFlyout(Control control) =>
        (control as Button)?.Flyout as Flyout ?? FlyoutBase.GetAttachedFlyout(control) as Flyout;

    internal static ToolTipOwner? GetOwner(Control control, bool create = false)
    {
        Refresh(control);
        if (create && GetIsMaterialBehaviorEnabled(control) && !Owners.TryGetValue(control, out _))
            Owners.Add(control, new ToolTipOwner(control));
        return Owners.TryGetValue(control, out var owner) ? owner : null;
    }
}