using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioObject
{
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame f, ColorBgra p, int x, int y)
	{
		var c = StudioRgb.From (p);
		switch (kind) {
			case StudioKind.OuterGlow:
				double coverage = f.Blurred (x, y, s.Radius).A / 255.0;
				var halo = s.First.Pixel (255 * coverage * (.4 + s.Detail * .6) * s.FirstAlpha);
				return StudioMath.Over (p, halo);
			case StudioKind.LongShadow:
				int length = s.Radius * 3, steps = Math.Min (64, length);
				double alpha = 0;
				for (int i = 1; i <= steps; i++) {
					double distance = i * length / (double) steps;
					var behind = f.Outside ((int) Math.Round (x - Math.Cos (s.Angle) * distance), (int) Math.Round (y - Math.Sin (s.Angle) * distance));
					alpha = Math.Max (alpha, behind.A * (1 - i / (double) (steps + 1) * s.Detail));
				}
				return StudioMath.Over (p, s.First.Pixel (alpha * s.FirstAlpha));
			case StudioKind.AlphaErode: return p.NewAlpha (f.MorphAlpha (x, y, s.Radius, false));
			case StudioKind.AlphaDilate:
				byte dilated = f.MorphAlpha (x, y, s.Radius, true);
				if (p.A == 0) c = StudioRgb.From (f.Blurred (x, y, s.Radius));
				return c.Pixel (dilated);
			case StudioKind.EdgeColorize:
				double edge = Math.Clamp (f.Edge (x, y) * (1 + s.Detail * 5), 0, 1);
				return (c * .15 + StudioMath.Lerp (s.First, s.Second, edge * edge) * edge).Pixel (p.A);
			case StudioKind.NormalMap:
				var slope = f.Slope (x, y, s.Radius);
				double nx = -slope.X * (1 + s.Detail * 12), ny = -slope.Y * (1 + s.Detail * 12);
				double magnitude = Math.Sqrt (nx * nx + ny * ny + 1);
				return new StudioRgb (.5 + nx / magnitude * .5, .5 + ny / magnitude * .5, .5 + .5 / magnitude).Pixel (p.A);
			case StudioKind.HeightLight:
				var gradient = f.Slope (x, y, s.Radius);
				double gx = -gradient.X * (2 + s.Detail * 12), gy = -gradient.Y * (2 + s.Detail * 12);
				double normalLength = Math.Sqrt (gx * gx + gy * gy + 1);
				double light = (gx * Math.Cos (s.Angle) * .7 + gy * Math.Sin (s.Angle) * .7 + .7) / normalLength;
				return (c * s.First * (.3 + Math.Max (0, light) * 1.2)).Pixel (p.A);
			case StudioKind.DistanceFade:
				double dx = (x + .5 - f.Width * .5) / Math.Max (.5, f.Width * .5), dy = (y + .5 - f.Height * .5) / Math.Max (.5, f.Height * .5);
				double radius = Math.Sqrt (dx * dx + dy * dy), outer = s.Scale / 100.0 * 1.5;
				double fade = 1 - StudioMath.Smooth (outer * (.2 + s.Detail * .5), Math.Max (.02, outer), radius);
				return c.Pixel (p.A * fade);
			case StudioKind.RoundedCorners:
				double corner = Math.Min (s.Radius, Math.Min (f.Width, f.Height) * .5);
				double qx = Math.Abs (x + .5 - f.Width * .5) - (f.Width * .5 - corner);
				double qy = Math.Abs (y + .5 - f.Height * .5) - (f.Height * .5 - corner);
				double sd = Math.Sqrt (Math.Max (qx, 0) * Math.Max (qx, 0) + Math.Max (qy, 0) * Math.Max (qy, 0)) + Math.Min (Math.Max (qx, qy), 0) - corner;
				return c.Pixel (p.A * Math.Clamp (.5 - sd, 0, 1));
			case StudioKind.SeamlessBlend:
				double wx = .5 * (1 - StudioMath.Smooth (0, s.Radius, Math.Min (x, f.Width - 1 - x)));
				double wy = .5 * (1 - StudioMath.Smooth (0, s.Radius, Math.Min (y, f.Height - 1 - y)));
				var top = StudioMath.Mix (p, f.At (f.Width - 1 - x, y), wx);
				var bottom = StudioMath.Mix (f.At (x, f.Height - 1 - y), f.At (f.Width - 1 - x, f.Height - 1 - y), wx);
				return StudioMath.Mix (top, bottom, wy);
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
	}
}
