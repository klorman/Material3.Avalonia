using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Material3.Avalonia.Build.Tasks;

public sealed unsafe class GenerateMaterialSymbolFonts : Microsoft.Build.Utilities.Task
{
    [Required] public ITaskItem[] Manifests { get; set; } = null!;
    [Required] public string CatalogFile { get; set; } = null!;
    [Required] public string SourceFontDirectory { get; set; } = null!;
    [Required] public string OutputDirectory { get; set; } = null!;
    [Output] public ITaskItem[] GeneratedFonts { get; private set; } = [];

#if NET8_0_OR_GREATER
    static GenerateMaterialSymbolFonts() => NativeLibrary.SetDllImportResolver(
        typeof(GenerateMaterialSymbolFonts).Assembly, ResolveHarfBuzz);
#endif

    public override bool Execute()
    {
        try
        {
            var requests = ReadRequests();
            var catalog = File.ReadLines(CatalogFile).Select(line => line.Split('\t'))
                .ToDictionary(parts => parts[0], parts => uint.Parse(parts[1], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
            Directory.CreateDirectory(OutputDirectory);
            var generated = new List<ITaskItem>();
            foreach (var (style, flag) in new[] { ("Outlined", 1), ("Rounded", 2), ("Sharp", 4) })
            {
                var all = requests.TryGetValue("*", out var allStyles) && (allStyles & flag) != 0;
                var codepoints = requests.Where(pair => pair.Key != "*" && (pair.Value & flag) != 0)
                    .Select(pair => catalog.TryGetValue(pair.Key, out var codepoint)
                        ? codepoint
                        : throw new InvalidDataException($"Unknown Material Symbol '{pair.Key}'."))
                    .Distinct().OrderBy(codepoint => codepoint).ToArray();
                if (!all && codepoints.Length == 0) continue;

                var source = Path.Combine(SourceFontDirectory, style + ".ttf");
                var output = Path.Combine(OutputDirectory, style + ".ttf");
                var stamp = output + ".sha256";
                var key = ComputeKey(source, all, codepoints);
                if (!File.Exists(output) || !File.Exists(stamp) || File.ReadAllText(stamp) != key)
                {
                    if (all) File.Copy(source, output, true);
                    else Subset(source, output, codepoints);
                    File.WriteAllText(stamp, key);
                }

                var item = new TaskItem(output);
                item.SetMetadata("LogicalName", $"Material3.Avalonia.Symbols.{style}.ttf");
                generated.Add(item);
            }

            GeneratedFonts = generated.ToArray();
            return true;
        }
        catch (Exception error)
        {
            Log.LogErrorFromException(error, true);
            return false;
        }
    }

    private Dictionary<string, int> ReadRequests()
    {
        var requests = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifest in Manifests.Select(item => item.ItemSpec).Distinct(StringComparer.OrdinalIgnoreCase))
        foreach (var line in File.ReadLines(manifest))
        {
            var separator = line.LastIndexOf('|');
            var symbol = line.Substring(0, separator);
            var styles = int.Parse(line.Substring(separator + 1), CultureInfo.InvariantCulture);
            requests[symbol] = requests.TryGetValue(symbol, out var current) ? current | styles : styles;
        }

        return requests;
    }

    private static string ComputeKey(string source, bool all, IReadOnlyList<uint> codepoints)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(File.ReadAllBytes(source));
        hash.AppendData(Encoding.UTF8.GetBytes("material3-avalonia-subset-v4\n" + all + "\n" +
                                               string.Join(",", codepoints)));
        return BitConverter.ToString(hash.GetHashAndReset()).Replace("-", string.Empty);
    }

    private static void Subset(string source, string output, IReadOnlyList<uint> codepoints)
    {
        var bytes = File.ReadAllBytes(source);
        fixed (byte* data = bytes)
        {
            var blob = Native.hb_blob_create(data, (uint)bytes.Length, 0, 0, 0);
            var face = Native.hb_face_create(blob, 0);
            var input = Native.hb_subset_input_create_or_fail();
            if (blob == 0 || face == 0 || input == 0)
                throw new InvalidOperationException("HarfBuzz initialization failed.");
            try
            {
                var set = Native.hb_subset_input_unicode_set(input);
                foreach (var codepoint in codepoints) Native.hb_set_add(set, codepoint);
                IncludeFilledGlyphs(face, input, codepoints);
                var subset = Native.hb_subset_or_fail(face, input);
                if (subset == 0)
                    throw new InvalidOperationException("HarfBuzz could not subset the Material Symbols font.");
                try
                {
                    var result = Native.hb_face_reference_blob(subset);
                    try
                    {
                        var resultData = Native.hb_blob_get_data(result, out var length);
                        if (resultData == null || length == 0)
                            throw new InvalidOperationException("HarfBuzz produced an empty font.");
                        var resultBytes = new byte[checked((int)length)];
                        Marshal.Copy((IntPtr)resultData, resultBytes, 0, resultBytes.Length);
                        using var stream = File.Create(output);
                        stream.Write(resultBytes, 0, resultBytes.Length);
                    }
                    finally
                    {
                        Native.hb_blob_destroy(result);
                    }
                }
                finally
                {
                    Native.hb_face_destroy(subset);
                }
            }
            finally
            {
                Native.hb_subset_input_destroy(input);
                Native.hb_face_destroy(face);
                Native.hb_blob_destroy(blob);
            }
        }
    }

    private static void IncludeFilledGlyphs(nint face, nint input, IReadOnlyList<uint> codepoints)
    {
        var font = Native.hb_font_create(face);
        var buffer = Native.hb_buffer_create();
        if (font == 0 || buffer == 0) throw new InvalidOperationException("HarfBuzz shaping initialization failed.");
        try
        {
            Native.hb_ot_font_set_funcs(font);
            var variation = new Variation(0x46494c4c, 1);
            Native.hb_font_set_variations(font, &variation, 1);
            var glyphSet = Native.hb_subset_input_glyph_set(input);
            foreach (var codepoint in codepoints)
            {
                Native.hb_buffer_reset(buffer);
                Native.hb_buffer_add_utf32(buffer, &codepoint, 1, 0, 1);
                Native.hb_buffer_guess_segment_properties(buffer);
                Native.hb_shape(font, buffer, 0, 0);
                var glyphs = Native.hb_buffer_get_glyph_infos(buffer, out var count);
                for (var index = 0; index < count; index++) Native.hb_set_add(glyphSet, glyphs[index]);
            }
        }
        finally
        {
            Native.hb_buffer_destroy(buffer);
            Native.hb_font_destroy(font);
        }
    }

#if NET8_0_OR_GREATER
    private static nint ResolveHarfBuzz(string name, System.Reflection.Assembly assembly, DllImportSearchPath? path)
    {
        if (name != "libHarfBuzzSharp") return 0;
        var assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;
        var filename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libHarfBuzzSharp.dll" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libHarfBuzzSharp.dylib" : "libHarfBuzzSharp.so";
        var candidates = Directory.EnumerateFiles(assemblyDirectory, filename, SearchOption.AllDirectories);
        foreach (var candidate in candidates)
            if (NativeLibrary.TryLoad(candidate, out var handle))
                return handle;
        return 0;
    }
#endif

    private static class Native
    {
        private const string Library = "libHarfBuzzSharp";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_blob_create(byte* data, uint length, int mode, nint user, nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_blob_destroy(nint blob);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* hb_blob_get_data(nint blob, out uint length);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_face_create(nint blob, uint index);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_face_destroy(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_face_reference_blob(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_font_create(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_destroy(nint font);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_ot_font_set_funcs(nint font);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_set_variations(nint font, Variation* variations, uint count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_buffer_create();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_buffer_destroy(nint buffer);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_buffer_reset(nint buffer);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_buffer_add_utf32(nint buffer, uint* text, int length, uint offset, int count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_buffer_guess_segment_properties(nint buffer);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_shape(nint font, nint buffer, nint features, uint count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint* hb_buffer_get_glyph_infos(nint buffer, out uint length);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_subset_input_create_or_fail();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_subset_input_destroy(nint input);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_subset_input_unicode_set(nint input);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_subset_input_glyph_set(nint input);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_set_add(nint set, uint codepoint);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_subset_or_fail(nint source, nint input);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Variation(uint tag, float value)
    {
        public readonly uint Tag = tag;
        public readonly float Value = value;
    }
}