using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3.Avalonia.Tokens;

namespace Material3.Avalonia.Tokens.Internal;

internal static class TokenResolver
{
    public static object? Resolve(
        object rootKey,
        object? rootValue,
        object? target,
        object? templatedParent,
        Type targetType,
        TokenValueKind valueKind = TokenValueKind.TargetType)
    {
        if (IsUnset(rootValue))
        {
            var targetHost = target as IResourceHost;
            var templatedParentHost = IsUnset(templatedParent) ? null : templatedParent as IResourceHost;
            if (!TryFindResource(rootKey, targetHost, templatedParentHost, out rootValue))
            {
                if (IsTransientlyDetached(target, templatedParent))
                    return BindingOperations.DoNothing;

                throw MissingResource(rootKey, GetPath(null, rootKey));
            }

            return rootValue is TokenAlias alias
                ? ResolveAlias(rootKey, alias, target, templatedParent, targetHost, templatedParentHost, targetType,
                    valueKind)
                : ValidateType(rootValue, rootKey, null, targetType, valueKind);
        }

        if (rootValue is not TokenAlias rootAlias)
            return ValidateType(rootValue, rootKey, null, targetType, valueKind);

        var resolvedTargetHost = target as IResourceHost;
        var resolvedTemplatedParentHost = IsUnset(templatedParent) ? null : templatedParent as IResourceHost;

        if (resolvedTargetHost is null && resolvedTemplatedParentHost is null)
        {
            if (IsTransientlyDetached(target, templatedParent))
                return BindingOperations.DoNothing;

            var path = GetPath(null, rootKey);
            throw new TokenResolutionException(
                $"Token '{FormatPath(path)}' resolved to an alias, but no Avalonia resource host is available.",
                path);
        }

        return ResolveAlias(
            rootKey,
            rootAlias,
            target,
            templatedParent,
            resolvedTargetHost,
            resolvedTemplatedParentHost,
            targetType,
            valueKind);
    }

    private static object? ResolveAlias(
        object rootKey,
        TokenAlias rootAlias,
        object? target,
        object? templatedParent,
        IResourceHost? targetHost,
        IResourceHost? templatedParentHost,
        Type targetType,
        TokenValueKind valueKind)
    {
        var path = new AliasPath(rootKey);
        object? value = rootAlias;
        while (value is TokenAlias alias)
        {
            if (alias.ResourceKey is not { } nextKey)
                throw NullAliasResource(path);

            if (path.Contains(nextKey))
            {
                path.Add(nextKey);
                throw AliasCycle(path);
            }

            path.Add(nextKey);

            if (!TryFindResource(nextKey, targetHost, templatedParentHost, out value) || IsUnset(value))
            {
                if (IsTransientlyDetached(target, templatedParent))
                    return BindingOperations.DoNothing;

                throw MissingResource(nextKey, path.Materialize());
            }
        }

        return ValidateType(value, path, targetType, valueKind);
    }

    private static bool TryFindResource(
        object resourceKey,
        IResourceHost? targetHost,
        IResourceHost? templatedParentHost,
        out object? value)
    {
        if (TryFindResource(targetHost, resourceKey, out value))
            return true;

        if (!ReferenceEquals(templatedParentHost, targetHost)
            && TryFindResource(templatedParentHost, resourceKey, out value))
            return true;

        value = null;
        return false;
    }

    private static bool TryFindResource(IResourceHost? resourceHost, object resourceKey, out object? value)
    {
        if (resourceHost is not null
            && resourceHost.TryFindResource(resourceKey, GetThemeVariant(resourceHost), out value))
            return true;

        value = null;
        return false;
    }

    private static bool IsTransientlyDetached(object? target, object? templatedParent)
    {
        return IsDetached(target) || IsDetached(templatedParent);
    }

    private static bool IsDetached(object? value)
    {
        if (value is null || IsUnset(value))
            return false;

        if (value is ILogical logical)
            return !logical.IsAttachedToLogicalTree;

        return value is Visual visual && !visual.IsAttachedToVisualTree();
    }

    private static ThemeVariant? GetThemeVariant(IResourceHost resourceHost)
    {
        return resourceHost is StyledElement element ? element.ActualThemeVariant : null;
    }

    private static object? ValidateType(
        object? value,
        object rootKey,
        IReadOnlyList<object>? path,
        Type targetType,
        TokenValueKind valueKind)
    {
        return valueKind switch
        {
            TokenValueKind.Numeric => ValidateNumeric(value, rootKey, path),
            TokenValueKind.Thickness => ValidateAssignable(value, rootKey, path, typeof(Thickness), false),
            _ => ValidateAssignable(value, rootKey, path, targetType, true)
        };
    }

    private static object? ValidateType(
        object? value,
        AliasPath path,
        Type targetType,
        TokenValueKind valueKind)
    {
        return valueKind switch
        {
            TokenValueKind.Numeric => ValidateNumeric(value, path),
            TokenValueKind.Thickness => ValidateAssignable(value, path, typeof(Thickness), false),
            _ => ValidateAssignable(value, path, targetType, true)
        };
    }

    private static object? ValidateAssignable(
        object? value,
        object rootKey,
        IReadOnlyList<object>? path,
        Type targetType,
        bool allowNull)
    {
        if ((value is null && allowNull) || targetType == typeof(object))
            return value;

        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is not null && effectiveTargetType.IsInstanceOfType(value))
            return value;

        var diagnosticPath = GetPath(path, rootKey);
        throw new TokenResolutionException(
            value is null
                ? $"Token '{FormatPath(diagnosticPath)}' resolved to null, which cannot be assigned to '{targetType.FullName}'."
                : $"Token '{FormatPath(diagnosticPath)}' resolved to '{value.GetType().FullName}', which cannot be assigned to '{targetType.FullName}'.",
            diagnosticPath);
    }

    private static object? ValidateAssignable(
        object? value,
        AliasPath path,
        Type targetType,
        bool allowNull)
    {
        if ((value is null && allowNull) || targetType == typeof(object))
            return value;

        var effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is not null && effectiveTargetType.IsInstanceOfType(value))
            return value;

        var diagnosticPath = path.Materialize();
        throw new TokenResolutionException(
            value is null
                ? $"Token '{FormatPath(diagnosticPath)}' resolved to null, which cannot be assigned to '{targetType.FullName}'."
                : $"Token '{FormatPath(diagnosticPath)}' resolved to '{value.GetType().FullName}', which cannot be assigned to '{targetType.FullName}'.",
            diagnosticPath);
    }

    private static object? ValidateNumeric(object? value, object rootKey, IReadOnlyList<object>? path)
    {
        var result = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            null => throw NumericError(
                rootKey,
                path,
                "resolved to null, which cannot be assigned to a numeric value."),
            _ => throw NumericError(
                rootKey,
                path,
                $"resolved to '{value.GetType().FullName}', which cannot be assigned to a numeric value.")
        };

        if (!double.IsFinite(result))
            throw NumericError(rootKey, path, "resolved to a non-finite numeric value.");

        return value;
    }

    private static object? ValidateNumeric(object? value, AliasPath path)
    {
        var result = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            null => throw NumericError(
                path,
                "resolved to null, which cannot be assigned to a numeric value."),
            _ => throw NumericError(
                path,
                $"resolved to '{value.GetType().FullName}', which cannot be assigned to a numeric value.")
        };

        if (!double.IsFinite(result))
            throw NumericError(path, "resolved to a non-finite numeric value.");

        return value;
    }

    private static TokenResolutionException NumericError(
        object rootKey,
        IReadOnlyList<object>? path,
        string reason)
    {
        var diagnosticPath = GetPath(path, rootKey);
        return new TokenResolutionException($"Token '{FormatPath(diagnosticPath)}' {reason}", diagnosticPath);
    }

    private static TokenResolutionException NumericError(AliasPath path, string reason)
    {
        var diagnosticPath = path.Materialize();
        return new TokenResolutionException($"Token '{FormatPath(diagnosticPath)}' {reason}", diagnosticPath);
    }

    private static TokenResolutionException NullAliasResource(AliasPath path)
    {
        var diagnosticPath = path.Materialize();
        return new TokenResolutionException($"Token '{FormatPath(diagnosticPath)}' aliases a null resource key.",
            diagnosticPath);
    }

    private static TokenResolutionException AliasCycle(AliasPath path)
    {
        var diagnosticPath = path.Materialize();
        return new TokenResolutionException($"Token alias cycle detected: {FormatPath(diagnosticPath)}.",
            diagnosticPath);
    }

    private static TokenResolutionException MissingResource(object resourceKey, IReadOnlyList<object> path)
    {
        return new TokenResolutionException(
            $"Token '{FormatPath(path)}' could not be resolved. Resource '{resourceKey}' was not found.",
            path);
    }

    private static bool IsUnset(object? value)
    {
        return value is UnsetValueType;
    }

    private static IReadOnlyList<object> GetPath(IReadOnlyList<object>? path, object rootKey)
    {
        return path ?? new[] { rootKey };
    }

    private static string FormatPath(IEnumerable<object> path)
    {
        return string.Join(" -> ", path);
    }

    private struct AliasPath
    {
        private const int InlineCapacity = 4;

        private object? _item0;
        private object? _item1;
        private object? _item2;
        private object? _item3;
        private List<object>? _items;

        public AliasPath(object rootKey)
        {
            _item0 = rootKey;
            _item1 = null;
            _item2 = null;
            _item3 = null;
            _items = null;
            Count = 1;
        }

        private int Count { get; set; }

        public void Add(object item)
        {
            if (_items is not null)
            {
                _items.Add(item);
                Count++;
                return;
            }

            switch (Count)
            {
                case 0:
                    _item0 = item;
                    break;
                case 1:
                    _item1 = item;
                    break;
                case 2:
                    _item2 = item;
                    break;
                case 3:
                    _item3 = item;
                    break;
                default:
                    _items = new List<object>(InlineCapacity + 1) { _item0!, _item1!, _item2!, _item3!, item };
                    break;
            }

            Count++;
        }

        public bool Contains(object item)
        {
            if (_items is not null)
                return _items.Contains(item);

            return Count switch
            {
                >= 1 when Equals(_item0, item) => true,
                >= 2 when Equals(_item1, item) => true,
                >= 3 when Equals(_item2, item) => true,
                >= 4 when Equals(_item3, item) => true,
                _ => false
            };
        }

        public IReadOnlyList<object> Materialize()
        {
            if (_items is not null)
                return new List<object>(_items);

            var path = new List<object>(Count);
            if (Count >= 1) path.Add(_item0!);
            if (Count >= 2) path.Add(_item1!);
            if (Count >= 3) path.Add(_item2!);
            if (Count >= 4) path.Add(_item3!);

            return path;
        }
    }
}