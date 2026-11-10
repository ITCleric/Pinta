using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioMaterials
{
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame f, ColorBgra p, int x, int y)
	{
		var c = StudioRgb.From (p); double l = c.Luma; StudioRgb result;
		switch (kind) {
			case StudioKind.FineFilmGrain:
				double grain = StudioMath.Noise (x, y, s.Seed) + StudioMath.Noise (x, y, s.Seed + 1) + StudioMath.Noise (x, y, s.Seed + 2) - 1.5;
				grain *= (.015 + s.Detail * .15) * (.4 + Math.Sin (Math.Clamp (l, 0, 1) * Math.PI) * .6);
				result = c + new StudioRgb (grain, grain * .95, grain * 1.04);
				break;
			case StudioKind.PaperFibers:
				double fiber = Math.Sin (y * Math.PI * 2 / s.Scale + StudioMath.Noise (x / (s.Scale * 2), y / s.Scale, s.Seed) * 2);
				double tooth = StudioMath.Noise (x, y, s.Seed + 3) - .5;
				double light = 1 + fiber * (.025 + s.Detail * .07) + tooth * .08;
				result = StudioMath.Lerp (c, s.Second, .08) * light;
				break;
			case StudioKind.CanvasWeave:
				double u = x * Math.PI * 2 / s.Scale, v = y * Math.PI * 2 / s.Scale;
				double weave = Math.Cos (u) * .5 + Math.Cos (v) * .5;
				weave += Math.Sin (u * .5) * Math.Sin (v * .5) * .3;
				result = c * (1 + weave * (.07 + s.Detail * .2));
				break;
			case StudioKind.BrushedMetal:
				double streak = StudioMath.Noise (x / s.Scale, y, s.Seed) * .12 + StudioMath.Noise (0, y, s.Seed + 1) * .16;
				double reflection = .35 + .35 * Math.Pow (Math.Sin ((y + .5) / f.Height * Math.PI), 3) + streak;
				return (s.First * reflection * (1 + s.Detail * .3)).Pixel (255 * s.FirstAlpha);
			case StudioKind.RainGlass:
				int cellX = x / s.Scale, cellY = y / s.Scale;
				double centerX = (cellX + .25 + StudioMath.Noise (cellX, cellY, s.Seed) * .5) * s.Scale;
				double centerY = (cellY + .25 + StudioMath.Noise (cellX, cellY, s.Seed + 1) * .5) * s.Scale;
				double dx = (x - centerX) / (s.Scale * .4), dy = (y - centerY) / (s.Scale * .48);
				double distance = Math.Sqrt (dx * dx + dy * dy);
				if (distance >= 1) return p;
				var refracted = StudioRgb.From (f.Sample (x + dx * s.Radius * (1 - distance), y + dy * s.Radius * (1 - distance)));
				double shine = Math.Pow (Math.Max (0, 1 - Math.Abs (distance - .8) * 10), 2) * Math.Max (0, -dx - dy) * .3;
				result = refracted + new StudioRgb (shine, shine, shine);
				break;
			case StudioKind.WatercolorWash:
				double best = double.MaxValue; var wash = p;
				for (int qy = -1; qy <= 1; qy += 2) for (int qx = -1; qx <= 1; qx += 2) {
					int sr = 0, sg = 0, sb = 0, sa = 0; double sum = 0, sumSq = 0;
					for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++) {
						var sample = f.At (x + qx * s.Radius * i / 2, y + qy * s.Radius * j / 2);
						double lum = StudioRgb.From (sample).Luma;
						sr += sample.R; sg += sample.G; sb += sample.B; sa += sample.A; sum += lum; sumSq += lum * lum;
					}
					double variance = Math.Max (0, sumSq / 9 - sum * sum / 81);
					if (variance < best) { best = variance; wash = ColorBgra.FromBgra ((byte) (sb / 9), (byte) (sg / 9), (byte) (sr / 9), (byte) (sa / 9)); }
				}
				double paper = 1 + (StudioMath.Noise (x, y, s.Seed) - .5) * .045;
				result = StudioMath.Lerp (c, StudioRgb.From (wash), .6 + s.Detail * .4) * paper;
				break;
			case StudioKind.Crosshatch:
				double xx = x * Math.Cos (s.Angle) + y * Math.Sin (s.Angle), yy = -x * Math.Sin (s.Angle) + y * Math.Cos (s.Angle);
				double stroke = .08 + s.Detail * .18;
				bool ink = l < .85 && StudioMath.Fract (xx / s.Scale) < stroke || l < .55 && StudioMath.Fract (yy / s.Scale) < stroke || l < .25 && StudioMath.Fract (y / (double) s.Scale) < stroke;
				result = ink ? s.First : s.Second;
				break;
			case StudioKind.ComicInking:
				int levels = 3 + (int) (s.Detail * 5);
				var quantized = new StudioRgb (Math.Round (c.R * levels) / levels, Math.Round (c.G * levels) / levels, Math.Round (c.B * levels) / levels);
				double edge = StudioMath.Smooth (.08 + s.Detail * .12, .22 + s.Detail * .12, f.Edge (x, y));
				result = quantized * (1 - edge * .95);
				break;
			case StudioKind.EngravedLines:
				double ex = x * Math.Cos (s.Angle) + y * Math.Sin (s.Angle), ey = -x * Math.Sin (s.Angle) + y * Math.Cos (s.Angle);
				double phase = StudioMath.Fract (ey / s.Scale + Math.Sin (ex / (s.Scale * 3.0)) * .18);
				result = phase < Math.Clamp (1 - l, .02, .98) ? s.First : s.Second;
				break;
			case StudioKind.WoodcutPrint:
				double wood = (StudioMath.Noise (x / s.Scale, y, s.Seed) - .5) * .09 + Math.Sin (x / (double) s.Scale + y * .12) * .06;
				double tone = Math.Floor (Math.Clamp (l + wood, 0, .999) * 3) / 2;
				double carved = StudioMath.Smooth (.08, .18 + s.Detail * .2, f.Edge (x, y));
				result = StudioMath.Lerp (s.First, s.Second, tone) * (1 - carved * .85);
				break;
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
		return result.Pixel (p.A);
	}
}
