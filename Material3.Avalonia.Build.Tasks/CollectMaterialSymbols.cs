using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Material3.Avalonia.Build.Tasks;

public sealed class CollectMaterialSymbols : Microsoft.Build.Utilities.Task
{
    public ITaskItem[] XamlFiles { get; set; } = [];
    public ITaskItem[] ReferencedManifests { get; set; } = [];
    [Required] public string CatalogFile { get; set; } = null!;
    [Required] public string OutputManifest { get; set; } = null!;
    [Output] public ITaskItem Manifest { get; private set; } = null!;

    public override bool Execute()
    {
        try
        {
            var requests = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in ReferencedManifests)
                ReadManifest(item.ItemSpec, requests);

            foreach (var item in XamlFiles)
                ReadXaml(item.ItemSpec, requests);

            var catalog = new HashSet<string>(File.ReadLines(CatalogFile)
                .Select(line => line.Split('\t')[0]), StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in requests.Keys.Where(symbol => symbol != "*"))
                if (!catalog.Contains(symbol))
                    throw new InvalidDataException($"Unknown Material Symbol '{symbol}'.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputManifest)!);
            var content = string.Join("\n", requests.Select(pair => $"{pair.Key}|{pair.Value}"));
            if (content.Length != 0) content += "\n";
            WriteIfChanged(OutputManifest, content);
            Manifest = new TaskItem(OutputManifest);
            return true;
        }
        catch (Exception error)
        {
            Log.LogErrorFromException(error, true);
            return false;
        }
    }

    private static void ReadManifest(string path, IDictionary<string, int> requests)
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path))
        {
            var separator = line.LastIndexOf('|');
            if (separator <= 0 || !int.TryParse(line.Substring(separator + 1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var families))
                throw new InvalidDataException($"Invalid Material Symbols manifest line in '{path}': {line}");
            Add(requests, line.Substring(0, separator), families);
        }
    }

    private static void ReadXaml(string path, IDictionary<string, int> requests)
    {
        if (!File.Exists(path)) return;
        var document = XDocument.Load(path, LoadOptions.None);
        if (document.Root is null) return;

        foreach (var element in document.Root.DescendantsAndSelf())
        {
            if (IsSymbolsElement(element, "MaterialSymbolResource"))
            {
                var symbol = element.Attribute("Symbol")?.Value;
                if (string.IsNullOrWhiteSpace(symbol))
                    throw new InvalidDataException($"MaterialSymbolResource in '{path}' requires Symbol.");
                var isStaticSymbol = TryParseStaticSymbol(element, symbol!, out var staticSymbol);
                Add(requests, (isStaticSymbol ? staticSymbol : symbol)!.Trim(),
                    ParseFamilies(element.Attribute("Families")?.Value));

                foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
                {
                    if (isStaticSymbol && attribute.Name.LocalName == "Symbol") continue;
                    ReadMarkupExtensions(element, attribute.Value, requests);
                }

                continue;
            }

            if (IsSymbolsElement(element, "MaterialSymbolsInclude"))
            {
                ReadInclude(element, path, requests);
            }

            foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
                ReadMarkupExtensions(element, attribute.Value, requests);
        }
    }

    private static void ReadInclude(XElement element, string path, IDictionary<string, int> requests)
    {
        var symbols = element.Attribute("Symbols")?.Value;
        if (string.IsNullOrWhiteSpace(symbols))
            throw new InvalidDataException($"MaterialSymbolsInclude in '{path}' requires Symbols.");

        var families = ParseFamilies(element.Attribute("Families")?.Value);
        foreach (var rawSymbol in symbols!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var symbol = rawSymbol.Trim();
            if (symbol.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                Add(requests, "*", families);
                return;
            }

            Add(requests, symbol, families);
        }
    }

    private static void ReadMarkupExtensions(XElement element, string value, IDictionary<string, int> requests)
    {
        foreach (Match match in SymbolExtensionRegex.Matches(value))
        {
            if (!IsSymbolsNamespace(element.GetNamespaceOfPrefix(match.Groups["prefix"].Value))) continue;
            var (symbol, families) = ParseSymbolExtension(match.Groups["body"].Value);
            Add(requests, symbol, families);
        }

        foreach (Match match in StaticSymbolRegex.Matches(value))
        {
            if (!IsXamlNamespace(element.GetNamespaceOfPrefix(match.Groups["x"].Value)) ||
                !IsSymbolsNamespace(element.GetNamespaceOfPrefix(match.Groups["prefix"].Value))) continue;
            Add(requests, match.Groups["symbol"].Value, AllFamilies);
        }
    }

    private static bool TryParseStaticSymbol(XElement element, string value, out string symbol)
    {
        var match = StaticSymbolValueRegex.Match(value);
        if (match.Success && IsXamlNamespace(element.GetNamespaceOfPrefix(match.Groups["x"].Value)) &&
            IsSymbolsNamespace(element.GetNamespaceOfPrefix(match.Groups["prefix"].Value)))
        {
            symbol = match.Groups["symbol"].Value;
            return true;
        }

        symbol = string.Empty;
        return false;
    }

    private static (string Symbol, int Families) ParseSymbolExtension(string body)
    {
        string? symbol = null;
        string? families = null;
        var readingFamilies = false;
        foreach (var rawPart in SplitArguments(body))
        {
            var part = rawPart.Trim();
            var equals = part.IndexOf('=');
            if (equals < 0)
            {
                if (symbol is null)
                    symbol = Unquote(part);
                else if (readingFamilies)
                    families += "," + Unquote(part);
                continue;
            }

            var name = part.Substring(0, equals).Trim();
            var argument = Unquote(part.Substring(equals + 1).Trim());
            readingFamilies = name.Equals("Families", StringComparison.OrdinalIgnoreCase);
            if (name.Equals("Symbol", StringComparison.OrdinalIgnoreCase)) symbol = argument;
            else if (readingFamilies) families = argument;
        }

        if (string.IsNullOrWhiteSpace(symbol))
            throw new InvalidDataException("Symbol markup extension requires a MaterialSymbol value.");
        return (symbol!, ParseFamilies(families));
    }

    private static IEnumerable<string> SplitArguments(string value)
    {
        var start = 0;
        var quote = '\0';
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (quote == '\0' && character is '\'' or '"') quote = character;
            else if (character == quote) quote = '\0';
            else if (quote == '\0' && character == ',')
            {
                yield return value.Substring(start, index - start);
                start = index + 1;
            }
        }

        yield return value.Substring(start);
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == value[value.Length - 1] && value[0] is '\'' or '"'
            ? value.Substring(1, value.Length - 2)
            : value;

    private static bool IsSymbolsElement(XElement element, string name) =>
        element.Name.LocalName == name && IsSymbolsNamespace(element.Name.Namespace);

    private static bool IsSymbolsNamespace(XNamespace? value) => value is not null &&
                                                                 (value.NamespaceName.Equals(
                                                                      "using:Material3.Avalonia.Symbols",
                                                                      StringComparison.Ordinal) ||
                                                                  value.NamespaceName.Equals(
                                                                      "clr-namespace:Material3.Avalonia.Symbols",
                                                                      StringComparison.Ordinal) ||
                                                                  value.NamespaceName.StartsWith(
                                                                      "clr-namespace:Material3.Avalonia.Symbols;",
                                                                      StringComparison.Ordinal));

    private static bool IsXamlNamespace(XNamespace? value) =>
        value?.NamespaceName == "http://schemas.microsoft.com/winfx/2006/xaml";

    private const int AllFamilies = 7;

    private static int ParseFamilies(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value!.Equals("All", StringComparison.OrdinalIgnoreCase))
            return AllFamilies;
        var result = 0;
        foreach (var rawPart in value.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var part = rawPart.Trim();
            result |= part.ToLowerInvariant() switch
            {
                "none" => 0,
                "outlined" => 1,
                "rounded" => 2,
                "sharp" => 4,
                _ => throw new InvalidDataException($"Unknown Material Symbol family '{part}'.")
            };
        }

        return result;
    }

    private static void Add(IDictionary<string, int> requests, string symbol, int families) =>
        requests[symbol] = requests.TryGetValue(symbol, out var current) ? current | families : families;

    private static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path) == content) return;
        File.WriteAllText(path, content);
    }

    private static readonly Regex SymbolExtensionRegex = new(
        @"\{(?<prefix>[A-Za-z_][\w.-]*):Symbol(?:Extension)?\s+(?<body>[^{}]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StaticSymbolRegex = new(
        @"\{(?<x>[A-Za-z_][\w.-]*):Static\s+(?<prefix>[A-Za-z_][\w.-]*):MaterialSymbol\.(?<symbol>[A-Za-z_][\w]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StaticSymbolValueRegex = new(
        @"^\s*\{(?<x>[A-Za-z_][\w.-]*):Static\s+(?<prefix>[A-Za-z_][\w.-]*):MaterialSymbol\.(?<symbol>[A-Za-z_][\w]*)\}\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
}