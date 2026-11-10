using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioPlay
{
	// 5x7 bitmap glyphs, authored here; no external font or emoji asset is required.
	private static readonly byte[][] glyphs = [
		[0,0,0,0,0,0,0], [0,0,0,0,0,4,0], [0,4,0,0,4,0,0], [0,0,0,31,0,0,0],
		[0,4,4,31,4,4,0], [0,21,14,31,14,21,0], [0,14,17,17,17,14,0],
		[10,10,31,10,31,10,10], [17,18,4,4,8,9,17], [14,17,23,21,23,16,14],
	];
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame f, ColorBgra p, int x, int y)
	{
		var c = StudioRgb.From (p); StudioRgb result;
		switch (kind) {
			case StudioKind.VhsTape:
				double jitter = (StudioMath.Noise (0, y / s.Scale, s.Seed) - .5) * s.Radius * 2;
				var baseColor = StudioRgb.From (f.Sample (x + jitter, y));
				var red = StudioRgb.From (f.Sample (x + jitter - s.Radius, y));
				var blue = StudioRgb.From (f.Sample (x + jitter + s.Radius, y));
				double scan = (y & 1) == 0 ? .88 : 1;
				double grain = (StudioMath.Noise (x, y, s.Seed + 1) - .5) * .1 * s.Detail;
				double scratch = StudioMath.Noise (0, y, s.Seed + 4) > .985 ? .2 + s.Detail * .3 : 0;
				result = new StudioRgb (red.R, baseColor.G, blue.B) * scan + new StudioRgb (grain + scratch, grain + scratch, grain + scratch);
				break;
			case StudioKind.CrtScreen:
				double nx = (x + .5) / f.Width * 2 - 1, ny = (y + .5) / f.Height * 2 - 1;
				double curvature = 1 + s.Detail * .13 * (nx * nx + ny * ny);
				var screen = StudioRgb.From (f.Sample ((nx * curvature + 1) * f.Width * .5 - .5, (ny * curvature + 1) * f.Height * .5 - .5));
				int channel = x / s.Scale % 3;
				double line = y % (s.Scale * 2) < s.Scale ? 1 : .62;
				result = screen * new StudioRgb (channel == 0 ? 1.25 : .65, channel == 1 ? 1.25 : .65, channel == 2 ? 1.25 : .65) * line * (1 - .15 * (nx * nx + ny * ny));
				break;
			case StudioKind.PocketPalette:
				double luminance = StudioRgb.From (f.AverageCell (x, y, s.Scale, s.Scale)).Luma;
				double dither = (StudioMath.Bayer[(y / s.Scale % 4) * 4 + x / s.Scale % 4] / 16.0 - .5) * .18;
				int level = (int) Math.Clamp (Math.Round ((luminance + dither) * 3), 0, 3);
				result = level switch { 0 => new (.06,.18,.08), 1 => new (.22,.39,.13), 2 => new (.53,.65,.25), _ => new (.79,.87,.53) };
				break;
			case StudioKind.ThermalVision:
				double heat = Math.Pow (Math.Clamp (c.Luma, 0, 1), .5 + s.Detail);
				result = heat > .85 ? StudioMath.Lerp (new (1,.2,0), new (1,1,.85), (heat - .85) / .15)
					: StudioMath.Hsv ((1 - heat / .85) * .72, 1, .2 + .8 * Math.Sqrt (heat / .85));
				break;
			case StudioKind.AsciiMosaic:
				int cw = s.Scale, ch = Math.Max (7, s.Scale * 7 / 5);
				double tone = StudioRgb.From (f.AverageCell (x, y, cw, ch)).Luma;
				int glyph = (int) Math.Clamp (Math.Round ((1 - tone) * (glyphs.Length - 1)), 0, glyphs.Length - 1);
				int column = x % cw * 5 / cw, row = y % ch * 7 / ch;
				result = (glyphs[glyph][row] & (1 << (4 - column))) != 0 ? s.First : s.Second;
				break;
			case StudioKind.EmojiMosaic:
				double lum = StudioRgb.From (f.AverageCell (x, y, s.Scale, s.Scale)).Luma;
				double u = (x % s.Scale + .5) / s.Scale - .5, v = (y % s.Scale + .5) / s.Scale - .5;
				double face = Math.Clamp ((.44 - Math.Sqrt (u * u + v * v)) * s.Scale + .5, 0, 1);
				bool eye = Math.Pow (Math.Abs (u) - .16, 2) + Math.Pow (v + .1, 2) < .003;
				double smile = lum > .35 ? .17 + .65 * (.065 - u * u) : .23 - .8 * (.065 - u * u);
				bool mouth = lum > .75 ? u * u + Math.Pow (v - .2, 2) < .008 : Math.Abs (u) < .23 && Math.Abs (v - smile) < .025;
				var faceColor = eye || mouth ? new StudioRgb (.14,.12,.1) : s.First * (1.05 - v * .3);
				result = StudioMath.Lerp (s.Second, faceColor, face);
				break;
			case StudioKind.PixelConfetti:
				int gx = x / s.Scale, gy = y / s.Scale;
				double chance = StudioMath.Noise (gx, gy, s.Seed);
				if (chance > .15 + s.Detail * .8) return p;
				double px = x - (gx + .2 + StudioMath.Noise (gx, gy, s.Seed + 1) * .6) * s.Scale;
				double py = y - (gy + .2 + StudioMath.Noise (gx, gy, s.Seed + 2) * .6) * s.Scale;
				double rotation = StudioMath.Noise (gx, gy, s.Seed + 3) * Math.PI * 2;
				double rx = px * Math.Cos (rotation) + py * Math.Sin (rotation), ry = -px * Math.Sin (rotation) + py * Math.Cos (rotation);
				double edge = Math.Min (s.Scale * .09 - Math.Abs (rx), s.Scale * .25 - Math.Abs (ry));
				double coverage = Math.Clamp (edge + .5, 0, 1);
				var color = StudioMath.Hsv (StudioMath.Noise (gx, gy, s.Seed + 4), .7, 1);
				return StudioMath.Over (color.Pixel (coverage * 235), p);
			case StudioKind.ComicBurst:
				double dx = (x + .5 - f.Width * .5) / Math.Max (1, f.Width * .5), dy = (y + .5 - f.Height * .5) / Math.Max (1, f.Height * .5);
				double angle = Math.Atan2 (dy, dx) + s.Angle;
				double radius = Math.Sqrt (dx * dx + dy * dy);
				double alternate = StudioMath.Fract (angle / (Math.PI * 2) * s.Scale) < .5 ? 0 : 1;
				double intensity = StudioMath.Smooth (.2 + (1 - s.Detail) * .5, 1.1, radius);
				return StudioMath.Over (StudioMath.Lerp (s.First, s.Second, alternate).Pixel (intensity * 235), p);
			case StudioKind.LaserGrid:
				double horizon = f.Height * .35, depth = (y - horizon) / Math.Max (1, f.Height * .65);
				double glow = Math.Exp (-Math.Abs (y - horizon) / Math.Max (1, f.Height * .035));
				double grid = 0;
				if (depth > 0) {
					double floorX = (x - f.Width * .5) / Math.Max (1, f.Width * depth) * s.Scale;
					double floorZ = 1 / (depth + .08) * s.Scale * .15;
					double distX = Math.Min (StudioMath.Fract (floorX), 1 - StudioMath.Fract (floorX));
					double distZ = Math.Min (StudioMath.Fract (floorZ), 1 - StudioMath.Fract (floorZ));
					grid = Math.Max (1 - StudioMath.Smooth (.01, .07, distX), 1 - StudioMath.Smooth (.01, .06, distZ));
				}
				result = s.Second * (.65 + glow) + s.First * (grid * (.6 + s.Detail * .7) + glow * .65);
				return result.Pixel (255);
			case StudioKind.PartyPixelate:
				var cell = StudioRgb.From (f.AverageCell (x, y, s.Scale, s.Scale));
				double seedHue = StudioMath.Noise (x / s.Scale, y / s.Scale, s.Seed);
				var tint = StudioMath.Hsv (seedHue, .6 + s.Detail * .4, 1);
				result = StudioMath.Lerp (cell, tint * (.15 + cell.Luma * 1.1), .25 + s.Detail * .65);
				break;
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
		return result.Pixel (p.A);
	}
}
