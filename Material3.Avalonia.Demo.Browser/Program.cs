using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Material3.Avalonia;
using Material3.Avalonia.Demo;
using Material3.Avalonia.Fonts.Roboto;

[SupportedOSPlatform("browser")]
internal static class Program
{
    public static Task Main(string[] args) => AppBuilder.Configure<App>()
        .WithRobotoFont()
        .UseBrowser()
        .StartBrowserAppAsync("out");
}