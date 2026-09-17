using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Material3.Avalonia.Fonts.Roboto;

/// <summary>Provides Roboto as an optional application font.</summary>
public static class RobotoAppBuilderExtensions
{
    private const string FontCollectionKey = "fonts:Roboto";
    private const string DefaultFontFamilyName = "fonts:Roboto#Roboto";

    /// <summary>Registers Roboto and selects it as the default Avalonia font family.</summary>
    public static AppBuilder WithRobotoFont(this AppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureFonts(static manager => manager.AddFontCollection(new RobotoFontCollection()))
            .With(new FontManagerOptions { DefaultFamilyName = DefaultFontFamilyName });
    }

    private sealed class RobotoFontCollection() : EmbeddedFontCollection(
        new Uri(FontCollectionKey, UriKind.Absolute),
        new Uri("avares://Material3.Avalonia.Fonts.Roboto/Assets", UriKind.Absolute));
}