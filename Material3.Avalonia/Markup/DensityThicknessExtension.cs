using Avalonia.Data;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Density;
using Material3.Avalonia.Density.Internal;
using Material3.Avalonia.Markup.Internal;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Markup;

/// <summary>
/// Creates a density-aware thickness geometry binding.
/// </summary>
public sealed class DensityThicknessExtension
{
    private MaterialDensity _mostDense;
    private bool _hasMostDense;

    /// <summary>
    /// Initializes a new instance of the <see cref="DensityThicknessExtension" /> class.
    /// </summary>
    public DensityThicknessExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DensityThicknessExtension" /> class with a dynamic resource key.
    /// </summary>
    public DensityThicknessExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>
    /// Gets or sets the base thickness geometry binding.
    /// </summary>
    public BindingBase? Base { get; set; }

    /// <summary>
    /// Gets or sets the dynamic resource key used as the base thickness geometry value.
    /// </summary>
    public object? ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets which vertical sides receive the density adjustment.
    /// </summary>
    public DensitySides Sides { get; set; }

    /// <summary>
    /// Gets or sets the most dense level allowed by the target component geometry.
    /// </summary>
    public MaterialDensity MostDense
    {
        get => _mostDense;
        set
        {
            _mostDense = value;
            _hasMostDense = true;
        }
    }

    /// <summary>
    /// Provides a binding that updates when the base value or inherited density changes.
    /// </summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (!_hasMostDense) throw new InvalidOperationException("DensityThickness requires MostDense.");

        var binding = new MultiBinding
        {
            Converter = DensityThicknessConverter.Instance,
            ConverterParameter = new DensityThicknessOptions(Sides, _mostDense)
        };

        binding.Bindings.Add(CreateBaseBinding(serviceProvider));
        binding.Bindings.Add(DensityBinding.CreateDensityBinding());
        XamlBindingPriority.ApplyTemplatePriorityIfNeeded(binding, serviceProvider);

        return binding;
    }

    private BindingBase CreateBaseBinding(IServiceProvider serviceProvider)
    {
        if (Base is not null && ResourceKey is not null)
            throw new InvalidOperationException("DensityThickness requires either ResourceKey or Base, not both.");

        if (Base is not null)
            return Base;

        if (ResourceKey is not null)
            return TokenBindingFactory.Create(ResourceKey, TokenValueKind.Thickness, serviceProvider);

        throw new InvalidOperationException("DensityThickness requires a resource key or Base binding.");
    }
}