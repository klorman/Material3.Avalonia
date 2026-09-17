using FluentAssertions;
using Material3.Avalonia.Build.Tasks;

namespace Material3.Avalonia.Tests.Symbols;

public sealed class MaterialSymbolBuildTests
{
    [Fact]
    public void ScannerUnionsEverySupportedAxamlDeclaration()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"material-symbol-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var xaml = Path.Combine(directory, "Symbols.axaml");
            File.WriteAllText(xaml,
                """
                <Styles xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
                    <Button Tag="{symbols:Symbol Home}" />
                    <Button Tag="{symbols:Symbol Home}" />
                    <Button Tag="{symbols:Symbol Tune, Families=Rounded}" />
                    <Button Tag="{symbols:Symbol Tune, Families=Sharp}" />
                    <Button Tag="{x:Static symbols:MaterialSymbol.Search}" />
                    <symbols:MaterialSymbolResource x:Key="DeleteIcon" Symbol="Delete" Families="Outlined" />
                    <symbols:MaterialSymbolsInclude x:Key="Player" Symbols="PlayArrow, Pause" Families="Rounded" />
                    <symbols:MaterialSymbolsInclude x:Key="Dynamic" Symbols="All" Families="Rounded" />
                </Styles>
                """);
            var manifest = Path.Combine(directory, "requests.manifest");
            var catalog = WriteCatalog(directory, "Delete", "Home", "Pause", "PlayArrow", "Search", "Tune");
            var task = new CollectMaterialSymbols
            {
                XamlFiles = [new Microsoft.Build.Utilities.TaskItem(xaml)],
                CatalogFile = catalog,
                OutputManifest = manifest
            };

            task.Execute().Should().BeTrue();
            File.ReadAllLines(manifest).Should().Equal(
                "*|2",
                "Delete|1",
                "Home|7",
                "Pause|2",
                "PlayArrow|2",
                "Search|7",
                "Tune|6");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ScannerRejectsUnknownSymbolsBeforeWritingManifest()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"material-symbol-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var xaml = Path.Combine(directory, "Symbols.axaml");
            File.WriteAllText(xaml,
                """
                <Styles xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
                    <symbols:MaterialSymbolsInclude x:Key="Player" Symbols="PlayArow" />
                </Styles>
                """);
            var buildEngine = new RecordingBuildEngine();
            var task = new CollectMaterialSymbols
            {
                BuildEngine = buildEngine,
                XamlFiles = [new Microsoft.Build.Utilities.TaskItem(xaml)],
                CatalogFile = WriteCatalog(directory, "PlayArrow"),
                OutputManifest = Path.Combine(directory, "requests.manifest")
            };

            task.Execute().Should().BeFalse();
            buildEngine.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("Unknown Material Symbol 'PlayArow'");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ScannerPreservesFamiliesForStaticMaterialSymbolResource()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"material-symbol-build-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var xaml = Path.Combine(directory, "Symbols.axaml");
            File.WriteAllText(xaml,
                """
                <Styles xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
                    <symbols:MaterialSymbolResource x:Key="DeleteIcon"
                                                     Symbol="{x:Static symbols:MaterialSymbol.Delete}"
                                                     Families="Rounded" />
                </Styles>
                """);
            var manifest = Path.Combine(directory, "requests.manifest");
            var task = new CollectMaterialSymbols
            {
                XamlFiles = [new Microsoft.Build.Utilities.TaskItem(xaml)],
                CatalogFile = WriteCatalog(directory, "Delete"),
                OutputManifest = manifest
            };

            task.Execute().Should().BeTrue();
            File.ReadAllLines(manifest).Should().Equal("Delete|2");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string WriteCatalog(string directory, params string[] symbols)
    {
        var path = Path.Combine(directory, "catalog.tsv");
        File.WriteAllLines(path, symbols.Select((symbol, index) => $"{symbol}\t{0xE000 + index:X}"));
        return path;
    }

    private sealed class RecordingBuildEngine : Microsoft.Build.Framework.IBuildEngine
    {
        public List<Microsoft.Build.Framework.BuildErrorEventArgs> Errors { get; } = [];
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public void LogErrorEvent(Microsoft.Build.Framework.BuildErrorEventArgs error) => Errors.Add(error);

        public void LogWarningEvent(Microsoft.Build.Framework.BuildWarningEventArgs warning)
        {
        }

        public void LogMessageEvent(Microsoft.Build.Framework.BuildMessageEventArgs message)
        {
        }

        public void LogCustomEvent(Microsoft.Build.Framework.CustomBuildEventArgs customEvent)
        {
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames,
            System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => false;
    }
}