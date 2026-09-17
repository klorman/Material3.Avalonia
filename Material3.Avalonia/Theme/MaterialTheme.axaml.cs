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
using Material3.Avalonia.Symbols;
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

    /// <summary>Identifies the SymbolStyle property.</summary>
    public static readonly StyledProperty<SymbolStyle> SymbolStyleProperty =
        AvaloniaProperty.Register<MaterialTheme, SymbolStyle>(nameof(SymbolStyle), SymbolStyle.Outlined);

    /// <summary>Identifies the SymbolWeight property.</summary>
    public static readonly StyledProperty<double> SymbolWeightProperty =
        AvaloniaProperty.Register<MaterialTheme, double>(nameof(SymbolWeight), 400,
            validate: value => double.IsFinite(value) && value is >= 100 and <= 700);

    /// <summary>Identifies the SymbolGrade property.</summary>
    public static readonly StyledProperty<double> SymbolGradeProperty =
        AvaloniaProperty.Register<MaterialTheme, double>(nameof(SymbolGrade), 0,
            validate: value => double.IsFinite(value) && value is >= -50 and <= 200);

    /// <summary>Identifies the SymbolIsFilled property.</summary>
    public static readonly StyledProperty<bool> SymbolIsFilledProperty =
        AvaloniaProperty.Register<MaterialTheme, bool>(nameof(SymbolIsFilled), false);

    /// <summary>Identifies the SymbolOpticalSize property.</summary>
    public static readonly StyledProperty<double?> SymbolOpticalSizeProperty =
        AvaloniaProperty.Register<MaterialTheme, double?>(nameof(SymbolOpticalSize), null,
            validate: value => value is null || double.IsFinite(value.Value) && value is >= 20 and <= 48);

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

    /// <summary>The default Material Symbols design family.</summary>
    public SymbolStyle SymbolStyle
    {
        get => GetValue(SymbolStyleProperty);
        set => SetValue(SymbolStyleProperty, value);
    }

    /// <summary>The default Material Symbols weight from 100 to 700.</summary>
    public double SymbolWeight
    {
        get => GetValue(SymbolWeightProperty);
        set => SetValue(SymbolWeightProperty, value);
    }

    /// <summary>The default Material Symbols grade from -50 to 200.</summary>
    public double SymbolGrade
    {
        get => GetValue(SymbolGradeProperty);
        set => SetValue(SymbolGradeProperty, value);
    }

    /// <summary>Whether Material Symbols are filled by default.</summary>
    public bool SymbolIsFilled
    {
        get => GetValue(SymbolIsFilledProperty);
        set => SetValue(SymbolIsFilledProperty, value);
    }

    /// <summary>The default Material Symbols optical size, or null to follow each icon size.</summary>
    public double? SymbolOpticalSize
    {
        get => GetValue(SymbolOpticalSizeProperty);
        set => SetValue(SymbolOpticalSizeProperty, value);
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
        resources.Add(new KeyValuePair<object, object?>("Material.SymbolStyle", SymbolStyle));
        resources.Add(new KeyValuePair<object, object?>("Material.SymbolWeight", SymbolWeight));
        resources.Add(new KeyValuePair<object, object?>("Material.SymbolGrade", SymbolGrade));
        resources.Add(new KeyValuePair<object, object?>("Material.SymbolIsFilled", SymbolIsFilled));
        resources.Add(new KeyValuePair<object, object?>("Material.SymbolOpticalSize", SymbolOpticalSize));

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
            || change.Property == ContrastProperty
            || change.Property == SymbolStyleProperty
            || change.Property == SymbolWeightProperty
            || change.Property == SymbolGradeProperty
            || change.Property == SymbolIsFilledProperty
            || change.Property == SymbolOpticalSizeProperty)
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