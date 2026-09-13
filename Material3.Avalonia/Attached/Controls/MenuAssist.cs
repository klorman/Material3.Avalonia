using Avalonia;
using Avalonia.Controls;
using Material3.Avalonia.Attached.Controls.Internal;

namespace Material3.Avalonia.Attached.Controls;

/// <summary>Defines the Material menu color mapping.</summary>
public enum MenuColorStyle
{
    /// <summary>Uses surface colors with tertiary selection.</summary>
    Standard,

    /// <summary>Uses tertiary colors for greater emphasis.</summary>
    Vibrant
}

/// <summary>Defines the position of the selected menu item's checkmark.</summary>
public enum MenuCheckmarkPlacement
{
    /// <summary>Replaces the leading icon when checked.</summary>
    Leading,

    /// <summary>Replaces the trailing icon when checked.</summary>
    Trailing,

    /// <summary>Shows selection without a checkmark.</summary>
    None
}

/// <summary>Provides Material presentation settings for native Avalonia menus.</summary>
public static class MenuAssist
{
    static MenuAssist()
    {
        CheckmarkPlacementProperty.Changed.AddClassHandler<AvaloniaObject>((target, _) => Register(target));
        ColorStyleProperty.Changed.AddClassHandler<AvaloniaObject>((target, _) => Register(target));
        IsAnimationEnabledProperty.Changed.AddClassHandler<AvaloniaObject>((target, _) => Register(target));
    }

    private static void Register(AvaloniaObject target)
    {
        if (target is MenuFlyout flyout)
            MenuFlyoutRegistration.Get(flyout);
    }

    /// <summary>Defines the menu color mapping.</summary>
    public static readonly AttachedProperty<MenuColorStyle> ColorStyleProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, MenuColorStyle>("ColorStyle", typeof(MenuAssist),
            MenuColorStyle.Standard, inherits: true);

    /// <summary>Gets the menu color mapping.</summary>
    public static MenuColorStyle GetColorStyle(AvaloniaObject target) => target.GetValue(ColorStyleProperty);

    /// <summary>Sets the menu color mapping.</summary>
    public static void SetColorStyle(AvaloniaObject target, MenuColorStyle value) =>
        target.SetValue(ColorStyleProperty, value);

    /// <summary>Defines the position of the selected item's checkmark.</summary>
    public static readonly AttachedProperty<MenuCheckmarkPlacement> CheckmarkPlacementProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, MenuCheckmarkPlacement>("CheckmarkPlacement",
            typeof(MenuAssist),
            MenuCheckmarkPlacement.Leading, inherits: true);

    /// <summary>Gets the checkmark placement.</summary>
    public static MenuCheckmarkPlacement GetCheckmarkPlacement(AvaloniaObject target) =>
        target.GetValue(CheckmarkPlacementProperty);

    /// <summary>Sets the checkmark placement.</summary>
    public static void SetCheckmarkPlacement(AvaloniaObject target, MenuCheckmarkPlacement value) =>
        target.SetValue(CheckmarkPlacementProperty, value);

    /// <summary>Defines the supporting text below the item label.</summary>
    public static readonly AttachedProperty<string?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("SupportingText", typeof(MenuAssist), null,
            inherits: false);

    /// <summary>Gets the supporting text below the item label.</summary>
    public static string? GetSupportingText(AvaloniaObject target) => target.GetValue(SupportingTextProperty);

    /// <summary>Sets the supporting text below the item label.</summary>
    public static void SetSupportingText(AvaloniaObject target, string? value) =>
        target.SetValue(SupportingTextProperty, value);

    /// <summary>Defines the trailing text, replacing the displayed input gesture.</summary>
    public static readonly AttachedProperty<string?> TrailingTextProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("TrailingText", typeof(MenuAssist), null,
            inherits: false);

    /// <summary>Gets the trailing text, replacing the displayed input gesture.</summary>
    public static string? GetTrailingText(AvaloniaObject target) => target.GetValue(TrailingTextProperty);

    /// <summary>Sets the trailing text, replacing the displayed input gesture.</summary>
    public static void SetTrailingText(AvaloniaObject target, string? value) =>
        target.SetValue(TrailingTextProperty, value);

    /// <summary>Defines the decorative trailing icon.</summary>
    public static readonly AttachedProperty<object?> TrailingIconProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("TrailingIcon", typeof(MenuAssist), null,
            inherits: false);

    /// <summary>Gets the decorative trailing icon.</summary>
    public static object? GetTrailingIcon(AvaloniaObject target) => target.GetValue(TrailingIconProperty);

    /// <summary>Sets the decorative trailing icon.</summary>
    public static void SetTrailingIcon(AvaloniaObject target, object? value) =>
        target.SetValue(TrailingIconProperty, value);

    /// <summary>Defines the decorative badge content.</summary>
    public static readonly AttachedProperty<object?> BadgeProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("Badge", typeof(MenuAssist), null, inherits: false);

    /// <summary>Gets the decorative badge content.</summary>
    public static object? GetBadge(AvaloniaObject target) => target.GetValue(BadgeProperty);

    /// <summary>Sets the decorative badge content.</summary>
    public static void SetBadge(AvaloniaObject target, object? value) => target.SetValue(BadgeProperty, value);

    /// <summary>Defines whether menu animations are enabled.</summary>
    public static readonly AttachedProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsAnimationEnabled", typeof(MenuAssist), true,
            inherits: true);

    /// <summary>Gets whether menu animations are enabled.</summary>
    public static bool GetIsAnimationEnabled(AvaloniaObject target) => target.GetValue(IsAnimationEnabledProperty);

    /// <summary>Sets whether menu animations are enabled.</summary>
    public static void SetIsAnimationEnabled(AvaloniaObject target, bool value) =>
        target.SetValue(IsAnimationEnabledProperty, value);
}