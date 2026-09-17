using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Material3.Avalonia.Fonts.Roboto;

namespace Material3.Avalonia.Tests;

internal static class TestApp
{
    private static readonly object SyncRoot = new();

    public static void EnsureStarted()
    {
        if (Application.Current is not null)
            return;

        lock (SyncRoot)
        {
            if (Application.Current is not null)
                return;

            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .WithRobotoFont()
                .SetupWithoutStarting();
            Application.Current!.Styles.Add((Styles)AvaloniaXamlLoader.Load(
                new Uri("avares://Material3.Avalonia/Theme/MaterialThemeStyles.axaml")));
        }
    }
}