namespace Material3.Avalonia.Motion.Transitions;

/// <summary>Animates explicit numeric values and switches automatic (null) values immediately.</summary>
public sealed class SpringNullableDoubleTransition : SpringTransitionBase<double?, NullableDoubleAdapter>
{
    /// <inheritdoc />
    protected override double? Interpolate(double progress, double? from, double? to) =>
        from is null || to is null
            ? to
            : base.Interpolate(progress, from, to);
}

/// <summary>Adapts explicit nullable numbers to spring interpolation.</summary>
public sealed class NullableDoubleAdapter : ISpringValueAdapter<double?>
{
    /// <inheritdoc />
    public int Components => 1;

    /// <inheritdoc />
    public void Read(double? value, Span<double> into) =>
        into[0] = value ?? throw new ArgumentNullException(nameof(value));

    /// <inheritdoc />
    public double? Make(ReadOnlySpan<double> components) => components[0];
}