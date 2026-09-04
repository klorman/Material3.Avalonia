using Avalonia;
using Avalonia.Data;
using Material3.Avalonia.Converters;
using Material3.Avalonia.Markup.Internal;
using Material3.Avalonia.Tokens.Internal;

namespace Material3.Avalonia.Markup;

/// <summary>
/// Creates a dynamically-updating <see cref="Thickness" /> from one, two, or four resource keys.
/// </summary>
public sealed class DynamicThicknessExtension
{
    private readonly List<object> _resourceKeys = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicThicknessExtension" /> class.
    /// </summary>
    public DynamicThicknessExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicThicknessExtension" /> class with a uniform resource key.
    /// </summary>
    public DynamicThicknessExtension(object uniform)
    {
        AddResourceKey(uniform);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicThicknessExtension" /> class with horizontal and vertical resource keys.
    /// </summary>
    public DynamicThicknessExtension(object horizontal, object vertical)
    {
        AddResourceKey(horizontal);
        AddResourceKey(vertical);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicThicknessExtension" /> class with side-specific resource keys.
    /// </summary>
    public DynamicThicknessExtension(object left, object top, object right, object bottom)
    {
        AddResourceKey(left);
        AddResourceKey(top);
        AddResourceKey(right);
        AddResourceKey(bottom);
    }

    /// <summary>
    /// Provides a binding that updates when any referenced resource changes.
    /// </summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (_resourceKeys.Count is not (1 or 2 or 4))
            throw new InvalidOperationException("DynamicThickness expects 1, 2, or 4 resource keys.");

        var binding = new MultiBinding
        {
            Converter = ThicknessConverter.Instance
        };

        foreach (var resourceKey in _resourceKeys)
            binding.Bindings.Add(TokenBindingFactory.Create(resourceKey, TokenValueKind.Numeric, serviceProvider));

        XamlBindingPriority.ApplyTemplatePriorityIfNeeded(binding, serviceProvider);

        return binding;
    }

    private void AddResourceKey(object resourceKey)
    {
        if (resourceKey is string text)
        {
            var keys = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in keys) _resourceKeys.Add(key);

            return;
        }

        _resourceKeys.Add(resourceKey);
    }
}