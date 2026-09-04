using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Bdziam.UI.Theming.MaterialColors.ColorSpace;
using Bdziam.UI.Theming.MaterialColors.DynamicColor;
using Bdziam.UI.Theming.MaterialColors.Scheme;
using Material3.Avalonia.Motion;
using Material3.Avalonia.Tokens.System;

namespace Material3.Avalonia.Theme;

public class MaterialTheme : Styles
{
    private const int RebuildResourceCapacity = 72;

    public static readonly StyledProperty<Color> SourceColorProperty =
        AvaloniaProperty.Register<MaterialTheme, Color>(nameof(SourceColor), MaterialThemeOptions.Defaults.SourceColor);

    public static readonly StyledProperty<DynamicSchemeVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTheme, DynamicSchemeVariant>(nameof(Variant),
            MaterialThemeOptions.Defaults.Variant);

    public static readonly StyledProperty<ThemeMode> ModeProperty =
        AvaloniaProperty.Register<MaterialTheme, ThemeMode>(nameof(Mode), MaterialThemeOptions.Defaults.Mode);

    public static readonly StyledProperty<Contrast> ContrastProperty =
        AvaloniaProperty.Register<MaterialTheme, Contrast>(nameof(Contrast), MaterialThemeOptions.Defaults.Contrast);

    public static readonly StyledProperty<MotionSchemeKind?> MotionSchemeProperty =
        AvaloniaProperty.Register<MaterialTheme, MotionSchemeKind?>(nameof(MotionScheme),
            MaterialThemeOptions.Defaults.MotionScheme);

    public Color SourceColor
    {
        get => GetValue(SourceColorProperty);
        set => SetValue(SourceColorProperty, value);
    }

    public DynamicSchemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ThemeMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public Contrast Contrast
    {
        get => GetValue(ContrastProperty);
        set => SetValue(ContrastProperty, value);
    }

    public MotionSchemeKind? MotionScheme
    {
        get => GetValue(MotionSchemeProperty);
        set => SetValue(MotionSchemeProperty, value);
    }

    public MaterialThemeOptions Options
    {
        get => new(SourceColor, Variant, Mode, Contrast, MotionScheme);
        set
        {
            SourceColor = value.SourceColor;
            Variant = value.Variant;
            Mode = value.Mode;
            Contrast = value.Contrast;
            MotionScheme = value.MotionScheme;
        }
    }

    private readonly IPlatformSettings? _platformSettings;
    private bool _isSubscribedToSystem;

    public MaterialTheme()
    {
        AvaloniaXamlLoader.Load(this);

        TryAdoptSystemAccent();

        OwnerChanged += (_, _) =>
        {
            UpdateSystemSubscription();
            ApplyMotionSettings();
        };

        _platformSettings = Application.Current?.PlatformSettings;

        UpdateSystemSubscription();
        Rebuild();
    }

    private void OnSystemColorValuesChanged(object? s, PlatformColorValues e)
    {
        if (Mode == ThemeMode.System)
            Rebuild();
    }

    private void TryAdoptSystemAccent()
    {
        try
        {
            var accentColor = Application.Current?.PlatformSettings?.GetColorValues().AccentColor1;
            if (accentColor is { } color)
                SetCurrentValue(SourceColorProperty, color);
        }
        catch
        {
            // ignored
        }
    }

    private void Rebuild()
    {
        var isDark = ResolveIsDark();
        var hct = Hct.FromInt(Options.SourceColor.ToUInt32());
        var scheme = DynamicSchemeMap.GetDynamicScheme(hct, isDark, Options.Contrast.Level, Options.Variant);

        var resources = new List<KeyValuePair<object, object?>>(RebuildResourceCapacity);
        ColorResourceWriter.AddResources(resources, scheme);
        ShadowResourceWriter.AddResources(resources, scheme);
        resources.Add(new KeyValuePair<object, object?>("Material.DynamicScheme", scheme));
        resources.Add(new KeyValuePair<object, object?>("Material.IsDark", isDark));
        resources.Add(new KeyValuePair<object, object?>("Material.ContrastLevel", Options.Contrast.Level));
        resources.Add(new KeyValuePair<object, object?>("Material.SourceColor", Options.SourceColor));
        resources.Add(new KeyValuePair<object, object?>("Material.SchemeVariant", Options.Variant));

        SetResourceItems(Resources, resources);
    }

    private static void SetResourceItems(
        IResourceDictionary target,
        IEnumerable<KeyValuePair<object, object?>> resources)
    {
        if (target is ResourceDictionary resourceDictionary)
        {
            resourceDictionary.SetItems(resources);
            return;
        }

        foreach (var (key, value) in resources)
            target[key] = value;
    }

    private bool ResolveIsDark()
    {
        return Mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            ThemeMode.System => GetSystemIsDark() ?? false,
            _ => false
        };
    }

    private bool? GetSystemIsDark()
    {
        try
        {
            var values = _platformSettings?.GetColorValues();
            return values?.ThemeVariant switch
            {
                PlatformThemeVariant.Dark => true,
                PlatformThemeVariant.Light => false,
                _ => null
            };
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private void UpdateSystemSubscription()
    {
        if (_platformSettings is null)
            return;

        var want = Owner is not null && Mode == ThemeMode.System;

        if (want && !_isSubscribedToSystem)
        {
            _platformSettings.ColorValuesChanged += OnSystemColorValuesChanged;
            _isSubscribedToSystem = true;
        }
        else if (!want && _isSubscribedToSystem)
        {
            _platformSettings.ColorValuesChanged -= OnSystemColorValuesChanged;
            _isSubscribedToSystem = false;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModeProperty)
            UpdateSystemSubscription();

        if (change.Property == SourceColorProperty
            || change.Property == VariantProperty
            || change.Property == ModeProperty
            || change.Property == ContrastProperty)
            Rebuild();

        if (change.Property == MotionSchemeProperty && change.NewValue != change.OldValue) ApplyMotionSettings();
    }

    private void ApplyMotionSettings()
    {
        if (MotionScheme is null)
            return;

        var motionScheme = MotionScheme switch
        {
            MotionSchemeKind.Standard => Motion.MotionScheme.Standard,
            MotionSchemeKind.Expressive => Motion.MotionScheme.Expressive,
            _ => MotionSettings.GlobalScheme
        };

        MotionSettings.GlobalScheme = motionScheme;

        Resources["Material.MotionSchemeKind"] = Options.MotionScheme;
    }
}