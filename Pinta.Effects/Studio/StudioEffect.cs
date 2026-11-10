// Original CPU effect implementations. MIT license; no third-party plugin binaries are loaded.
using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public abstract class StudioEffect : ConfigurableEffect
{
	public StudioKind Kind { get; }
	public StudioEffectInfo Info => StudioCatalog.Get (Kind);
	public StudioEffectData Data => (StudioEffectData) EffectData!;
	protected StudioEffect (StudioKind kind)
	{
		Kind = kind;
		EffectData = new StudioEffectData (StudioCatalog.Get (kind));
	}
	public override string Name => Translations.GetString (Info.Name);
	public override string Description => Translations.GetString (Info.Description);
	public override string Icon => "pinta-studio-" + Info.Slug;
	public override string EffectMenuCategory => Translations.GetString (Info.Category);
	// Whole-frame preparation makes neighborhood caches independent of the selection boundaries.
	public override bool IsTileable => false;

	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		var input = source.GetReadOnlyPixelData ();
		var output = destination.GetPixelData ();
		// In-place rendering also needs a stable source while remapping or sampling neighbors.
		ColorBgra[]? stable = ReferenceEquals (source, destination) ? input.ToArray () : null;
		if (stable is not null) input = stable;
		var settings = new StudioSettings (Data);
		var frame = new StudioFrame (input, source.Width, source.Height);
		foreach (var roi in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ())) {
				var original = input[pixel.memoryOffset];
				if (settings.Amount == 0) { output[pixel.memoryOffset] = original; continue; }
				int x = pixel.coordinates.X, y = pixel.coordinates.Y;
				ColorBgra result = ((int) Kind / 10) switch {
					0 => StudioPhoto.Process (Kind, settings, ref frame, original, x, y),
					1 => StudioOptics.Process (Kind, settings, ref frame, original, x, y),
					2 => StudioMaterials.Process (Kind, settings, ref frame, original, x, y),
					3 => StudioGeometry.Process (Kind, settings, ref frame, original, x, y),
					4 => StudioObject.Process (Kind, settings, ref frame, original, x, y),
					_ => StudioPlay.Process (Kind, settings, ref frame, original, x, y),
				};
				output[pixel.memoryOffset] = StudioMath.Mix (original, result, settings.Amount);
			}
		}
	}
}

public sealed class StudioEffectData : EffectData
{
	private readonly StudioEffectInfo info;
	public StudioEffectData (StudioEffectInfo info)
	{
		this.info = info;
		Amount = info.Amount; Radius = info.Radius; Scale = info.Scale; Detail = info.Detail;
		FirstColor = Color.FromHex (info.FirstColor)!.Value;
		SecondColor = Color.FromHex (info.SecondColor)!.Value;
	}
	[Caption ("Strength (%)"), MinimumValue (0), MaximumValue (100)] public int Amount { get; set; }
	[Caption ("Radius"), MinimumValue (1), MaximumValue (64), VisibleWhen (nameof (ShowRadius))] public int Radius { get; set; }
	[Caption ("Scale"), MinimumValue (2), MaximumValue (128), VisibleWhen (nameof (ShowScale))] public int Scale { get; set; }
	[Caption ("Angle"), MinimumValue (-180), MaximumValue (180), VisibleWhen (nameof (ShowAngle))] public int Angle { get; set; } = 45;
	[Caption ("Detail"), MinimumValue (0), MaximumValue (100), VisibleWhen (nameof (ShowDetail))] public int Detail { get; set; }
	[Caption ("Seed"), MinimumValue (0), MaximumValue (65535), VisibleWhen (nameof (ShowSeed))] public int Seed { get; set; } = 17;
	[Caption ("First Color"), VisibleWhen (nameof (ShowFirstColor))] public Color FirstColor { get; set; }
	[Caption ("Second Color"), VisibleWhen (nameof (ShowSecondColor))] public Color SecondColor { get; set; }
	[Skip] public bool ShowRadius => info.Controls.HasFlag (StudioControls.Radius);
	[Skip] public bool ShowScale => info.Controls.HasFlag (StudioControls.Scale);
	[Skip] public bool ShowAngle => info.Controls.HasFlag (StudioControls.Angle);
	[Skip] public bool ShowDetail => info.Controls.HasFlag (StudioControls.Detail);
	[Skip] public bool ShowSeed => info.Controls.HasFlag (StudioControls.Seed);
	[Skip] public bool ShowFirstColor => info.Controls.HasFlag (StudioControls.Colors) && info.Kind != StudioKind.PaperFibers;
	[Skip] public bool ShowSecondColor => info.Kind is StudioKind.SplitToning or StudioKind.DuotonePrint or StudioKind.PaperFibers or StudioKind.Crosshatch or StudioKind.EngravedLines or StudioKind.WoodcutPrint or StudioKind.EdgeColorize or StudioKind.AsciiMosaic or StudioKind.EmojiMosaic or StudioKind.ComicBurst or StudioKind.LaserGrid;
}

internal readonly record struct StudioSettings
{
	public double Amount { get; }
	public int Radius { get; }
	public int Scale { get; }
	public double Detail { get; }
	public double Angle { get; }
	public int Seed { get; }
	public StudioRgb First { get; }
	public StudioRgb Second { get; }
	public double FirstAlpha { get; }
	public double SecondAlpha { get; }
	public StudioSettings (StudioEffectData data)
	{
		Amount = Math.Clamp (data.Amount, 0, 100) / 100.0;
		Radius = Math.Clamp (data.Radius, 1, 64); Scale = Math.Clamp (data.Scale, 2, 128);
		Detail = Math.Clamp (data.Detail, 0, 100) / 100.0;
		Angle = data.Angle * Math.PI / 180; Seed = data.Seed;
		First = new (data.FirstColor.R, data.FirstColor.G, data.FirstColor.B);
		Second = new (data.SecondColor.R, data.SecondColor.G, data.SecondColor.B);
		FirstAlpha = Math.Clamp (data.FirstColor.A, 0, 1); SecondAlpha = Math.Clamp (data.SecondColor.A, 0, 1);
	}
}

internal readonly record struct StudioRgb (double R, double G, double B)
{
	public double Luma => .2126 * R + .7152 * G + .0722 * B;
	public static StudioRgb From (ColorBgra p) => p.A == 0 ? new (0, 0, 0) : new (p.R / (double) p.A, p.G / (double) p.A, p.B / (double) p.A);
	public ColorBgra Pixel (double alpha) => ColorBgra.FromBgra (
		StudioMath.Byte (Math.Clamp (B, 0, 1) * alpha), StudioMath.Byte (Math.Clamp (G, 0, 1) * alpha),
		StudioMath.Byte (Math.Clamp (R, 0, 1) * alpha), StudioMath.Byte (alpha));
	public static StudioRgb operator + (StudioRgb a, StudioRgb b) => new (a.R + b.R, a.G + b.G, a.B + b.B);
	public static StudioRgb operator - (StudioRgb a, StudioRgb b) => new (a.R - b.R, a.G - b.G, a.B - b.B);
	public static StudioRgb operator * (StudioRgb a, double v) => new (a.R * v, a.G * v, a.B * v);
	public static StudioRgb operator * (StudioRgb a, StudioRgb b) => new (a.R * b.R, a.G * b.G, a.B * b.B);
	public StudioRgb Map (Func<double, double> map) => new (map (R), map (G), map (B));
}

internal static class StudioMath
{
	public static byte Byte (double v) => (byte) Math.Clamp ((int) Math.Round (v), 0, 255);
	public static double Fract (double v) => v - Math.Floor (v);
	public static double Smooth (double low, double high, double v)
	{
		double t = Math.Clamp ((v - low) / Math.Max (.000001, high - low), 0, 1);
		return t * t * (3 - 2 * t);
	}
	public static double Noise (int x, int y, int seed)
	{
		uint h = unchecked((uint) (x * 374761393 + y * 668265263 + seed * 1442695041));
		h = unchecked((h ^ (h >> 13)) * 1274126177); h ^= h >> 16;
		return h / (double) uint.MaxValue;
	}
	public static StudioRgb Lerp (StudioRgb a, StudioRgb b, double t) => a * (1 - t) + b * t;
	public static ColorBgra Mix (ColorBgra a, ColorBgra b, double t)
	{
		if (t <= 0 || a == b) return a;
		if (t >= 1) return b;
		byte alpha = Byte (a.A + (b.A - a.A) * t);
		return ColorBgra.FromBgra ((byte) Math.Min (alpha, Byte (a.B + (b.B - a.B) * t)),
			(byte) Math.Min (alpha, Byte (a.G + (b.G - a.G) * t)), (byte) Math.Min (alpha, Byte (a.R + (b.R - a.R) * t)), alpha);
	}
	public static ColorBgra Over (ColorBgra top, ColorBgra bottom)
	{
		double t = 1 - top.A / 255.0;
		byte a = Byte (top.A + bottom.A * t);
		return ColorBgra.FromBgra ((byte) Math.Min (a, Byte (top.B + bottom.B * t)),
			(byte) Math.Min (a, Byte (top.G + bottom.G * t)), (byte) Math.Min (a, Byte (top.R + bottom.R * t)), a);
	}
	public static StudioRgb Hsv (double hue, double saturation, double value)
	{
		hue = Fract (hue) * 6; int sector = (int) hue; double f = hue - sector;
		double p = value * (1 - saturation), q = value * (1 - f * saturation), t = value * (1 - (1 - f) * saturation);
		return sector switch { 0 => new (value, t, p), 1 => new (q, value, p), 2 => new (p, value, t), 3 => new (p, q, value), 4 => new (t, p, value), _ => new (value, p, q) };
	}
	public static double Hue (StudioRgb c) => Math.Atan2 (Math.Sqrt (3) * (c.G - c.B), 2 * c.R - c.G - c.B);
	public static readonly int[] Bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];
}
