using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Markup.Internal;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Markup;

/// <summary>Combines shadow geometry and a color while preserving each layer's opacity.</summary>
public sealed class DynamicBoxShadowExtension
{
    /// <summary>Initializes a new instance of the <see cref="DynamicBoxShadowExtension" /> class.</summary>
    public DynamicBoxShadowExtension()
    {
    }

    /// <summary>Initializes a new instance with shadow and color token keys or bindings.</summary>
    public DynamicBoxShadowExtension(object shadows, object color)
    {
        Shadows = shadows;
        Color = color;
    }

    /// <summary>Gets or sets the shadow geometry token key or explicit binding.</summary>
    public object? Shadows { get; set; }

    /// <summary>Gets or sets the shadow color token key or explicit binding.</summary>
    public object? Color { get; set; }

    /// <summary>Provides a binding that updates when either input changes.</summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Shadows is null || Color is null)
            throw new InvalidOperationException("DynamicBoxShadow requires Shadows and Color.");
        var binding = new MultiBinding { Converter = BoxShadowConverter.Instance };
        binding.Bindings.Add(CreateBinding(Shadows, serviceProvider));
        binding.Bindings.Add(CreateBinding(Color, serviceProvider));
        XamlBindingPriority.ApplyTemplatePriorityIfNeeded(binding, serviceProvider);
        return binding;
    }

    private static BindingBase CreateBinding(object value, IServiceProvider serviceProvider) =>
        value is BindingBase binding and not DynamicResourceExtension
            ? binding
            : TokenBindingFactory.Create(value, TokenValueKind.Raw, serviceProvider);
}