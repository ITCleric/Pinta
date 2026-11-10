using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioPhoto
{
	private static double Curve (double v, double slope, double low) => (1 / (1 + Math.Exp (-slope * (v - .5))) - low) / (1 - 2 * low);
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame frame, ColorBgra p, int x, int y)
	{
		var c = StudioRgb.From (p); double l = c.Luma;
		StudioRgb result;
		switch (kind) {
			case StudioKind.SplitToning:
				var tint = StudioMath.Lerp (s.First, s.Second, StudioMath.Smooth (.1, .9, l));
				var colored = tint * Math.Min (2, (l + .08) / (tint.Luma + .08));
				result = StudioMath.Lerp (c, colored, .25 + s.Detail * .55);
				break;
			case StudioKind.Vibrance:
				double saturation = Math.Max (c.R, Math.Max (c.G, c.B)) - Math.Min (c.R, Math.Min (c.G, c.B));
				double protection = c.R > c.G && c.G > c.B ? .55 : 1;
				result = new StudioRgb (l, l, l) + (c - new StudioRgb (l, l, l)) * (1 + (1 - saturation) * s.Detail * 2 * protection);
				break;
			case StudioKind.ColorTemperature:
				double temperature = (s.Detail - .5) * 2;
				result = c * new StudioRgb (1 + .25 * temperature, 1 + .04 * temperature, 1 - .25 * temperature);
				result *= (l + .001) / (result.Luma + .001);
				break;
			case StudioKind.HighlightRolloff:
				double excess = Math.Max (0, l - .55);
				double recovered = .55 + excess / (1 + excess * (2 + s.Detail * 6));
				result = l > .55 ? c * (recovered / l) : c;
				break;
			case StudioKind.ShadowLift:
				double lift = Math.Pow (1 - l, 3) * (.08 + s.Detail * .36);
				result = c + new StudioRgb (lift * .94, lift, lift * 1.07);
				break;
			case StudioKind.FilmicContrast:
				double slope = 3 + s.Detail * 9;
				double low = 1 / (1 + Math.Exp (slope * .5));
				result = new (Curve (c.R, slope, low), Curve (c.G, slope, low), Curve (c.B, slope, low));
				break;
			case StudioKind.MatteFade:
				double fade = .04 + s.Detail * .28;
				result = c * (1 - fade * .8) + new StudioRgb (fade * .6, fade * .58, fade * .62);
				break;
			case StudioKind.BleachBypass:
				var desaturated = StudioMath.Lerp (c, new StudioRgb (l, l, l), .45 + s.Detail * .4);
				result = desaturated.Map (v => v < .5 ? 2 * v * v : 1 - 2 * (1 - v) * (1 - v));
				break;
			case StudioKind.DuotonePrint:
				int bx = (x / Math.Max (1, s.Scale / 4)) & 3, by = (y / Math.Max (1, s.Scale / 4)) & 3;
				double threshold = (StudioMath.Bayer[by * 4 + bx] + .5) / 16;
				result = l > threshold ? s.Second : s.First;
				break;
			case StudioKind.SelectiveColorPop:
				double delta = Math.Abs (StudioMath.Hue (c) - StudioMath.Hue (s.First));
				delta = Math.Min (delta, Math.PI * 2 - delta);
				double tolerance = .12 + s.Detail * 1.6;
				double keep = 1 - StudioMath.Smooth (tolerance * .35, tolerance, delta);
				result = StudioMath.Lerp (new (l, l, l), c, keep);
				break;
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
		return result.Pixel (p.A);
	}
}
