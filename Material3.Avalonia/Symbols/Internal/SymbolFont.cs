using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;

namespace Material3.Avalonia.Symbols.Internal;

internal sealed unsafe class SymbolFont
{
    private readonly nint _font;
    private readonly nint _buffer;
    private readonly uint _unitsPerEm;
    private static readonly nint DrawFunctions = CreateDrawFunctions();

    public SymbolFont(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        fixed (byte* data = bytes)
        {
            var blob = Native.hb_blob_create(data, (uint)bytes.Length, 0, 0, 0);
            var face = Native.hb_face_create(blob, 0);
            _unitsPerEm = Native.hb_face_get_upem(face);
            _font = Native.hb_font_create(face);
            _buffer = Native.hb_buffer_create();
            Native.hb_ot_font_set_funcs(_font);
            Native.hb_font_set_scale(_font, (int)_unitsPerEm, (int)_unitsPerEm);
            Native.hb_face_destroy(face);
            Native.hb_blob_destroy(blob);
        }
    }

    ~SymbolFont()
    {
        Native.hb_buffer_destroy(_buffer);
        Native.hb_font_destroy(_font);
    }

    public bool Contains(MaterialSymbol symbol) => Native.hb_font_get_nominal_glyph(_font, (uint)symbol, out _) != 0;

    public SymbolDrawing Draw(MaterialSymbol symbol, float weight, float grade, float fill, float opticalSize)
    {
        if (!Contains(symbol))
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Symbol is absent from the registered font.");
        var axes = stackalloc Variation[]
        {
            new(0x77676874, weight), new(0x47524144, grade), new(0x46494c4c, fill), new(0x6f70737a, opticalSize)
        };
        Native.hb_font_set_variations(_font, axes, 4);
        var codepoint = (uint)symbol;
        Native.hb_buffer_reset(_buffer);
        Native.hb_buffer_add_utf32(_buffer, &codepoint, 1, 0, 1);
        Native.hb_buffer_guess_segment_properties(_buffer);
        // Symbols use conditional GSUB substitutions near FILL=1; cmap lookup alone skips them.
        Native.hb_shape(_font, _buffer, 0, 0);
        var glyphs = Native.hb_buffer_get_glyph_infos(_buffer, out var count);
        if (count != 1 || glyphs == null || *glyphs == 0)
            throw new InvalidOperationException($"Material Symbol '{symbol}' did not shape to a single glyph.");
        var glyph = *glyphs;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.SetFillRule(FillRule.NonZero);
            var state = new DrawState(context);
            var handle = GCHandle.Alloc(state);
            try
            {
                Native.hb_font_draw_glyph(_font, glyph, DrawFunctions, GCHandle.ToIntPtr(handle));
                state.Error?.Throw();
            }
            finally
            {
                handle.Free();
            }
        }

        // Google Symbols use a square em field with the baseline at its bottom edge.
        return new SymbolDrawing(geometry, new Rect(0, -(double)_unitsPerEm, _unitsPerEm, _unitsPerEm));
    }

    private sealed class DrawState(StreamGeometryContext context)
    {
        public StreamGeometryContext Context { get; } = context;
        public ExceptionDispatchInfo? Error { get; set; }
    }

    private static nint CreateDrawFunctions()
    {
        var funcs = Native.hb_draw_funcs_create();
        Native.hb_draw_funcs_set_move_to_func(funcs, &Move, 0, 0);
        Native.hb_draw_funcs_set_line_to_func(funcs, &Line, 0, 0);
        Native.hb_draw_funcs_set_quadratic_to_func(funcs, &Quadratic, 0, 0);
        Native.hb_draw_funcs_set_cubic_to_func(funcs, &Cubic, 0, 0);
        Native.hb_draw_funcs_set_close_path_func(funcs, &Close, 0, 0);
        Native.hb_draw_funcs_make_immutable(funcs);
        return funcs;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Move(nint funcs, nint data, nint state, float x, float y, nint user)
    {
        var target = (DrawState)GCHandle.FromIntPtr(data).Target!;
        if (target.Error is not null) return;
        try
        {
            target.Context.BeginFigure(new Point(x, -y), true);
        }
        catch (Exception error)
        {
            target.Error = ExceptionDispatchInfo.Capture(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Line(nint funcs, nint data, nint state, float x, float y, nint user)
    {
        var target = (DrawState)GCHandle.FromIntPtr(data).Target!;
        if (target.Error is not null) return;
        try
        {
            target.Context.LineTo(new Point(x, -y));
        }
        catch (Exception error)
        {
            target.Error = ExceptionDispatchInfo.Capture(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Quadratic(nint funcs, nint data, nint state, float cx, float cy, float x, float y, nint user)
    {
        var target = (DrawState)GCHandle.FromIntPtr(data).Target!;
        if (target.Error is not null) return;
        try
        {
            target.Context.QuadraticBezierTo(new Point(cx, -cy), new Point(x, -y));
        }
        catch (Exception error)
        {
            target.Error = ExceptionDispatchInfo.Capture(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Cubic(nint funcs, nint data, nint state, float cx, float cy, float dx, float dy, float x,
        float y, nint user)
    {
        var target = (DrawState)GCHandle.FromIntPtr(data).Target!;
        if (target.Error is not null) return;
        try
        {
            target.Context.CubicBezierTo(new Point(cx, -cy), new Point(dx, -dy), new Point(x, -y));
        }
        catch (Exception error)
        {
            target.Error = ExceptionDispatchInfo.Capture(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Close(nint funcs, nint data, nint state, nint user)
    {
        var target = (DrawState)GCHandle.FromIntPtr(data).Target!;
        if (target.Error is not null) return;
        try
        {
            target.Context.EndFigure(true);
        }
        catch (Exception error)
        {
            target.Error = ExceptionDispatchInfo.Capture(error);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Variation(uint tag, float value)
    {
        public readonly uint Tag = tag;
        public readonly float Value = value;
    }

    private static class Native
    {
        private const string Library = "libHarfBuzzSharp";

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
        public static extern nint hb_blob_create(byte* data, uint length, int mode, nint user, nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_face_create(nint blob, uint index);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint hb_face_get_upem(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_font_create(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_ot_font_set_funcs(nint font);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_set_scale(nint font, int x, int y);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_face_destroy(nint face);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_blob_destroy(nint blob);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_destroy(nint font);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hb_font_get_nominal_glyph(nint font, uint codepoint, out uint glyph);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_set_variations(nint font, Variation* variations, uint count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint hb_draw_funcs_create();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_make_immutable(nint funcs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_font_draw_glyph(nint font, uint glyph, nint funcs, nint data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_set_move_to_func(nint funcs,
            delegate* unmanaged[Cdecl]<nint, nint, nint, float, float, nint, void> callback, nint user, nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_set_line_to_func(nint funcs,
            delegate* unmanaged[Cdecl]<nint, nint, nint, float, float, nint, void> callback, nint user, nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_set_quadratic_to_func(nint funcs,
            delegate* unmanaged[Cdecl]<nint, nint, nint, float, float, float, float, nint, void> callback, nint user,
            nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_set_cubic_to_func(nint funcs,
            delegate* unmanaged[Cdecl]<nint, nint, nint, float, float, float, float, float, float, nint, void> callback,
            nint user, nint destroy);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void hb_draw_funcs_set_close_path_func(nint funcs,
            delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void> callback, nint user, nint destroy);
    }
}