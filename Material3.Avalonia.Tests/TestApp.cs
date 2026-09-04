using Avalonia;
using Avalonia.Headless;

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
                .SetupWithoutStarting();
        }
    }
}
