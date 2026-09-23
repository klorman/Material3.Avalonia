using Avalonia;
using Avalonia.Data;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Markup.Internal;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Markup;

/// <summary>Creates a dynamically-updating corner radius from a token and the target bounds.</summary>
public sealed class DynamicCornerRadiusExtension
{
    /// <summary>Initializes a new instance of the <see cref="DynamicCornerRadiusExtension" /> class.</summary>
    public DynamicCornerRadiusExtension()
    {
    }

    /// <summary>Initializes a new instance with a corner radius resource key.</summary>
    public DynamicCornerRadiusExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    /// <summary>Gets or sets the corner radius token resource key.</summary>
    public object? ResourceKey { get; set; }

    /// <summary>Provides a binding that updates when the token or bounds change.</summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (ResourceKey is null)
            throw new InvalidOperationException("DynamicCornerRadius requires a resource key.");
        var binding = new MultiBinding { Converter = DynamicCornerRadiusConverter.Instance };
        binding.Bindings.Add(TokenBindingFactory.Create(ResourceKey, TokenValueKind.Raw, serviceProvider));
        binding.Bindings.Add(BoundsBinding.Create());
        XamlBindingPriority.ApplyTemplatePriorityIfNeeded(binding, serviceProvider);
        return binding;
    }
}