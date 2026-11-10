using System;
using Pinta.Core;

namespace Pinta.Effects;

/// <summary>Per-render buffers, never shared by preview clones or retained across documents.</summary>
internal ref struct StudioFrame (ReadOnlySpan<ColorBgra> source, int width, int height)
{
	public readonly ReadOnlySpan<ColorBgra> Source = source;
	public readonly int Width = width;
	public readonly int Height = height;
	private ColorBgra[]? blur;
	private ColorBgra[]? lightBlur;
	private ColorBgra[]? streak;
	private ColorBgra[]? cells;
	private ColorBgra[]? sorted;
	private byte[]? eroded;
	private byte[]? dilated;
	private int cellWidth, cellHeight;

	public ColorBgra At (int x, int y) => Source[Math.Clamp (y, 0, Height - 1) * Width + Math.Clamp (x, 0, Width - 1)];
	public ColorBgra Outside (int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? ColorBgra.Zero : Source[y * Width + x];
	public ColorBgra Sample (double x, double y) => ModernSampling.Bilinear (Source, Width, Height, x, y);
	public double Lum (int x, int y) => StudioRgb.From (At (x, y)).Luma;
	public (double X, double Y) Slope (int x, int y, int radius = 1) => (
		(Lum (x + radius, y) - Lum (x - radius, y)) * .5,
		(Lum (x, y + radius) - Lum (x, y - radius)) * .5);
	public double Edge (int x, int y)
	{
		double gx = Lum (x + 1, y - 1) + 2 * Lum (x + 1, y) + Lum (x + 1, y + 1) - Lum (x - 1, y - 1) - 2 * Lum (x - 1, y) - Lum (x - 1, y + 1);
		double gy = Lum (x - 1, y + 1) + 2 * Lum (x, y + 1) + Lum (x + 1, y + 1) - Lum (x - 1, y - 1) - 2 * Lum (x, y - 1) - Lum (x + 1, y - 1);
		return Math.Sqrt (gx * gx + gy * gy) / 4;
	}
	public ColorBgra Blurred (int x, int y, int radius)
	{
		blur ??= GaussianApproximation (Source, Width, Height, radius);
		return blur[y * Width + x];
	}
	public ColorBgra LightBlurred (int x, int y, int radius, double threshold)
	{
		if (lightBlur is null) {
			var bright = BrightPass (threshold);
			lightBlur = GaussianApproximation (bright, Width, Height, radius);
		}
		return lightBlur[y * Width + x];
	}
	public ColorBgra LightStreak (int x, int y, int radius, double threshold)
	{
		if (streak is null) {
			streak = new ColorBgra[Source.Length];
			var buffer = new ColorBgra[Source.Length];
			int r = Math.Max (1, (int) Math.Round (radius / Math.Sqrt (3)));
			BoxPass (BrightPass (threshold), streak, Width, Height, r, true);
			BoxPass (streak, buffer, Width, Height, r, true);
			BoxPass (buffer, streak, Width, Height, r, true);
		}
		return streak[y * Width + x];
	}
	private ColorBgra[] BrightPass (double threshold)
	{
		var bright = new ColorBgra[Source.Length];
		for (int i = 0; i < bright.Length; i++) {
			var p = Source[i];
			double factor = StudioMath.Smooth (threshold, 1, StudioRgb.From (p).Luma);
			bright[i] = ColorBgra.FromBgra (StudioMath.Byte (p.B * factor), StudioMath.Byte (p.G * factor), StudioMath.Byte (p.R * factor), StudioMath.Byte (p.A * factor));
		}
		return bright;
	}
	private static ColorBgra[] GaussianApproximation (ReadOnlySpan<ColorBgra> input, int width, int height, int radius)
	{
		var a = new ColorBgra[input.Length]; var b = new ColorBgra[input.Length];
		int r = Math.Max (1, (int) Math.Round (radius / Math.Sqrt (3)));
		BoxPass (input, a, width, height, r, true); BoxPass (a, b, width, height, r, false);
		for (int pass = 0; pass < 2; pass++) { BoxPass (b, a, width, height, r, true); BoxPass (a, b, width, height, r, false); }
		return b;
	}
	private static void BoxPass (ReadOnlySpan<ColorBgra> input, Span<ColorBgra> output, int width, int height, int radius, bool horizontal)
	{
		int lines = horizontal ? height : width, length = horizontal ? width : height, count = radius * 2 + 1;
		for (int line = 0; line < lines; line++) {
			int sb = 0, sg = 0, sr = 0, sa = 0;
			for (int k = -radius; k <= radius; k++) {
				int pos = Math.Clamp (k, 0, length - 1), index = horizontal ? line * width + pos : pos * width + line;
				var c = input[index]; sb += c.B; sg += c.G; sr += c.R; sa += c.A;
			}
			for (int pos = 0; pos < length; pos++) {
				int index = horizontal ? line * width + pos : pos * width + line;
				output[index] = ColorBgra.FromBgra ((byte) (sb / count), (byte) (sg / count), (byte) (sr / count), (byte) (sa / count));
				int remove = Math.Clamp (pos - radius, 0, length - 1), add = Math.Clamp (pos + radius + 1, 0, length - 1);
				var old = input[horizontal ? line * width + remove : remove * width + line];
				var next = input[horizontal ? line * width + add : add * width + line];
				sb += next.B - old.B; sg += next.G - old.G; sr += next.R - old.R; sa += next.A - old.A;
			}
		}
	}
	public ColorBgra AverageCell (int x, int y, int cw, int ch)
	{
		if (cells is null || cellWidth != cw || cellHeight != ch) {
			cellWidth = cw; cellHeight = ch;
			int columns = (Width + cw - 1) / cw, rows = (Height + ch - 1) / ch;
			cells = new ColorBgra[columns * rows];
			for (int cy = 0; cy < rows; cy++) for (int cx = 0; cx < columns; cx++) {
				long b = 0, g = 0, r = 0, a = 0; int count = 0;
				for (int yy = cy * ch; yy < Math.Min ((cy + 1) * ch, Height); yy++)
					for (int xx = cx * cw; xx < Math.Min ((cx + 1) * cw, Width); xx++) {
						var c = Source[yy * Width + xx]; b += c.B; g += c.G; r += c.R; a += c.A; count++;
					}
				cells[cy * columns + cx] = ColorBgra.FromBgra ((byte) (b / count), (byte) (g / count), (byte) (r / count), (byte) (a / count));
			}
		}
		return cells[(y / ch) * ((Width + cw - 1) / cw) + x / cw];
	}
	public byte MorphAlpha (int x, int y, int radius, bool maximum)
	{
		ref byte[]? cached = ref (maximum ? ref dilated : ref eroded);
		if (cached is null) {
			var alpha = new byte[Source.Length]; for (int i = 0; i < alpha.Length; i++) alpha[i] = Source[i].A;
			var intermediate = new byte[alpha.Length]; cached = new byte[alpha.Length];
			MorphPass (alpha, intermediate, Width, Height, radius, true, maximum);
			MorphPass (intermediate, cached, Width, Height, radius, false, maximum);
		}
		return cached[y * Width + x];
	}
	private static void MorphPass (ReadOnlySpan<byte> input, Span<byte> output, int width, int height, int radius, bool horizontal, bool maximum)
	{
		int lines = horizontal ? height : width, length = horizontal ? width : height;
		int[] indices = new int[length + 2 * radius]; int[] values = new int[indices.Length];
		for (int line = 0; line < lines; line++) {
			int head = 0, tail = 0;
			for (int i = -radius; i < length + radius; i++) {
				int v = i < 0 || i >= length ? 0 : input[horizontal ? line * width + i : i * width + line];
				while (tail > head && (maximum ? values[tail - 1] <= v : values[tail - 1] >= v)) tail--;
				indices[tail] = i; values[tail] = v; tail++;
				while (tail > head && indices[head] < i - 2 * radius) head++;
				int pos = i - radius;
				if (pos >= 0 && pos < length) output[horizontal ? line * width + pos : pos * width + line] = (byte) values[head];
			}
		}
	}
	public ColorBgra Sorted (int x, int y, int span, double threshold)
	{
		if (sorted is null) {
			sorted = Source.ToArray ();
			for (int yy = 0; yy < Height; yy++) for (int start = 0; start < Width; start += span) {
				int end = Math.Min (start + span, Width), run = -1;
				for (int xx = start; xx <= end; xx++) {
					bool eligible = xx < end && sorted[yy * Width + xx].A > 0 && StudioRgb.From (sorted[yy * Width + xx]).Luma >= threshold;
					if (eligible && run < 0) run = xx;
					if (!eligible && run >= 0) {
						Array.Sort (sorted, yy * Width + run, xx - run, LuminanceComparer.Instance); run = -1;
					}
				}
			}
		}
		return sorted[y * Width + x];
	}
	private sealed class LuminanceComparer : System.Collections.Generic.IComparer<ColorBgra>
	{
		public static readonly LuminanceComparer Instance = new ();
		public int Compare (ColorBgra a, ColorBgra b) => StudioRgb.From (a).Luma.CompareTo (StudioRgb.From (b).Luma);
	}
}
