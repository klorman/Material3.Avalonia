using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Input;
using Material3.Avalonia.Controls.Primitives;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Attached.Controls.Internal;

// Bind only the current state's tokens: instantiating every inactive style's MultiBindings
// makes attaching a large popup expensive. The shared resolver still owns resource semantics.
internal sealed class MenuItemColors : IDisposable
{
    private readonly MenuItem _item;
    private readonly List<TokenSlot> _slots = [];
    private readonly TokenSlot _background;
    private readonly TokenSlot _label;
    private readonly TokenSlot _labelOpacity;
    private readonly TokenSlot _supporting;
    private readonly TokenSlot _supportingOpacity;
    private readonly TokenSlot _trailing;
    private readonly TokenSlot _trailingOpacity;
    private readonly TokenSlot[] _leadingIcons;
    private readonly TokenSlot[] _leadingOpacities;
    private readonly TokenSlot[] _trailingIcons;
    private readonly TokenSlot[] _trailingOpacities;
    private readonly TokenSlot _state;
    private readonly TokenSlot _stateOpacity;
    private readonly TokenSlot _ripple;
    private readonly TokenSlot _rippleOpacity;

    internal MenuItemColors(MenuItem item, INameScope scope)
    {
        _item = item;
        _background = Slot(item, TemplatedControl.BackgroundProperty);
        _label = Slot(item, TemplatedControl.ForegroundProperty);
        _labelOpacity = Slot(scope.Find<Control>("PART_HeaderPresenter"), Visual.OpacityProperty);
        _supporting = Slot(scope.Find<Control>("PART_SupportingText"), TemplatedControl.ForegroundProperty);
        _supportingOpacity = Slot(scope.Find<Control>("PART_SupportingText"), Visual.OpacityProperty);
        _trailing = Slot(scope.Find<Control>("PART_TrailingText"), TextBlock.ForegroundProperty);
        _trailingOpacity = Slot(scope.Find<Control>("PART_TrailingText"), Visual.OpacityProperty);
        _leadingIcons =
        [
            Slot(scope.Find<Control>("PART_Icon"), TemplatedControl.ForegroundProperty),
            Slot(scope.Find<Control>("PART_Check"), TemplatedControl.ForegroundProperty)
        ];
        _leadingOpacities =
        [
            Slot(scope.Find<Control>("PART_Icon"), Visual.OpacityProperty),
            Slot(scope.Find<Control>("PART_Check"), Visual.OpacityProperty)
        ];
        _trailingIcons =
        [
            Slot(scope.Find<Control>("PART_TrailingIcon"), TemplatedControl.ForegroundProperty),
            Slot(scope.Find<Control>("PART_Chevron"), TemplatedControl.ForegroundProperty),
            Slot(scope.Find<Control>("PART_TrailingCheck"), TemplatedControl.ForegroundProperty)
        ];
        _trailingOpacities =
        [
            Slot(scope.Find<Control>("PART_TrailingIcon"), Visual.OpacityProperty),
            Slot(scope.Find<Control>("PART_Chevron"), Visual.OpacityProperty),
            Slot(scope.Find<Control>("PART_TrailingCheck"), Visual.OpacityProperty)
        ];
        _state = Slot(scope.Find<Control>("PART_StateLayer"), StateLayer.BrushProperty);
        _stateOpacity = Slot(scope.Find<Control>("PART_StateLayer"), Visual.OpacityProperty);
        _ripple = Slot(scope.Find<Control>("PART_Ripple"), InkRipple.BrushProperty);
        _rippleOpacity = Slot(scope.Find<Control>("PART_Ripple"), InkRipple.BaseOpacityProperty);
        item.Classes.CollectionChanged += OnClassesChanged;
        item.PropertyChanged += OnPropertyChanged;
        Update();
    }

    private TokenSlot Slot(AvaloniaObject? target, AvaloniaProperty property)
    {
        var slot = new TokenSlot(target, property);
        _slots.Add(slot);
        return slot;
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Update();

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MenuAssist.ColorStyleProperty || e.Property == MenuItemPresentation.DisplayCheckedProperty ||
            e.Property == InputElement.IsEffectivelyEnabledProperty || e.Property == MenuItem.IconProperty ||
            e.Property == MenuItem.ToggleTypeProperty || e.Property == ItemsControl.ItemCountProperty ||
            e.Property == MenuItem.InputGestureProperty || e.Property == MenuAssist.SupportingTextProperty ||
            e.Property == MenuAssist.TrailingTextProperty || e.Property == MenuAssist.TrailingIconProperty ||
            e.Property == MenuAssist.IsAnimationEnabledProperty) Update();
    }

    private void Update()
    {
        var selected = MenuItemPresentation.GetDisplayChecked(_item);
        var disabled = !_item.IsEffectivelyEnabled;
        var colorStyle = MenuAssist.GetColorStyle(_item);
        var prefix = "MdCompMenus" + colorStyle + "Item";
        var selection = selected ? "Selected" : "";
        var state = disabled ? "Disabled" :
            _item.Classes.Contains("m3-menu-pressed") ? "Pressed" :
            _item.Classes.Contains("m3-menu-keyboard-focus") ? "Focus" :
            _item.Classes.Contains("m3-hovered") ? "Hover" : "";
        var textState = selected && disabled ? "" : state;
        _background.Bind(prefix + selection + "ContainerBrush");
        _label.Bind(prefix + selection + textState + "LabelTextBrush");
        var detailState = disabled ? selected ? "" : "Disabled" :
            (colorStyle == MenuColorStyle.Standard) == selected ? state : "";
        var supporting = MenuAssist.GetSupportingText(_item) is not null;
        var trailing = MenuAssist.GetTrailingText(_item) is { } text ? text.Length > 0 : _item.InputGesture is not null;
        _supporting.Bind(prefix + selection + detailState + "SupportingTextBrush", enabled: supporting);
        _trailing.Bind(prefix + selection + detailState + "TrailingSupportingTextBrush", enabled: trailing);
        var iconState = selected && (colorStyle == MenuColorStyle.Vibrant || disabled) ? "" : state;
        for (var i = 0; i < 2; i++)
        {
            var leading = i == 0 ? _item.Icon is not null : selected || _item.ToggleType != MenuItemToggleType.None;
            var trailingIcon = i == 0 ? MenuAssist.GetTrailingIcon(_item) is not null : _item.HasSubMenu;
            _leadingIcons[i].Bind(prefix + selection + iconState + "LeadingIconBrush", enabled: leading);
            _trailingIcons[i].Bind(prefix + selection + iconState + "TrailingIconBrush", enabled: trailingIcon);
            _leadingOpacities[i].Bind(disabled ? prefix + selection + "DisabledLeadingIconOpacity" : null, 1d, leading);
            _trailingOpacities[i].Bind(disabled ? prefix + selection + "DisabledTrailingIconOpacity" : null, 1d,
                trailingIcon);
        }

        _trailingIcons[2].Bind(prefix + selection + iconState + "TrailingIconBrush");
        _trailingOpacities[2].Bind(disabled ? prefix + selection + "DisabledTrailingIconOpacity" : null, 1d);

        _labelOpacity.Bind(disabled ? prefix + selection + "DisabledLabelTextOpacity" : null, 1d);
        var detailOpacityPrefix = prefix + (colorStyle == MenuColorStyle.Vibrant ? selection : "");
        _supportingOpacity.Bind(disabled ? detailOpacityPrefix + "DisabledSupportingTextOpacity" : null, 1d,
            supporting);
        _trailingOpacity.Bind(disabled ? detailOpacityPrefix + "DisabledTrailingSupportingTextOpacity" : null, 1d,
            trailing);
        var animated = MenuAssist.GetIsAnimationEnabled(_item);
        _ripple.Bind(prefix + selection + "PressedStateLayerBrush", enabled: animated);
        _rippleOpacity.Bind(prefix + selection + "PressedStateLayerOpacity", enabled: animated);
        var layer = state.Length > 0 ? state : _item.IsSubMenuOpen ? "Active" : "";
        var brushState = layer == "Active" && selected ? "Hover" : layer;
        var opacityPrefix = prefix + (layer == "Active" ? "" : selection);
        _state.Bind(!disabled && layer.Length > 0 ? prefix + selection + brushState + "StateLayerBrush" : null,
            Brushes.Transparent);
        _stateOpacity.Bind(!disabled && layer.Length > 0 ? opacityPrefix + layer + "StateLayerOpacity" : null, 0d);
    }

    public void Dispose()
    {
        _item.Classes.CollectionChanged -= OnClassesChanged;
        _item.PropertyChanged -= OnPropertyChanged;
        foreach (var slot in _slots) slot.Dispose();
    }

    private sealed class TokenSlot(AvaloniaObject? target, AvaloniaProperty property) : IDisposable
    {
        private string? _key;
        private bool _initialized;
        private IDisposable? _binding;

        internal void Bind(string? key, object? fallback = null, bool enabled = true)
        {
            if (!enabled)
            {
                _binding?.Dispose();
                _binding = null;
                _initialized = false;
                return;
            }

            if (target is null || _initialized && _key == key) return;
            _initialized = true;
            _key = key;
            var previousBinding = _binding;
            if (key is null)
                _binding = target.SetValue(property, fallback, BindingPriority.Style);
            else
            {
                var binding = TokenBindingFactory.Create(key);
                ((MultiBinding)binding).Priority = BindingPriority.Style;
                _binding = target.Bind(property, binding);
            }

            // Keep the old value until its replacement is installed: an intermediate default
            // would become the starting value of an interrupted state transition.
            previousBinding?.Dispose();
        }

        public void Dispose() => _binding?.Dispose();
    }
}