using Avalonia.Data;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Material3.Avalonia.Markup.Internal;

internal static class XamlBindingPriority
{
    public static void ApplyTemplatePriorityIfNeeded(MultiBinding binding, IServiceProvider? serviceProvider)
    {
        if (serviceProvider?.GetService(typeof(IAvaloniaXamlIlControlTemplateProvider)) is not null)
            binding.Priority = BindingPriority.Template;
    }
}