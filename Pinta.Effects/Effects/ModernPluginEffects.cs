// Independently written implementations inspired by publicly documented effect categories.
// Not binary-compatible with, or derived from, third-party Paint.NET plugins. MIT license.
using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class GradientMappingEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Gradient Mapping");
	public override string Icon => "pinta-modern-gradient-map";
	public override string EffectMenuCategory => Translations.GetString ("Color");
	public GradientMappingData Data => (GradientMappingData) EffectData!;
	public GradientMappingEffect () { EffectData = new GradientMappingData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		double t = (.2126 * p.R + .7152 * p.G + .0722 * p.B) / 255;
		if (Data.Reverse) t = 1 - t;
		double amount = Data.Amount / 100.0;
		Color a = Data.Shadows, b = Data.Highlights;
		byte Channel (byte original, double start, double end) => Byte (original * (1 - amount) + (start + (end - start) * t) * 255 * amount);
		return ColorBgra.FromBgra (Channel (p.B, a.B, b.B), Channel (p.G, a.G, b.G), Channel (p.R, a.R, b.R), p.A);
	}
}
public sealed class GradientMappingData : EffectData
{
	[Caption ("Shadows")] public Color Shadows { get; set; } = new (.12, .05, .3);
	[Caption ("Highlights")] public Color Highlights { get; set; } = new (1, .8, .3);
	[Caption ("Amount (%)"), MinimumValue (0), MaximumValue (100)] public int Amount { get; set; } = 100;
	[Caption ("Reverse")] public bool Reverse { get; set; }
}

public sealed class DropShadowEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Drop Shadow");
	public override string Icon => "pinta-modern-drop-shadow";
	public override bool IsTileable => false;
	public override string EffectMenuCategory => Translations.GetString ("Object");
	public DropShadowData Data => (DropShadowData) EffectData!;
	public DropShadowEffect () { EffectData = new DropShadowData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize ();
		// Separable box convolution is linear in image size, regardless of blur radius.
		// Compute from the entire source so selection and tile edges don't affect the shadow.
		var horizontal = new double[size.Width * size.Height];
		var blurred = new double[horizontal.Length];
		int radius = Math.Clamp (Data.Radius, 0, 64), diameter = radius * 2 + 1;
		var src = source.GetReadOnlyPixelData ();
		for (int y = 0; y < size.Height; y++) {
			double sum = 0;
			for (int x = 0; x <= Math.Min (radius, size.Width - 1); x++) sum += src[y * size.Width + x].A;
			for (int x = 0; x < size.Width; x++) {
				horizontal[y * size.Width + x] = sum / diameter;
				int remove = x - radius, add = x + radius + 1;
				if (remove >= 0) sum -= src[y * size.Width + remove].A;
				if (add < size.Width) sum += src[y * size.Width + add].A;
			}
		}
		for (int x = 0; x < size.Width; x++) {
			double sum = 0;
			for (int y = 0; y <= Math.Min (radius, size.Height - 1); y++) sum += horizontal[y * size.Width + x];
			for (int y = 0; y < size.Height; y++) {
				blurred[y * size.Width + x] = sum / diameter;
				int remove = y - radius, add = y + radius + 1;
				if (remove >= 0) sum -= horizontal[remove * size.Width + x];
				if (add < size.Height) sum += horizontal[add * size.Width + x];
			}
		}
		var dst = destination.GetPixelData ();
		Color shadow = Data.Color;
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X - Data.OffsetX, y = p.coordinates.Y - Data.OffsetY;
			double a = x < 0 || y < 0 || x >= size.Width || y >= size.Height ? 0 : blurred[y * size.Width + x] * Data.Opacity / 100.0 * shadow.A;
			var c = src[p.memoryOffset];
			double behind = (255 - c.A) / 255.0;
			byte Byte (double v) => (byte) Math.Clamp (Math.Round (v), 0, 255);
			dst[p.memoryOffset] = ColorBgra.FromBgra (Byte (c.B + shadow.B * a * behind), Byte (c.G + shadow.G * a * behind), Byte (c.R + shadow.R * a * behind), Byte (c.A + a * behind));
		}
	}
}
public sealed class DropShadowData : EffectData
{
	[Caption ("Horizontal Offset"), MinimumValue (-200), MaximumValue (200)] public int OffsetX { get; set; } = 8;
	[Caption ("Vertical Offset"), MinimumValue (-200), MaximumValue (200)] public int OffsetY { get; set; } = 8;
	[Caption ("Blur Radius"), MinimumValue (0), MaximumValue (64)] public int Radius { get; set; } = 5;
	[Caption ("Opacity (%)"), MinimumValue (0), MaximumValue (100)] public int Opacity { get; set; } = 60;
	[Caption ("Shadow Color")] public Color Color { get; set; } = Color.Black;
}

public static class PaintCells
{
	public static PointI Seed (int x, int y, int cell, int width, int height)
	{
		uint h = unchecked((uint) (x * 374761393 + y * 668265263));
		h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
		double jx = (h & 255) / 255.0 * .6 + .2, jy = ((h >> 8) & 255) / 255.0 * .6 + .2;
		return new (Math.Clamp ((int) ((x + jx) * cell), 0, width - 1), Math.Clamp ((int) ((y + jy) * cell), 0, height - 1));
	}
}

public sealed class PointillismEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Pointillism");
	public override string Icon => "pinta-modern-pointillism";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Artistic");
	public PointillismData Data => (PointillismData) EffectData!;
	public PointillismEffect () { EffectData = new PointillismData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize (); var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		int cell = Math.Max (2, Data.Spacing);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X, y = p.coordinates.Y;
			double nearest = double.MaxValue; var point = PointI.Zero;
			for (int j = -1; j <= 1; j++) for (int i = -1; i <= 1; i++) {
				var seed = PaintCells.Seed (x / cell + i, y / cell + j, cell, size.Width, size.Height);
				double distance = (x - seed.X) * (x - seed.X) + (y - seed.Y) * (y - seed.Y);
				if (distance < nearest) { nearest = distance; point = seed; }
			}
			var c = src[point.Y * size.Width + point.X].ToStraightAlpha ();
			double coverage = Math.Clamp (cell * Data.DotSize / 100.0 - Math.Sqrt (nearest) + .5, 0, 1);
			byte Channel (byte v) => (byte) Math.Clamp (Math.Round (255 + (v - 255) * coverage), 0, 255);
			dst[p.memoryOffset] = ColorBgra.FromBgra (Channel (c.B), Channel (c.G), Channel (c.R), src[p.memoryOffset].A).ToPremultipliedAlpha ();
		}
	}
}
public sealed class PointillismData : EffectData
{
	[Caption ("Dot Spacing"), MinimumValue (2), MaximumValue (64)] public int Spacing { get; set; } = 7;
	[Caption ("Dot Size (%)"), MinimumValue (10), MaximumValue (100)] public int DotSize { get; set; } = 45;
}

public sealed class StainedGlassEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Stained Glass");
	public override string Icon => "pinta-modern-stained-glass";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Artistic");
	public StainedGlassData Data => (StainedGlassData) EffectData!;
	public StainedGlassEffect () { EffectData = new StainedGlassData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize (); var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		int cell = Math.Max (2, Data.CellSize);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X, y = p.coordinates.Y;
			double first = double.MaxValue, second = double.MaxValue;
			var point = PointI.Zero; var other = new PointI (-1, -1);
			for (int j = -1; j <= 1; j++) for (int i = -1; i <= 1; i++) {
				var seed = PaintCells.Seed (x / cell + i, y / cell + j, cell, size.Width, size.Height);
				// Clamping outside cells can yield the same seed at the image border.
				if (seed == point && first != double.MaxValue || seed == other) continue;
				double distance = Math.Sqrt ((x - seed.X) * (x - seed.X) + (y - seed.Y) * (y - seed.Y));
				if (distance < first) { second = first; other = point; first = distance; point = seed; }
				else if (distance < second) { second = distance; other = seed; }
			}
			var c = src[point.Y * size.Width + point.X].ToStraightAlpha ();
			double edge = Math.Clamp ((second - first - Data.GroutWidth) * 1.5 + .5, 0, 1);
			dst[p.memoryOffset] = ColorBgra.FromBgra ((byte) (c.B * edge), (byte) (c.G * edge), (byte) (c.R * edge), src[p.memoryOffset].A).ToPremultipliedAlpha ();
		}
	}
}
public sealed class StainedGlassData : EffectData
{
	[Caption ("Cell Size"), MinimumValue (2), MaximumValue (128)] public int CellSize { get; set; } = 24;
	[Caption ("Grout Width"), MinimumValue (0), MaximumValue (8)] public int GroutWidth { get; set; } = 1;
}
