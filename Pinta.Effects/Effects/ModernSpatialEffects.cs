// Original implementations, MIT license. The source stays immutable during tiled renders.
using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public static class ModernSampling
{
	public static ColorBgra Bilinear (ReadOnlySpan<ColorBgra> data, int width, int height, double x, double y)
	{
		x = Math.Clamp (x, 0, width - 1); y = Math.Clamp (y, 0, height - 1);
		int x0 = (int) x, y0 = (int) y, x1 = Math.Min (x0 + 1, width - 1), y1 = Math.Min (y0 + 1, height - 1);
		double fx = x - x0, fy = y - y0;
		var a = data[y0 * width + x0]; var b = data[y0 * width + x1];
		var c = data[y1 * width + x0]; var d = data[y1 * width + x1];
		byte Channel (byte aa, byte bb, byte cc, byte dd) => (byte) Math.Clamp (Math.Round ((aa * (1 - fx) + bb * fx) * (1 - fy) + (cc * (1 - fx) + dd * fx) * fy), 0, 255);
		return ColorBgra.FromBgra (Channel (a.B, b.B, c.B, d.B), Channel (a.G, b.G, c.G, d.G), Channel (a.R, b.R, c.R, d.R), Channel (a.A, b.A, c.A, d.A));
	}
}

public sealed class SpherizeEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Spherize");
	public override string Icon => "pinta-modern-spherize";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Distort");
	public SpherizeData Data => (SpherizeData) EffectData!;
	public SpherizeEffect () { EffectData = new SpherizeData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize ();
		var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		double cx = (size.Width - 1) / 2.0, cy = (size.Height - 1) / 2.0, radius = Math.Max (1, Math.Min (size.Width, size.Height) / 2.0);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			double dx = p.coordinates.X - cx, dy = p.coordinates.Y - cy, r = Math.Sqrt (dx * dx + dy * dy) / radius;
			double scale = r > 0 && r < 1 ? 1 + (Math.Asin (r) * 2 / Math.PI / r - 1) * Data.Amount / 100.0 : 1;
			dst[p.memoryOffset] = ModernSampling.Bilinear (src, size.Width, size.Height, cx + dx * scale, cy + dy * scale);
		}
	}
}
public sealed class SpherizeData : EffectData
{
	[Caption ("Amount"), MinimumValue (-100), MaximumValue (100)] public int Amount { get; set; } = 100;
}

public sealed class WavesEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Waves");
	public override string Icon => "pinta-modern-waves";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Distort");
	public WavesData Data => (WavesData) EffectData!;
	public WavesEffect () { EffectData = new WavesData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize ();
		var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		double frequency = Math.PI * 2 / Math.Max (1, Data.Wavelength), phase = Data.Phase * Math.PI / 180;
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			double x = p.coordinates.X, y = p.coordinates.Y;
			dst[p.memoryOffset] = ModernSampling.Bilinear (src, size.Width, size.Height,
				x + Data.Horizontal * Math.Sin (y * frequency + phase), y + Data.Vertical * Math.Sin (x * frequency + phase));
		}
	}
}
public sealed class WavesData : EffectData
{
	[Caption ("Horizontal Amplitude"), MinimumValue (0), MaximumValue (200)] public int Horizontal { get; set; } = 12;
	[Caption ("Vertical Amplitude"), MinimumValue (0), MaximumValue (200)] public int Vertical { get; set; }
	[Caption ("Wavelength"), MinimumValue (1), MaximumValue (512)] public int Wavelength { get; set; } = 64;
	[Caption ("Phase"), MinimumValue (-180), MaximumValue (180)] public int Phase { get; set; }
}

public sealed class MosaicEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Mosaic");
	public override string Icon => "pinta-modern-mosaic";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Artistic");
	public MosaicData Data => (MosaicData) EffectData!;
	public MosaicEffect () { EffectData = new MosaicData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize (); var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		int cell = Math.Max (2, Data.TileSize), gap = Math.Clamp (Data.Grout, 0, cell - 1);
		// Average each intersecting tile once, independent of renderer tile boundaries.
		for (int y0 = roi.Top / cell * cell; y0 <= roi.Bottom; y0 += cell)
			for (int x0 = roi.Left / cell * cell; x0 <= roi.Right; x0 += cell) {
				int right = Math.Min (x0 + cell, size.Width), bottom = Math.Min (y0 + cell, size.Height), count = (right - x0) * (bottom - y0);
				long b = 0, g = 0, r = 0, a = 0;
				for (int y = y0; y < bottom; y++) for (int x = x0; x < right; x++) {
					var c = src[y * size.Width + x]; b += c.B; g += c.G; r += c.R; a += c.A;
				}
				var mean = ColorBgra.FromBgra ((byte) (b / count), (byte) (g / count), (byte) (r / count), (byte) (a / count));
				for (int y = Math.Max (y0, roi.Top); y <= Math.Min (bottom - 1, roi.Bottom); y++)
					for (int x = Math.Max (x0, roi.Left); x <= Math.Min (right - 1, roi.Right); x++)
						dst[y * size.Width + x] = x - x0 < gap || y - y0 < gap ? ColorBgra.Black.NewAlpha (mean.A) : mean;
			}
	}
}
public sealed class MosaicData : EffectData
{
	[Caption ("Tile Size"), MinimumValue (2), MaximumValue (128)] public int TileSize { get; set; } = 12;
	[Caption ("Grout Width"), MinimumValue (0), MaximumValue (16)] public int Grout { get; set; } = 1;
}

public sealed class LinocutEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Linocut");
	public override string Icon => "pinta-modern-linocut";
	public override string EffectMenuCategory => Translations.GetString ("Artistic");
	public LinocutData Data => (LinocutData) EffectData!;
	public LinocutEffect () { EffectData = new LinocutData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		byte c = .2126 * p.R + .7152 * p.G + .0722 * p.B >= Data.Threshold ? (byte) 255 : (byte) 0;
		return ColorBgra.FromBgra (c, c, c, p.A);
	}
}
public sealed class LinocutData : EffectData
{
	[Caption ("Ink Threshold"), MinimumValue (0), MaximumValue (255)] public int Threshold { get; set; } = 128;
}

public sealed class ChromaKeyEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Chroma Key");
	public override string Icon => "pinta-modern-chroma-key";
	public override string EffectMenuCategory => Translations.GetString ("Object");
	public ChromaKeyData Data => (ChromaKeyData) EffectData!;
	public ChromaKeyEffect () { EffectData = new ChromaKeyData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		double distance = Math.Sqrt (Math.Pow (p.R - Data.Red, 2) + Math.Pow (p.G - Data.Green, 2) + Math.Pow (p.B - Data.Blue, 2));
		double alpha = Math.Clamp ((distance - Data.Tolerance) / Math.Max (1, Data.Softness), 0, 1);
		return ColorBgra.FromBgra (p.B, p.G, p.R, Byte (p.A * alpha));
	}
}
public sealed class ChromaKeyData : EffectData
{
	[Caption ("Key Red"), MinimumValue (0), MaximumValue (255)] public int Red { get; set; }
	[Caption ("Key Green"), MinimumValue (0), MaximumValue (255)] public int Green { get; set; } = 255;
	[Caption ("Key Blue"), MinimumValue (0), MaximumValue (255)] public int Blue { get; set; }
	[Caption ("Tolerance"), MinimumValue (0), MaximumValue (442)] public int Tolerance { get; set; } = 80;
	[Caption ("Softness"), MinimumValue (1), MaximumValue (255)] public int Softness { get; set; } = 40;
}

public sealed class InnerShadowEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Inner Shadow");
	public override string Icon => "pinta-modern-inner-shadow";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Object");
	public InnerShadowData Data => (InnerShadowData) EffectData!;
	public InnerShadowEffect () { EffectData = new InnerShadowData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize (); var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		int radius = Math.Clamp (Data.Softness, 0, 32);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			var c = src[p.memoryOffset]; double alpha = 0; int samples = 0;
			// A bounded 5x5 sampling kernel gives deterministic, smooth falloff.
			for (int j = -2; j <= 2; j++) for (int i = -2; i <= 2; i++) {
				int x = p.coordinates.X - Data.OffsetX + i * radius / 2, y = p.coordinates.Y - Data.OffsetY + j * radius / 2;
				alpha += x < 0 || y < 0 || x >= size.Width || y >= size.Height ? 0 : src[y * size.Width + x].A;
				samples++;
			}
			double factor = 1 - (1 - alpha / samples / 255) * Data.Opacity / 100.0;
			dst[p.memoryOffset] = ColorBgra.FromBgra ((byte) (c.B * factor), (byte) (c.G * factor), (byte) (c.R * factor), c.A);
		}
	}
}
public sealed class InnerShadowData : EffectData
{
	[Caption ("Horizontal Offset"), MinimumValue (-100), MaximumValue (100)] public int OffsetX { get; set; } = 5;
	[Caption ("Vertical Offset"), MinimumValue (-100), MaximumValue (100)] public int OffsetY { get; set; } = 5;
	[Caption ("Softness"), MinimumValue (0), MaximumValue (32)] public int Softness { get; set; } = 4;
	[Caption ("Opacity (%)"), MinimumValue (0), MaximumValue (100)] public int Opacity { get; set; } = 70;
}

public sealed class BevelObjectEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Bevel Object");
	public override string Icon => "pinta-modern-bevel";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Object");
	public BevelObjectData Data => (BevelObjectData) EffectData!;
	public BevelObjectEffect () { EffectData = new BevelObjectData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize (); var src = source.GetReadOnlyPixelData (); var dst = destination.GetPixelData ();
		int depth = Math.Max (1, Data.Depth);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X, y = p.coordinates.Y;
			var c = src[p.memoryOffset];
			int before = x < depth || y < depth ? 0 : src[(y - depth) * size.Width + x - depth].A;
			int after = x + depth >= size.Width || y + depth >= size.Height ? 0 : src[(y + depth) * size.Width + x + depth].A;
			double lighting = (after - before) / 255.0 * Data.Strength / 100.0;
			byte Channel (byte v) => (byte) Math.Clamp (Math.Round (lighting > 0 ? v + (c.A - v) * lighting : v * (1 + lighting)), 0, c.A);
			dst[p.memoryOffset] = ColorBgra.FromBgra (Channel (c.B), Channel (c.G), Channel (c.R), c.A);
		}
	}
}
public sealed class BevelObjectData : EffectData
{
	[Caption ("Depth"), MinimumValue (1), MaximumValue (64)] public int Depth { get; set; } = 4;
	[Caption ("Strength (%)"), MinimumValue (0), MaximumValue (100)] public int Strength { get; set; } = 70;
}
