using Avalonia.Media;
using Bdziam.UI.Theming.MaterialColors.DynamicColor;

namespace Material3.Avalonia.Tokens.System;

internal static class ColorResourceWriter
{
    private static string BuildSysColorKey(string roleName)
    {
        return $"MdSys{roleName}Color";
    }

    private static KeyValuePair<object, object?> CreateColor(string key, uint argb)
    {
        return new KeyValuePair<object, object?>(key, Color.FromUInt32(argb));
    }

    private static KeyValuePair<object, object?> CreateRoleColor(string roleName, uint argb)
    {
        return CreateColor(BuildSysColorKey(roleName), argb);
    }

    public static void AddResources(ICollection<KeyValuePair<object, object?>> resources, DynamicScheme scheme)
    {
        resources.Add(CreateRoleColor(nameof(DynamicScheme.Background), scheme.Background));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnBackground), scheme.OnBackground));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.Surface), scheme.Surface));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceDim), scheme.SurfaceDim));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceBright), scheme.SurfaceBright));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceContainerLowest), scheme.SurfaceContainerLowest));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceContainerLow), scheme.SurfaceContainerLow));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceContainer), scheme.SurfaceContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceContainerHigh), scheme.SurfaceContainerHigh));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceContainerHighest), scheme.SurfaceContainerHighest));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSurface), scheme.OnSurface));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceVariant), scheme.SurfaceVariant));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSurfaceVariant), scheme.OnSurfaceVariant));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.InverseSurface), scheme.InverseSurface));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.InverseOnSurface), scheme.InverseOnSurface));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.Outline), scheme.Outline));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OutlineVariant), scheme.OutlineVariant));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.Shadow), scheme.Shadow));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.Scrim), scheme.Scrim));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SurfaceTint), scheme.SurfaceTint));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Primary), scheme.Primary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnPrimary), scheme.OnPrimary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.PrimaryContainer), scheme.PrimaryContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnPrimaryContainer), scheme.OnPrimaryContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.InversePrimary), scheme.InversePrimary));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Secondary), scheme.Secondary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSecondary), scheme.OnSecondary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SecondaryContainer), scheme.SecondaryContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSecondaryContainer), scheme.OnSecondaryContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Tertiary), scheme.Tertiary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnTertiary), scheme.OnTertiary));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.TertiaryContainer), scheme.TertiaryContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnTertiaryContainer), scheme.OnTertiaryContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Error), scheme.Error));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnError), scheme.OnError));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.ErrorContainer), scheme.ErrorContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnErrorContainer), scheme.OnErrorContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Warning), scheme.Warning));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnWarning), scheme.OnWarning));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.WarningContainer), scheme.WarningContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnWarningContainer), scheme.OnWarningContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Info), scheme.Info));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnInfo), scheme.OnInfo));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.InfoContainer), scheme.InfoContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnInfoContainer), scheme.OnInfoContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.Success), scheme.Success));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSuccess), scheme.OnSuccess));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SuccessContainer), scheme.SuccessContainer));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSuccessContainer), scheme.OnSuccessContainer));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.PrimaryFixed), scheme.PrimaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.PrimaryFixedDim), scheme.PrimaryFixedDim));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnPrimaryFixed), scheme.OnPrimaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnPrimaryFixedVariant), scheme.OnPrimaryFixedVariant));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.SecondaryFixed), scheme.SecondaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.SecondaryFixedDim), scheme.SecondaryFixedDim));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSecondaryFixed), scheme.OnSecondaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnSecondaryFixedVariant), scheme.OnSecondaryFixedVariant));

        resources.Add(CreateRoleColor(nameof(DynamicScheme.TertiaryFixed), scheme.TertiaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.TertiaryFixedDim), scheme.TertiaryFixedDim));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnTertiaryFixed), scheme.OnTertiaryFixed));
        resources.Add(CreateRoleColor(nameof(DynamicScheme.OnTertiaryFixedVariant), scheme.OnTertiaryFixedVariant));
    }
}