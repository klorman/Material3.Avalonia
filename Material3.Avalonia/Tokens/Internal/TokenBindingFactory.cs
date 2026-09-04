using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Material3.Avalonia.Markup.Internal;

namespace Material3.Avalonia.Tokens.Internal;

internal static class TokenBindingFactory
{
    public static BindingBase Create(
        object resourceKey,
        TokenValueKind valueKind = TokenValueKind.TargetType,
        IServiceProvider? serviceProvider = null)
    {
        resourceKey = NormalizeResourceKey(resourceKey);

        var binding = new MultiBinding
        {
            Converter = valueKind switch
            {
                TokenValueKind.Numeric => TokenResolverConverter.Numeric,
                TokenValueKind.Thickness => TokenResolverConverter.Thickness,
                _ => TokenResolverConverter.TargetType
            },
            ConverterParameter = resourceKey
        };

        binding.Bindings.Add(CreateDynamicResourceBinding(resourceKey, serviceProvider));
        binding.Bindings.Add(TokenTargetBinding.CreateResolutionTargetBinding());
        XamlBindingPriority.ApplyTemplatePriorityIfNeeded(binding, serviceProvider);

        return binding;
    }

    private static BindingBase CreateDynamicResourceBinding(object resourceKey, IServiceProvider? serviceProvider)
    {
        var extension = new DynamicResourceExtension(resourceKey);
        return serviceProvider is null ? extension : extension.ProvideValue(serviceProvider);
    }

    private static object NormalizeResourceKey(object resourceKey)
    {
        if (resourceKey is not DynamicResourceExtension dynamicResource)
            return resourceKey;

        return dynamicResource.ResourceKey
               ?? throw new InvalidOperationException("DynamicResource must specify a resource key.");
    }
}

internal enum TokenValueKind
{
    TargetType,
    Numeric,
    Thickness
}