using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioOptics
{
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame f, ColorBgra p, int x, int y)
	{
		var c = StudioRgb.From (p); StudioRgb result;
		switch (kind) {
			case StudioKind.BloomDiffusion:
				var light = f.LightBlurred (x, y, s.Radius, .35 + s.Detail * .55);
				var bloom = new StudioRgb (light.R / 255.0, light.G / 255.0, light.B / 255.0) * 2.2;
				result = new (1 - (1 - c.R) * Math.Max (0, 1 - bloom.R), 1 - (1 - c.G) * Math.Max (0, 1 - bloom.G), 1 - (1 - c.B) * Math.Max (0, 1 - bloom.B));
				break;
			case StudioKind.OrtonDream:
				var diffused = StudioRgb.From (f.Blurred (x, y, s.Radius));
				var screened = new StudioRgb (1 - (1 - c.R) * (1 - diffused.R), 1 - (1 - c.G) * (1 - diffused.G), 1 - (1 - c.B) * (1 - diffused.B));
				result = StudioMath.Lerp (screened, screened * diffused * 1.4, .3 + s.Detail * .5);
				break;
			case StudioKind.SoftFocus:
				var soft = StudioRgb.From (f.Blurred (x, y, s.Radius));
				result = StudioMath.Lerp (c, soft, .35 + s.Detail * .6);
				result += soft * .04;
				break;
			case StudioKind.UnsharpDetail:
				result = c + (c - StudioRgb.From (f.Blurred (x, y, s.Radius))) * (.5 + s.Detail * 3);
				break;
			case StudioKind.HighPass:
				result = new StudioRgb (.5, .5, .5) + (c - StudioRgb.From (f.Blurred (x, y, s.Radius))) * (.5 + s.Detail * 2);
				break;
			case StudioKind.Clarity:
				double low = StudioRgb.From (f.Blurred (x, y, s.Radius)).Luma;
				double delta = (c.Luma - low) * (.5 + s.Detail * 2);
				result = c + new StudioRgb (delta, delta, delta);
				break;
			case StudioKind.TiltShift:
				double nx = (x + .5) / f.Width - .5, ny = (y + .5) / f.Height - .5;
				double distance = Math.Abs (nx * Math.Sin (s.Angle) + ny * Math.Cos (s.Angle)) * 2;
				double focus = Math.Clamp (s.Scale / 100.0, .02, .95);
				return StudioMath.Mix (p, f.Blurred (x, y, s.Radius), StudioMath.Smooth (focus, focus + .35, distance));
			case StudioKind.BokehDisc:
				double r = 0, g = 0, b = 0, a = 0, weight = 0;
				for (int i = 0; i < 24; i++) {
					double angle = i * 2.399963229728653, radius = Math.Sqrt ((i + .5) / 24) * s.Radius;
					var sample = f.Sample (x + Math.Cos (angle) * radius, y + Math.Sin (angle) * radius);
					double w = 1 + Math.Pow (StudioRgb.From (sample).Luma, 4) * s.Detail * 3;
					r += sample.R * w; g += sample.G * w; b += sample.B * w; a += sample.A * w; weight += w;
				}
				result = StudioRgb.From (ColorBgra.FromBgra (StudioMath.Byte (b / weight), StudioMath.Byte (g / weight), StudioMath.Byte (r / weight), StudioMath.Byte (a / weight)));
				break;
			case StudioKind.ChromaticFringe:
				double cx = (f.Width - 1) * .5, cy = (f.Height - 1) * .5;
				double dx = (x - cx) / Math.Max (1, cx), dy = (y - cy) / Math.Max (1, cy);
				double shift = s.Radius * (.3 + s.Detail) * (dx * dx + dy * dy) * .5;
				var red = StudioRgb.From (f.Sample (x + dx * shift, y + dy * shift));
				var blue = StudioRgb.From (f.Sample (x - dx * shift, y - dy * shift));
				result = new (red.R, c.G, blue.B);
				break;
			case StudioKind.AnamorphicStreak:
				var streak = f.LightStreak (x, y, s.Radius * 4, .4 + s.Detail * .5);
				var streakLight = new StudioRgb (streak.R / 255.0, streak.G / 255.0, streak.B / 255.0) * s.First * (5 * s.FirstAlpha);
				result = c + streakLight;
				break;
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
		return result.Pixel (p.A);
	}
}
