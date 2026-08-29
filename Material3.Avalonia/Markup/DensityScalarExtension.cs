using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Density;

namespace Material3.Avalonia.Markup;

/// <summary>
/// Creates a density-aware scalar geometry binding.
/// </summary>
public sealed class DensityScalarExtension
{
    private MaterialDensity _mostDense;
    private bool _hasMostDense;

    /// <summary>
    /// Initializes a new instance of the <see cref="DensityScalarExtension" /> class.
    /// </summary>
    public DensityScalarExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DensityScalarExtension" /> class with a dynamic resource key.
    /// </summary>
    public DensityScalarExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>
    /// Gets or sets the base scalar geometry binding.
    /// </summary>
    public BindingBase? Base { get; set; }

    /// <summary>
    /// Gets or sets the dynamic resource key used as the base scalar geometry value.
    /// </summary>
    public object? ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets the multiplier applied to each density level delta.
    /// </summary>
    public double DeltaScale { get; set; } = 1d;

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
        if (!_hasMostDense) throw new InvalidOperationException("DensityScalar requires MostDense.");

        var binding = new MultiBinding
        {
            Converter = DensityScalarConverter.Instance,
            ConverterParameter = new DensityScalarOptions(_mostDense, DeltaScale)
        };

        binding.Bindings.Add(CreateBaseBinding());
        binding.Bindings.Add(DensityBinding.CreateDensityBinding());

        return binding;
    }

    private BindingBase CreateBaseBinding()
    {
        if (Base is not null && ResourceKey is not null)
            throw new InvalidOperationException("DensityScalar requires either ResourceKey or Base, not both.");

        if (Base is not null)
            return Base;

        if (ResourceKey is not null)
            return new DynamicResourceExtension(ResourceKey);

        throw new InvalidOperationException("DensityScalar requires a resource key or Base binding.");
    }
}
