// Independently implemented, MIT licensed. Operates on Pinta/Cairo surfaces.
using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class GridCheckerboardEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Grid / Checkerboard");
	public override string Icon => "pinta-modern-grid";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Render");
	public GridCheckerboardData Data => (GridCheckerboardData) EffectData!;
	public GridCheckerboardEffect () { EffectData = new GridCheckerboardData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var dst = destination.GetPixelData ();
		var src = source.GetReadOnlyPixelData ();
		var size = source.GetSize ();
		int cell = Math.Max (1, Data.CellSize), line = Math.Clamp (Data.LineWidth, 1, cell);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X, y = p.coordinates.Y;
			bool ink = Data.Checkerboard ? (x / cell + y / cell) % 2 == 0 : x % cell < line || y % cell < line;
			dst[p.memoryOffset] = ink ? ColorBgra.Black : Data.PreserveBackground ? src[p.memoryOffset] : ColorBgra.White;
		}
	}
}
public sealed class GridCheckerboardData : EffectData
{
	[Caption ("Cell Size"), MinimumValue (1), MaximumValue (512)] public int CellSize { get; set; } = 32;
	[Caption ("Line Width"), MinimumValue (1), MaximumValue (32)] public int LineWidth { get; set; } = 1;
	[Caption ("Checkerboard")] public bool Checkerboard { get; set; }
	[Caption ("Preserve Background")] public bool PreserveBackground { get; set; } = true;
}

public sealed class PolygonStarsEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Polygon / Stars");
	public override string Icon => "pinta-modern-stars";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Render");
	public PolygonStarsData Data => (PolygonStarsData) EffectData!;
	public PolygonStarsEffect () { EffectData = new PolygonStarsData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize ();
		int points = Math.Clamp (Data.Points, 3, 64), count = points * 2;
		double radius = Math.Min (size.Width, size.Height) * .48 * Data.Size / 100;
		double[] xs = new double[count], ys = new double[count];
		for (int i = 0; i < count; i++) {
			double a = i * Math.PI / points + Data.Rotation * Math.PI / 180 - Math.PI / 2;
			double r = radius * (i % 2 == 0 ? 1 : Data.InnerRadius / 100.0);
			xs[i] = size.Width / 2.0 + Math.Cos (a) * r;
			ys[i] = size.Height / 2.0 + Math.Sin (a) * r;
		}
		var src = source.GetReadOnlyPixelData ();
		var dst = destination.GetPixelData ();
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			// Four samples per pixel keep the independently rendered edges antialiased.
			int hits = 0;
			for (int sample = 0; sample < 4; sample++) {
				double x = p.coordinates.X + (sample % 2 + .5) / 2, y = p.coordinates.Y + (sample / 2 + .5) / 2;
				bool inside = false;
				for (int i = 0, j = count - 1; i < count; j = i++)
					if ((ys[i] > y) != (ys[j] > y) && x < (xs[j] - xs[i]) * (y - ys[i]) / (ys[j] - ys[i]) + xs[i]) inside = !inside;
				if (inside) hits++;
			}
			var c = src[p.memoryOffset];
			double remaining = 1 - hits / 4.0;
			dst[p.memoryOffset] = ColorBgra.FromBgra ((byte) (c.B * remaining), (byte) (c.G * remaining), (byte) (c.R * remaining), (byte) (255 - (255 - c.A) * remaining));
		}
	}
}
public sealed class PolygonStarsData : EffectData
{
	[Caption ("Points"), MinimumValue (3), MaximumValue (64)] public int Points { get; set; } = 5;
	[Caption ("Inner Radius (%)"), MinimumValue (1), MaximumValue (100)] public int InnerRadius { get; set; } = 45;
	[Caption ("Size (%)"), MinimumValue (1), MaximumValue (100)] public int Size { get; set; } = 80;
	[Caption ("Rotation"), MinimumValue (-180), MaximumValue (180)] public int Rotation { get; set; }
}

public sealed class OrderedDitherEffect : ConfigurableEffect
{
	private static readonly int[] bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];
	public override string Name => Translations.GetString ("Ordered Dither");
	public override string Icon => "pinta-modern-ordered-dither";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Color");
	public OrderedDitherData Data => (OrderedDitherData) EffectData!;
	public OrderedDitherEffect () { EffectData = new OrderedDitherData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var src = source.GetReadOnlyPixelData ();
		var dst = destination.GetPixelData ();
		double levels = Math.Clamp (Data.Levels, 2, 16) - 1;
		foreach (var p in Tiling.GeneratePixelOffsets (roi, source.GetSize ())) {
			var c = src[p.memoryOffset].ToStraightAlpha ();
			double threshold = (bayer[(p.coordinates.Y % 4) * 4 + p.coordinates.X % 4] + .5) / 16 - .5;
			byte Quantize (double channel) => (byte) Math.Clamp (Math.Round (channel / 255 * levels + threshold) / levels * 255, 0, 255);
			if (Data.Monochrome) {
				byte gray = Quantize (.2126 * c.R + .7152 * c.G + .0722 * c.B);
				dst[p.memoryOffset] = ColorBgra.FromBgra (gray, gray, gray, c.A).ToPremultipliedAlpha ();
			} else dst[p.memoryOffset] = ColorBgra.FromBgra (Quantize (c.B), Quantize (c.G), Quantize (c.R), c.A).ToPremultipliedAlpha ();
		}
	}
}
public sealed class OrderedDitherData : EffectData
{
	[Caption ("Levels Per Channel"), MinimumValue (2), MaximumValue (16)] public int Levels { get; set; } = 2;
	[Caption ("Monochrome")] public bool Monochrome { get; set; }
}

public sealed class HalftoneEffect : ConfigurableEffect
{
	public override string Name => Translations.GetString ("Halftone");
	public override string Icon => "pinta-modern-halftone";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Stylize");
	public HalftoneData Data => (HalftoneData) EffectData!;
	public HalftoneEffect () { EffectData = new HalftoneData (); }
	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var size = source.GetSize ();
		var src = source.GetReadOnlyPixelData ();
		var dst = destination.GetPixelData ();
		int cell = Math.Max (2, Data.CellSize);
		foreach (var p in Tiling.GeneratePixelOffsets (roi, size)) {
			int x = p.coordinates.X, y = p.coordinates.Y;
			int cx = Math.Min (x / cell * cell + cell / 2, size.Width - 1), cy = Math.Min (y / cell * cell + cell / 2, size.Height - 1);
			var c = src[cy * size.Width + cx].ToStraightAlpha ();
			double luminosity = (.2126 * c.R + .7152 * c.G + .0722 * c.B) / 255;
			double radius = cell * Math.Sqrt ((1 - luminosity) / Math.PI);
			double dx = x % cell + .5 - cell / 2.0, dy = y % cell + .5 - cell / 2.0;
			byte ink = (byte) (Math.Clamp (Math.Sqrt (dx * dx + dy * dy) - radius + .5, 0, 1) * 255);
			dst[p.memoryOffset] = ColorBgra.FromBgra (ink, ink, ink, src[p.memoryOffset].A).ToPremultipliedAlpha ();
		}
	}
}
public sealed class HalftoneData : EffectData
{
	[Caption ("Dot Spacing"), MinimumValue (2), MaximumValue (64)] public int CellSize { get; set; } = 8;
}
