namespace Material3.Avalonia.Density;

internal static class DensityCalculator
{
    private const int MinDensityLevel = (int)MaterialDensity.Dense5;
    private const int MaxDensityLevel = (int)MaterialDensity.Default;

    public static MaterialDensity ClampDensity(MaterialDensity requestedDensity, MaterialDensity mostDense)
    {
        var requestedLevel = NormalizeLevel(requestedDensity);
        var mostDenseLevel = NormalizeLevel(mostDense);

        return (MaterialDensity)Math.Max(requestedLevel, mostDenseLevel);
    }

    public static double GetDelta(MaterialDensity requestedDensity, MaterialDensity mostDense)
    {
        var effectiveDensity = ClampDensity(requestedDensity, mostDense);

        return (int)effectiveDensity * MaterialDensityMetrics.LevelDelta;
    }

    private static int NormalizeLevel(MaterialDensity density)
    {
        return Math.Clamp((int)density, MinDensityLevel, MaxDensityLevel);
    }
}
